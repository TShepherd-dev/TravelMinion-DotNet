# Markdown as projection, not storage

The Python prototype made a Trip's state durable as human-readable markdown files with YAML frontmatter, and that file interface was also its primary test seam. Moving to a server-side model, we make relational entities the source of truth for a Trip and render markdown **on demand** as an export/agent-facing artifact.

## Consequences

- The readable, diff-able trip document survives as a product affordance, but it is never the store and never the thing tests assert against.
- Tests target the entity model and repositories instead of the file round-trips the Python suite leaned on; a renderer/parser round-trip test replaces the old lossless-file tests.
- Any client that relied on editing files directly must move to the API.

## Status

The storage half of this decision is implemented: relational entities are the source of truth, and Activities/Itinerary are persisted as opaque JSON columns. The markdown **projection itself is not built yet** — there is no renderer/parser or round-trip test in the port. It is deferred and tracked in #2; until then the "render on demand" affordance is aspirational.
