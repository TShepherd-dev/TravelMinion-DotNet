# Domain Invariants

Rules the domain must always hold true, carried over from the Python prototype. These are not a glossary (see `CONTEXT.md`) and not decisions (see `docs/adr/`); they are constraints the model and its tests must enforce.

1. **The Approved Activity List is the sole planning input.** Itinerary generation reads a Trip's Approved Activity List and nothing else; Suggestions never feed planning directly.
2. **Must-dos schedule before fillers.** Within a destination, activities with `optional = false` are placed before those with `optional = true` when daily capacity is limited.
3. **Destination days must sum to the trip span.** A Trip Brief's `DestinationStop.days` across all stops must equal the number of days between `start_date` and `end_date` (inclusive); when they don't, days are redistributed evenly with the remainder going to earlier stops.
4. **A long-haul Travel Leg becomes a Free Day.** A Travel Leg at or over the long-haul threshold (6 hours) yields a recovery Free Day rather than a Travel Day with an afternoon activity.
