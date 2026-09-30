using System.Text.Json;
using TravelMinion.Domain;

namespace TravelMinion.Infrastructure.Persistence;

/// <summary>
/// JSON (de)serialization for the Approved Activity List. The list is a
/// whole-aggregate value (read and replaced as a unit), so it is persisted as a
/// single column rather than as a set of related tables.
/// </summary>
internal static class ActivityListSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize(ApprovedActivityList list)
        => JsonSerializer.Serialize(list.Activities, Options);

    public static ApprovedActivityList Deserialize(string json)
        => new(JsonSerializer.Deserialize<List<ApprovedActivity>>(json, Options) ?? new List<ApprovedActivity>());
}
