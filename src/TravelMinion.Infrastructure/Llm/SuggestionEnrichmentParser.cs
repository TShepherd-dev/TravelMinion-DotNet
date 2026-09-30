using System.Text.Json;
using System.Text.Json.Serialization;
using TravelMinion.Application;

namespace TravelMinion.Infrastructure;

/// <summary>
/// Parses the LLM's JSON response into a <see cref="SuggestionEnrichment"/>,
/// applying the same defaults as the Python prototype when data is missing.
/// </summary>
internal static class SuggestionEnrichmentParser
{
    internal const string DefaultRationale = "Popular destination attraction";
    internal const string DefaultArea = "City-wide";
    internal const string DefaultDuration = "1-2 hours";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Parses the model response, falling back to defaults on failure.</summary>
    internal static SuggestionEnrichment Parse(string? content)
    {
        var json = ExtractJson(content);
        if (json is null)
        {
            return Fallback();
        }

        try
        {
            var dto = JsonSerializer.Deserialize<EnrichmentDto>(json, SerializerOptions);
            if (dto is null)
            {
                return Fallback();
            }

            return new SuggestionEnrichment(
                ValueOrDefault(dto.Rationale, DefaultRationale),
                ValueOrDefault(dto.Area, DefaultArea),
                ValueOrDefault(dto.TypicalDuration, DefaultDuration),
                Clean(dto.OpeningHours),
                Clean(dto.ApproximateCost),
                Clean(dto.SeasonWeatherFit));
        }
        catch (JsonException)
        {
            return Fallback();
        }
    }

    private static SuggestionEnrichment Fallback() => new(DefaultRationale, DefaultArea, DefaultDuration);

    private static string? ExtractJson(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        return start >= 0 && end > start ? content[start..(end + 1)] : null;
    }

    private static string ValueOrDefault(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class EnrichmentDto
    {
        [JsonPropertyName("rationale")]
        public string? Rationale { get; set; }

        [JsonPropertyName("area")]
        public string? Area { get; set; }

        [JsonPropertyName("typicalDuration")]
        public string? TypicalDuration { get; set; }

        [JsonPropertyName("openingHours")]
        public string? OpeningHours { get; set; }

        [JsonPropertyName("approximateCost")]
        public string? ApproximateCost { get; set; }

        [JsonPropertyName("seasonWeatherFit")]
        public string? SeasonWeatherFit { get; set; }
    }
}
