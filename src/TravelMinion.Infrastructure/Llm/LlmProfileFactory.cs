using Microsoft.Extensions.Configuration;
using TravelMinion.Application;

namespace TravelMinion.Infrastructure;

/// <summary>
/// Builds an <see cref="LlmProfile"/> from configuration. Returns <c>null</c>
/// when the profile is not fully configured, so the host can start without an
/// LLM and report the gap rather than crash.
/// </summary>
public static class LlmProfileFactory
{
    public const string SectionName = "Llm";

    public static LlmProfile? FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(SectionName);
        var modelId = section["ModelId"];
        var baseUrl = section["BaseUrl"];
        var apiKey = section["ApiKey"];

        if (string.IsNullOrWhiteSpace(modelId) ||
            string.IsNullOrWhiteSpace(baseUrl) ||
            string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        return new LlmProfile(
            Name: section["Name"] ?? "default",
            Provider: section["Provider"] ?? "openai-compatible",
            BaseUrl: baseUrl,
            ApiKey: apiKey,
            ModelId: modelId);
    }
}
