namespace TravelMinion.Domain;

/// <summary>
/// A traveller's desired daily density, mapped by the planner to a target
/// number of daily time-blocks (packed ~5-6, casual ~2-3, nothing ~0-1).
/// </summary>
public enum TravelStyle
{
    /// <summary>Relaxed pace; a small number of activities per day.</summary>
    Casual,

    /// <summary>Full days; many activities per day.</summary>
    Packed,

    /// <summary>Mostly rest and free days.</summary>
    Nothing,
}
