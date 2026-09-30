namespace TravelMinion.Infrastructure;

/// <summary>Configuration for the Jina AI Reader URL fetcher.</summary>
public sealed class JinaOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Jina";

    /// <summary>Reader base URL; the target URL is appended directly.</summary>
    public string BaseUrl { get; set; } = "https://r.jina.ai/";
}
