using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using TravelMinion.Domain;

namespace TravelMinion.Infrastructure.Persistence;

/// <summary>
/// Serialises an <see cref="Itinerary"/> to/from JSON. The itinerary is a generated
/// day-by-day snapshot, so it is stored as a single JSON value rather than being
/// normalised into the relational model (which cannot represent the polymorphic
/// day hierarchy cleanly).
/// </summary>
internal static class ItinerarySerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new ItineraryDayJsonConverter() },
    };

    public static string Serialize(Itinerary itinerary)
        => JsonSerializer.Serialize(itinerary.Days, Options);

    public static Itinerary Deserialize(string json)
        => new(JsonSerializer.Deserialize<List<ItineraryDay>>(json, Options) ?? []);
}

internal sealed class ItineraryDayJsonConverter : JsonConverter<ItineraryDay>
{
    public override ItineraryDay Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var dayType = root.TryGetProperty("dayType", out var typeElement) ? typeElement.GetString() : null;
        var date = JsonSerializer.Deserialize<DateOnly>(root.GetProperty("date").GetRawText(), options);
        var destination = root.GetProperty("destination").GetString()!;

        return dayType switch
        {
            "travel" => new TravelDay(
                date,
                destination,
                JsonSerializer.Deserialize<TravelLeg>(root.GetProperty("travelLeg").GetRawText(), options)!,
                TryGetBlock(root, "afternoonActivity", options)),
            "free" => new FreeDay(date, destination, GetString(root, "notes")),
            _ => new ActivityDay(date, destination, GetBlocks(root, options)),
        };
    }

    public override void Write(Utf8JsonWriter writer, ItineraryDay value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        writer.WriteStartObject();

        switch (value)
        {
            case TravelDay travel:
                writer.WriteString("dayType", "travel");
                WriteCommon(writer, travel);
                writer.WritePropertyName("travelLeg");
                JsonSerializer.Serialize(writer, travel.TravelLeg, options);
                if (travel.AfternoonActivity is not null)
                {
                    writer.WritePropertyName("afternoonActivity");
                    JsonSerializer.Serialize(writer, travel.AfternoonActivity, options);
                }

                break;

            case FreeDay free:
                writer.WriteString("dayType", "free");
                WriteCommon(writer, free);
                if (free.Notes is not null)
                {
                    writer.WriteString("notes", free.Notes);
                }

                break;

            default:
                writer.WriteString("dayType", "activity");
                WriteCommon(writer, value);
                writer.WritePropertyName("timeBlocks");
                writer.WriteStartArray();
                foreach (var block in ((ActivityDay)value).TimeBlocks)
                {
                    JsonSerializer.Serialize(writer, block, options);
                }

                writer.WriteEndArray();
                break;
        }

        writer.WriteEndObject();
    }

    private static void WriteCommon(Utf8JsonWriter writer, ItineraryDay day)
    {
        writer.WriteString("date", day.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        writer.WriteString("destination", day.Destination);
    }

    private static string? GetString(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.GetString()
            : null;

    private static TimeBlock? TryGetBlock(JsonElement element, string property, JsonSerializerOptions options)
        => element.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null
            ? JsonSerializer.Deserialize<TimeBlock>(value.GetRawText(), options)
            : null;

    private static List<TimeBlock>? GetBlocks(JsonElement element, JsonSerializerOptions options)
        => element.TryGetProperty("timeBlocks", out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray()
                .Select(block => JsonSerializer.Deserialize<TimeBlock>(block.GetRawText(), options)!)
                .ToList()
            : null;
}
