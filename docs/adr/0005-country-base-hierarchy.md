# Geography is a Country → Base hierarchy

The Python prototype and the first port modeled a Trip's geography as a flat list of stops whose single `destination` string conflated city and country, so a multi-country trip with a country-level time budget and a chain of overnight bases could not be represented. We model geography as an ordered chain of **Countries**, each owning an ordered chain of **Bases** (a city or town where the traveller is based; accommodation implied), with the Trip's **Arrival** and **Departure** recorded as endpoint Bases and the day counts summing at each level (Base→Country, Country→Trip). Country is first-class because the traveller specifies a span per country *and* a chain of bases within it; collapsing country to a label on each base would lose the country-level span as an input.

## Considered Options

- **Country as a label on each Base** — rejected: the country span becomes a derived sum, so it cannot be captured as the traveller's input, and cross-country grouping has nowhere to live.
- **Keep the flat stop list** — rejected: it cannot express the base/excursion relationship or country grouping at all; that conflation is exactly what we are removing.

## Consequences

- Days are now specified at two levels. Invariant 3 (`docs/domain-invariants.md`) becomes a two-level sum, and Country and Base names must be unique within a Trip (activities are keyed by Base name).
- Activities gain an excursion flag distinguishing "at the base" from "a day-trip from the base"; research and planning iterate Country → Base.
- The existing stop entity and its table are replaced; existing trips are dev-only and are not migrated.
