using System.Text.Json;
using System.Text.Json.Serialization;
using TravelMinion.Application;

namespace TravelMinion.Infrastructure;

/// <summary>
/// Parses the LLM's JSON response into one or more <see cref="SuggestionEnrichment"/>
/// values, applying the same defaults as the Python prototype when data is
/// missing. Accepts either a JSON array (a list or guide page) or a single
/// object (a single-attraction page).
/// </summary>
internal static class SuggestionEnrichmentParser
{
    internal const string DefaultRationale = "Popular destination attraction";
    internal const string DefaultArea = "City-wide";
    internal const string DefaultDuration = "1-2 hours";

    /// <summary>Upper bound on the activities extracted from a single page.</summary>
    internal const int MaxPerPage = 8;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Parses the model response into named enrichments, falling back to a single
    /// default enrichment named <paramref name="fallbackName"/> on failure.
    /// </summary>
    internal static IReadOnlyList<SuggestionEnrichment> Parse(string? content, string fallbackName)
    {
        var json = ExtractJson(content);
        if (json is null)
        {
            return [Fallback(fallbackName)];
        }

        try
        {
            if (json[0] == '[')
            {
                var dtos = JsonSerializer.Deserialize<List<EnrichmentDto>>(json, SerializerOptions);
                if (dtos is null)
                {
                    return [Fallback(fallbackName)];
                }

                // An empty array is a deliberate "no attractions here", not a failure.
                return dtos.Take(MaxPerPage).Select(dto => Map(dto, fallbackName)).ToList();
            }

            var single = JsonSerializer.Deserialize<EnrichmentDto>(json, SerializerOptions);
            return single is null ? [Fallback(fallbackName)] : [Map(single, fallbackName)];
        }
        catch (JsonException)
        {
            return [Fallback(fallbackName)];
        }
    }

    private static SuggestionEnrichment Map(EnrichmentDto dto, string fallbackName) =>
        new(
            ValueOrDefault(dto.Name, fallbackName),
            ValueOrDefault(dto.Rationale, DefaultRationale),
            ValueOrDefault(dto.Area, DefaultArea),
            ValueOrDefault(dto.TypicalDuration, DefaultDuration),
            Clean(dto.OpeningHours),
            Clean(dto.ApproximateCost),
            Clean(dto.SeasonWeatherFit));

    private static SuggestionEnrichment Fallback(string fallbackName) =>
        new(fallbackName, DefaultRationale, DefaultArea, DefaultDuration);

    private static string? ExtractJson(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var objectStart = content.IndexOf('{');
        var arrayStart = content.IndexOf('[');

        int start;
        char close;
        if (arrayStart >= 0 && (objectStart < 0 || arrayStart < objectStart))
        {
            start = arrayStart;
            close = ']';
        }
        else if (objectStart >= 0)
        {
            start = objectStart;
            close = '}';
        }
        else
        {
            return null;
        }

        var end = content.LastIndexOf(close);
        return end > start ? content[start..(end + 1)] : null;
    }

    private static string ValueOrDefault(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class EnrichmentDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

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
