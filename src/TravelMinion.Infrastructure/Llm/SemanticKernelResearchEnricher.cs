using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using TravelMinion.Application;

namespace TravelMinion.Infrastructure;

/// <summary>
/// LLM-backed enricher. Replaces the Python prototype's regex heuristics for
/// rationale, area, duration, hours, cost and season fit with a single
/// structured-output call through Semantic Kernel. A page may describe one
/// attraction, several (a list or guide), or none; the model returns one entry
/// per attraction it can ground in the page.
/// </summary>
public sealed class SemanticKernelResearchEnricher : IResearchEnricher
{
    internal const int MaxContentLength = 5000;
    internal const int MaxOutputTokens = 1500;

    internal const string SystemPrompt =
        "You are a travel research assistant. Given one raw web search result, " +
        "extract the attraction(s) it describes for a traveller's itinerary. " +
        "If the page covers a single attraction, return an array with one object. " +
        "If the page is a list or guide covering several attractions, return one object per attraction, up to eight. " +
        "If the page is not about specific attractions, return an empty array. " +
        "Respond with ONLY a JSON array, no prose, no markdown, no explanation. " +
        "Each element uses exactly these keys: " +
        "{\"name\": string, \"rationale\": string, \"area\": string, \"typicalDuration\": string, " +
        "\"openingHours\": string|null, \"approximateCost\": string|null, \"seasonWeatherFit\": string|null}. " +
        "name is the attraction's proper name. " +
        "Keep every value under 100 characters. " +
        "rationale is one short sentence tying the attraction to the traveller's stated interests. " +
        "area is the neighbourhood or district, or \"City-wide\" if unknown. " +
        "typicalDuration is a short range like \"2-3 hours\". " +
        "Use null for any field you cannot ground in the provided text.";

    private readonly IChatCompletionService _chatCompletion;

    /// <summary>Creates the enricher over a Semantic Kernel chat completion service.</summary>
    public SemanticKernelResearchEnricher(IChatCompletionService chatCompletion)
    {
        ArgumentNullException.ThrowIfNull(chatCompletion);
        _chatCompletion = chatCompletion;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SuggestionEnrichment>> EnrichAsync(
        RawResult raw,
        IReadOnlyList<string> interests,
        string destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(raw);

        var history = new ChatHistory();
        history.AddSystemMessage(SystemPrompt);
        history.AddUserMessage(BuildPrompt(raw, interests, destination));

        var settings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = MaxOutputTokens,
            Temperature = 0,
        };

        var response = await _chatCompletion
            .GetChatMessageContentAsync(history, settings, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return SuggestionEnrichmentParser.Parse(response.Content, raw.Title);
    }

    /// <summary>Builds the user prompt for a single raw result.</summary>
    internal static string BuildPrompt(RawResult raw, IReadOnlyList<string> interests, string destination)
    {
        var content = raw.Content is { Length: > MaxContentLength }
            ? raw.Content[..MaxContentLength]
            : raw.Content;

        return $"""
            Destination: {destination}
            Traveller interests: {string.Join(", ", interests)}
            Source: {raw.SourceName}
            Title: {raw.Title}
            Snippet: {raw.Snippet}
            Page content: {content}
            """;
    }
}
