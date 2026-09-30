using System.Text.Json;
using TravelMinion.Domain;

namespace TravelMinion.Infrastructure.Persistence;

/// <summary>
/// Serialises a Research Job's per-destination progress to a JSON column.
/// </summary>
internal static class ResearchJobProgressSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(IReadOnlyList<ResearchJobProgress> progress)
        => JsonSerializer.Serialize(progress, Options);

    public static IReadOnlyList<ResearchJobProgress> Deserialize(string json)
    {
        var items = JsonSerializer.Deserialize<List<ResearchJobProgressItem>>(json, Options)
            ?? new List<ResearchJobProgressItem>();

        return items
            .Where(item => !string.IsNullOrWhiteSpace(item.Destination))
            .Select(item => new ResearchJobProgress(item.Destination, item.SuggestionsFound, item.Completed))
            .ToList();
    }

    private sealed record ResearchJobProgressItem(string Destination, int SuggestionsFound, bool Completed);
}
