using System.Text.RegularExpressions;
using TravelMinion.Domain;

namespace TravelMinion.Application;

/// <summary>
/// Plans an Itinerary from a Trip Brief and an Approved Activity List. This is
/// the deterministic port of the Python prototype's <c>planner.py</c>.
/// </summary>
public sealed partial class ItineraryPlanner
{
    private readonly TripBrief _brief;
    private readonly ApprovedActivityList _activities;
    private readonly int _targetDensity;

    public ItineraryPlanner(TripBrief tripBrief, ApprovedActivityList activities)
    {
        _brief = tripBrief ?? throw new ArgumentNullException(nameof(tripBrief));
        _activities = activities ?? throw new ArgumentNullException(nameof(activities));
        _targetDensity = TargetDensity(tripBrief.TravelStyle);
    }

    public Itinerary Plan()
    {
        var days = new List<ItineraryDay>();
        var currentDate = _brief.StartDate;

        var orderedStops = _brief.Destinations
            .OrderBy(stop => stop.Order ?? int.MaxValue)
            .ToList();

        for (var index = 0; index < orderedStops.Count; index++)
        {
            var stop = orderedStops[index];
            var destination = stop.Destination;

            if (index > 0)
            {
                var previous = orderedStops[index - 1].Destination;
                var transit = stop.TransitFromPrevious;
                var transitMinutes = TransitDurationParser.ToMinutes(transit);
                var (mode, duration) = SplitTransit(transit);
                var leg = new TravelLeg(previous, destination, mode, duration);

                if (transitMinutes >= PlannerConstants.LongHaulThreshold)
                {
                    days.Add(new FreeDay(
                        currentDate,
                        destination,
                        $"Recovery day after long travel ({transit ?? "long haul"})"));
                }
                else
                {
                    days.Add(new TravelDay(currentDate, destination, leg, CreateAfternoonActivity(destination)));
                }

                currentDate = currentDate.AddDays(1);
            }

            var destinationActivities = _activities.ByDestination(destination).ToList();
            if (destinationActivities.Count == 0)
            {
                for (var day = 0; day < stop.Days; day++)
                {
                    days.Add(new FreeDay(currentDate, destination, "No planned activities"));
                    currentDate = currentDate.AddDays(1);
                }

                continue;
            }

            var activityDays = PlanDestinationDays(destinationActivities, stop.Days, currentDate, destination);
            days.AddRange(activityDays);
            currentDate = currentDate.AddDays(activityDays.Count);
        }

        return new Itinerary(days);
    }

    private List<ItineraryDay> PlanDestinationDays(
        IReadOnlyList<ApprovedActivity> activities,
        int numDays,
        DateOnly startDate,
        string destination)
    {
        var days = new List<ItineraryDay>();
        if (activities.Count == 0 || numDays <= 0)
        {
            return days;
        }

        var orderedActivities = GroupByArea(activities);
        var activityIndex = 0;

        for (var dayNumber = 0; dayNumber < numDays; dayNumber++)
        {
            var currentDate = startDate.AddDays(dayNumber);

            if (_targetDensity == 0)
            {
                days.Add(new FreeDay(currentDate, destination, "Rest day"));
                continue;
            }

            var blocks = new List<TimeBlock>();
            var currentTime = PlannerConstants.MorningStart;

            var remainingDays = numDays - dayNumber;
            var remainingActivities = orderedActivities.Count - activityIndex;
            var targetToday = remainingDays > 0
                ? Math.Max(1, remainingActivities / remainingDays)
                : _targetDensity;
            targetToday = Math.Min(targetToday, _targetDensity + 1);

            var scheduled = 0;
            while (activityIndex < orderedActivities.Count && scheduled < targetToday)
            {
                if (currentTime >= PlannerConstants.LunchStart && currentTime < PlannerConstants.LunchEnd)
                {
                    currentTime = PlannerConstants.LunchEnd;
                }

                var block = ScheduleActivity(currentTime, orderedActivities[activityIndex]);
                if (block is not null)
                {
                    blocks.Add(block);
                    currentTime = (block.EndTime.Hour * 60) + block.EndTime.Minute + PlannerConstants.DefaultTransit;
                    activityIndex++;
                    scheduled++;
                }
                else
                {
                    activityIndex++;
                }
            }

            days.Add(blocks.Count > 0
                ? new ActivityDay(currentDate, destination, blocks)
                : new FreeDay(currentDate, destination, "No activities scheduled"));
        }

        return days;
    }

    private static TimeBlock? ScheduleActivity(int startMinutes, ApprovedActivity activity, int transitMinutes = PlannerConstants.DefaultTransit)
    {
        var duration = DurationParser.ToMinutes(activity.TypicalDuration);
        var endMinutes = startMinutes + duration;

        var opening = OpeningHoursParser.Parse(activity.OpeningHours);
        if (opening is { } hours)
        {
            var openMinutes = (hours.Open.Hour * 60) + hours.Open.Minute;
            var closeMinutes = (hours.Close.Hour * 60) + hours.Close.Minute;

            if (startMinutes < openMinutes || endMinutes > closeMinutes)
            {
                if (endMinutes <= closeMinutes)
                {
                    startMinutes = openMinutes;
                    endMinutes = startMinutes + duration;
                }
                else
                {
                    return null;
                }
            }
        }

        if (startMinutes >= PlannerConstants.DinnerStart)
        {
            return null;
        }

        if (endMinutes > PlannerConstants.EveningEnd)
        {
            endMinutes = PlannerConstants.EveningEnd;
        }

        var fallback = activity.IndoorFallback ?? IndoorFallback.Suggest(activity);

        return new TimeBlock(
            MinutesToTime(startMinutes),
            MinutesToTime(endMinutes),
            activity.Name,
            activity.Area,
            activity.TypicalDuration,
            $"{transitMinutes} min",
            fallback);
    }

    private static List<ApprovedActivity> GroupByArea(IReadOnlyList<ApprovedActivity> activities)
    {
        var groups = new List<List<ApprovedActivity>>();
        var index = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var activity in activities)
        {
            var area = activity.Area.Trim().ToLowerInvariant();
            if (!index.TryGetValue(area, out var position))
            {
                position = groups.Count;
                index[area] = position;
                groups.Add(new List<ApprovedActivity>());
            }

            groups[position].Add(activity);
        }

        return groups.SelectMany(group => group).ToList();
    }

    private static TimeBlock CreateAfternoonActivity(string destination)
        => new(
            new TimeOnly(15, 0),
            new TimeOnly(17, 0),
            $"Explore {destination}",
            destination,
            "2 hours");

    private static (string? Mode, string? Duration) SplitTransit(string? transit)
    {
        if (string.IsNullOrWhiteSpace(transit))
        {
            return (null, null);
        }

        var match = TransitMode().Match(transit.ToLowerInvariant());
        if (match.Success)
        {
            return (match.Groups[1].Value, transit[match.Length..].Trim());
        }

        return (null, transit);
    }

    private static TimeOnly MinutesToTime(int minutes) => new(minutes / 60, minutes % 60);

    private static int TargetDensity(TravelStyle style) => style switch
    {
        TravelStyle.Packed => 5,
        TravelStyle.Casual => 2,
        _ => 0,
    };

    [GeneratedRegex(@"^(flight|train|bus|car|ferry|boat)\s*")]
    private static partial Regex TransitMode();
}
