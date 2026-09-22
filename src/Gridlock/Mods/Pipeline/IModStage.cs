namespace Gridlock.Mods.Pipeline
{
    public interface IModStage
    {
        StagePhase Phase { get; }
        void Execute(ref ModContext ctx);
        IModStage Clone();
    }
}
