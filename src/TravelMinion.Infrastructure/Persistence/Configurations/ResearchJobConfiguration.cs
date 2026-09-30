using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TravelMinion.Domain;

namespace TravelMinion.Infrastructure.Persistence.Configurations;

internal sealed class ResearchJobConfiguration : IEntityTypeConfiguration<ResearchJob>
{
    public void Configure(EntityTypeBuilder<ResearchJob> builder)
    {
        builder.ToTable("ResearchJobs");
        builder.HasKey(j => j.Id);
        builder.Property(j => j.TripId).IsRequired();
        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(j => j.AttemptCount);
        builder.Property(j => j.CreatedAt);
        builder.Property(j => j.StartedAt);
        builder.Property(j => j.CompletedAt);
        builder.Property(j => j.FailureReason).HasMaxLength(2000);

        // Per-destination progress is stored as a JSON column. A nested owned
        // collection here would require the Research Job to be inserted before its
        // rows, which breaks when the job is appended to an already-tracked Trip.
        builder.Property(j => j.Progress)
            .HasConversion(
                new ValueConverter<IReadOnlyList<ResearchJobProgress>, string>(
                    progress => ResearchJobProgressSerializer.Serialize(progress),
                    json => ResearchJobProgressSerializer.Deserialize(json)))
            .Metadata.SetValueComparer(new ResearchJobProgressValueComparer());

        builder.HasIndex(j => j.TripId);

        builder.HasOne<Trip>()
            .WithMany(t => t.ResearchJobs)
            .HasForeignKey(j => j.TripId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
