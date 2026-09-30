namespace TravelMinion.Domain;

/// <summary>
/// Well-known default values used across the domain.
/// </summary>
public static class DomainDefaults
{
    /// <summary>
    /// Interests applied when a traveller does not supply any.
    /// </summary>
    public static IReadOnlyList<string> Interests { get; } = new[]
    {
        "local culture",
        "food and dining",
        "landmarks",
        "nature",
        "history",
    };
}
