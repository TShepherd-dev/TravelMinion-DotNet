using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using TravelMinion.Application;

namespace TravelMinion.Infrastructure;

/// <summary>
/// LLM-backed enricher. Replaces the Python prototype's regex heuristics for
/// rationale, area, duration, hours, cost and season fit with a single
/// structured-output call through Semantic Kernel.
/// </summary>
public sealed class SemanticKernelResearchEnricher : IResearchEnricher
{
    internal const int MaxContentLength = 1500;
    internal const int MaxOutputTokens = 400;

    internal const string SystemPrompt =
        "You are a travel research assistant. Given one raw web search result, " +
        "extract structured attraction details for a traveller's itinerary. " +
        "Respond with ONLY a JSON object, no prose, no markdown, no explanation, " +
        "using exactly these keys: " +
        "{\"rationale\": string, \"area\": string, \"typicalDuration\": string, " +
        "\"openingHours\": string|null, \"approximateCost\": string|null, \"seasonWeatherFit\": string|null}. " +
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
    public async Task<SuggestionEnrichment> EnrichAsync(
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

        return SuggestionEnrichmentParser.Parse(response.Content);
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
