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
            brief.Property(b => b.StartDate);
            brief.Property(b => b.EndDate);
            brief.Property(b => b.TravelStyle).HasConversion<string>().HasMaxLength(20);
            brief.Property(b => b.Budget).HasMaxLength(200);
            brief.Property(b => b.GroupSize);
            brief.Property(b => b.Mobility).HasMaxLength(400);

            brief.OwnsMany(b => b.Destinations, destination =>
            {
                destination.ToTable("DestinationStops");
                destination.Property(d => d.Destination).IsRequired().HasMaxLength(200);
                destination.Property(d => d.Days);
                destination.Property(d => d.Order);
                destination.Property(d => d.TransitFromPrevious).HasMaxLength(200);
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
