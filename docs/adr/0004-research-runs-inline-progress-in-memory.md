# Research runs inline; progress is an in-memory, per-circuit signal

The Research Step runs inline on the Blazor circuit request that starts it, and its progress is surfaced through an in-memory signal scoped to that circuit rather than by persisting intermediate Research Job state. A background worker with a durable queue would add a host, a queue, and a migration for no benefit while the app is single-user and the run is already synchronous; the cost is that progress is not durable (a process restart loses it) and a run is tied to the circuit that started it. Deferred: a background worker with persisted, pollable progress.

## Considered Options

- **Persist each status transition and per-destination progress, and poll the database** — rejected for now: `ResearchService` has no repository dependency and `ResearchRunner` saves once at the end, so this means new writes mid-run plus a polling loop, for a single-user app.
- **A background `IHostedService` with a `Channel<T>` queue and SignalR** — rejected for now: the largest change, and it buys durability the app does not yet need.

## Consequences

- A Research Job's `Running` state and per-destination progress are observable only while the run is in flight; they are not written to the database until the run finishes.
- Cancelling flushes the Suggestions found so far and records the Job as `Cancelled`; failing discards the in-flight preview and records the `FailureReason`.
- Two browser tabs are independent; the last save wins, as before.
