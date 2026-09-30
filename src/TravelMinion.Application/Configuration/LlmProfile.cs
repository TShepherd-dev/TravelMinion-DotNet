namespace TravelMinion.Application;

/// <summary>
/// A named, selectable model configuration for the AI-assisted steps.
/// Selects which model performs extraction and orchestration.
/// </summary>
/// <param name="Name">Human-readable profile name (e.g. "zen-deepseek").</param>
/// <param name="Provider">Provider identifier (e.g. "opencode-zen").</param>
/// <param name="BaseUrl">OpenAI-compatible base URL (e.g. https://opencode.ai/zen/v1).</param>
/// <param name="ApiKey">Secret API key. Never returned to clients.</param>
/// <param name="ModelId">Model identifier (e.g. deepseek-v4.1-flash).</param>
public sealed record LlmProfile(
    string Name,
    string Provider,
    string BaseUrl,
    string ApiKey,
    string ModelId);
