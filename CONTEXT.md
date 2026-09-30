# TravelMinion

A service that live-researches a trip's destinations against a traveller's goals, proposes a human-reviewed activity list, and plans that list into a day-by-day itinerary. This is the C#/.NET port of the Python TravelMinion prototype, re-shaped from a stateless folder-native skill into a server-side application.

## Language

### Trip lifecycle

**TravelMinion**:
The system as a whole: the service that researches, plans, and (eventually) calendar-publishes a trip.
_Avoid_: TravelMinon (old spelling)

**Trip**:
One traveller's trip, and the aggregate root that owns everything about it — its Trip Brief, Research Jobs, Approved Activity List, and Itinerary.
_Avoid_: Trip folder, workspace, project

**Trip Brief**:
The persisted capture of the clarifying interview — destinations, dates, interests, travel style, and other inputs.

**Destination Stop**:
A single destination within a Trip, carrying its position in the itinerary order, its day count, and an optional transit from the previous stop.

**Research Step**:
The phase that turns a Trip Brief into Suggestions by live web research.

**Research Job**:
A single execution of the Research Step for a Trip. Runs asynchronously and produces the Trip's Suggestions.

**Suggestion**:
A single researched attraction/activity candidate, carrying details (hours, cost, duration, area, season/weather fit, rationale).
_Avoid_: Item, result, hit

**Approved Activity List**:
The human-owned, living list for a Trip. Formed by the traveller approving/editing Suggestions and adding their own items. The sole source for itinerary planning. May change over time, forcing an itinerary Rebuild.
_Avoid_: Final list, chosen list

**Approved Activity**:
A single entry in a Trip's Approved Activity List. May originate from a Suggestion or be added by the traveller, and carries the traveller's curation (`approved`, `optional`, notes).

**Itinerary**:
A time-blocked day-by-day plan built from the Approved Activity List, organized as Activity Days, Travel Days, and Free Days.
_Avoid_: Schedule, plan

**Activity Day**:
An itinerary day with time-blocked activities (start/end, place, duration, transit-to-next).

**Travel Day**:
An itinerary day devoted to moving between destinations.

**Free Day**:
An itinerary day with no planned activities (the "nothing" travel style), or recovery after long travel.

**Travel Leg**:
A single move between two destinations, with a mode and duration, embedded as a block inside a day rather than a standalone day.

**Travel Style**:
The traveller's desired daily density (packed / casual / nothing), mapped to a target number of daily time-blocks.

**Rebuild**:
Re-generating a Trip's Itinerary after its Approved Activity List changes.

### Configuration

**LLM Profile**:
A named, traveller- or operator-selectable model configuration for the AI-assisted steps (provider, endpoint, key, model id). Selects which model performs extraction and orchestration.

### Deferred

**Per-trip Calendar**:
A dedicated Google Calendar created for a Trip and shared read-only with the travellers. Distinct from a traveller's primary calendar. Shelved: the Google integration is deliberately out of scope for the initial port.
_Avoid_: Shared calendar, primary calendar
