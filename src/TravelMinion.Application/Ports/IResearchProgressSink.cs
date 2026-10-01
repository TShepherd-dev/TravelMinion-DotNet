namespace TravelMinion.Application;

/// <summary>
/// The narrow view of Research Progress the <see cref="ResearchEngine"/> needs:
/// a way to announce which stage it has entered. Kept separate from the full
/// reporter so the engine depends only on what it uses.
/// </summary>
public interface IResearchProgressSink
{
    /// <summary>Announces that the engine has entered a stage.</summary>
    void Stage(ResearchStage stage);
}
