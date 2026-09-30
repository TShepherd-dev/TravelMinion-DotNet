namespace TravelMinion.Infrastructure;

/// <summary>Configuration for the Tavily research source.</summary>
public sealed class TavilyOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Tavily";

    /// <summary>Tavily search endpoint. The prototype's legacy /v1/search path is
    /// retired (404); the current API is POST /search.</summary>
    public string Endpoint { get; set; } = "https://api.tavily.com/search";

    /// <summary>API key. Supplied via user-secrets (dev) or Key Vault (prod).</summary>
    public string ApiKey { get; set; } = string.Empty;
}
