using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TravelMinion.Domain;

namespace TravelMinion.Infrastructure.Persistence.Configurations;

internal sealed class TripConfiguration : IEntityTypeConfiguration<Trip>
{
    public void Configure(EntityTypeBuilder<Trip> builder)
    {
        builder.ToTable("Trips");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);

        builder.OwnsOne(t => t.Brief, brief =>
        {
            brief.ToTable("TripBriefs");
            brief.Ignore(b => b.Bases);
            brief.Property(b => b.TravelStyle).HasConversion<string>().HasMaxLength(20);
            brief.Property(b => b.Budget).HasMaxLength(200);
            brief.Property(b => b.GroupSize);
            brief.Property(b => b.Mobility).HasMaxLength(400);

            brief.OwnsOne(b => b.Arrival, arrival =>
            {
                arrival.Property(a => a.BaseName).IsRequired().HasMaxLength(200);
                arrival.Property(a => a.Date);
                arrival.Property(a => a.Time);
            });

            brief.OwnsOne(b => b.Departure, departure =>
            {
                departure.Property(d => d.BaseName).IsRequired().HasMaxLength(200);
                departure.Property(d => d.Date);
                departure.Property(d => d.Time);
            });

            brief.OwnsMany(b => b.Countries, country =>
            {
                country.ToTable("Countries");
                country.Property(c => c.Name).IsRequired().HasMaxLength(200);
                country.Property(c => c.SpanInDays);

                country.OwnsMany(c => c.Bases, @base =>
                {
                    @base.ToTable("Bases");
                    @base.Property(b => b.Name).IsRequired().HasMaxLength(200);
                    @base.Property(b => b.Days);
                    @base.Property(b => b.TransitFromPrevious).HasMaxLength(200);
                });
            });

            brief.PrimitiveCollection(b => b.Interests);
            brief.PrimitiveCollection(b => b.Dietary);
            brief.PrimitiveCollection(b => b.PreferredSources);
            brief.PrimitiveCollection(b => b.TravellersToShare);
        });

        builder.OwnsMany(t => t.Suggestions, suggestion =>
        {
            suggestion.ToTable("Suggestions");
            suggestion.Property(s => s.Name).IsRequired().HasMaxLength(400);
            suggestion.Property(s => s.Destination).IsRequired().HasMaxLength(200);
            suggestion.Property(s => s.Rationale).HasMaxLength(2000);
            suggestion.Property(s => s.Area).HasMaxLength(400);
            suggestion.Property(s => s.TypicalDuration).HasMaxLength(100);
            suggestion.Property(s => s.OpeningHours).HasMaxLength(400);
            suggestion.Property(s => s.ApproximateCost).HasMaxLength(200);
            suggestion.Property(s => s.SeasonWeatherFit).HasMaxLength(400);
            suggestion.Property(s => s.SourceLink).HasMaxLength(1000);
            suggestion.Property(s => s.SourceName).HasConversion<string>().HasMaxLength(20);
            suggestion.Property(s => s.Confidence).HasConversion<string>().HasMaxLength(20);
            suggestion.Property(s => s.CouldntVerify).HasMaxLength(400);
            suggestion.Property(s => s.Discarded);
        });

        builder.Property(t => t.Activities).HasConversion(
            new ValueConverter<ApprovedActivityList, string>(
                list => ActivityListSerializer.Serialize(list),
                json => ActivityListSerializer.Deserialize(json)));

        builder.Property(t => t.Itinerary).HasConversion(
            new ValueConverter<Itinerary?, string>(
                itinerary => ItinerarySerializer.Serialize(itinerary!),
                json => ItinerarySerializer.Deserialize(json)));
    }
}
