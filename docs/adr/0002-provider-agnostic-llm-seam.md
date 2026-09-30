# Provider-agnostic LLM seam, opencode Zen by default

The AI-assisted steps (interview extraction, activity shaping) must not be locked to one model vendor, and the operator wants BYOK rather than being tied to Azure/Foundry GPT deployments. We define an **LLM Profile** (`{Provider, BaseUrl, ApiKey, ModelId}`) and register Semantic Kernel's `IChatCompletionService` behind it, defaulting to opencode Zen's OpenAI-compatible endpoint (`https://opencode.ai/zen/v1`) using `chat/completions` models. Keys come from user-secrets in development and Key Vault in production — never committed.

## Considered Options

- **Azure OpenAI via Foundry** — rejected: configuration friction the operator already hit, plus vendor lock-in.
- **Direct provider SDKs** — rejected: each vendor speaks a different protocol (Anthropic Messages, OpenAI Responses), forcing per-protocol connectors up front.

## Consequences

- Initially only OpenAI-compatible `chat/completions` models are selectable. Claude (Anthropic shape) and GPT-5.x (Responses shape) require separate connectors, deferred to a follow-up decision.
- The Seam is honest about provider differences, so swapping to OpenAI, a local endpoint, or Azure OpenAI later is configuration, not code.
