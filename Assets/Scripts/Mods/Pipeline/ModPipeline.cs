using System.Collections.Generic;

namespace Gridlock.Mods.Pipeline
{
    public class ModPipeline
    {
        private readonly List<(IModStage stage, ModTags tag)> _allStages = new();
        private readonly Dictionary<StagePhase, List<IModStage>> _byPhase = new();
        private ModTags _accumulatedTags;

        public ModTags AccumulatedTags => _accumulatedTags;

        public void AddStage(IModStage stage, ModTags tag)
        {
            _allStages.Add((stage, tag));
            _accumulatedTags |= tag;
            RebuildPhaseMap();
        }

        public void RunPhase(StagePhase phase, ref ModContext ctx)
        {
            if (!_byPhase.TryGetValue(phase, out var stages))
                return;

            for (int i = 0; i < stages.Count; i++)
                stages[i].Execute(ref ctx);
        }

        public ModPipeline Clone()
        {
            var clone = new ModPipeline();
            foreach (var (stage, tag) in _allStages)
                clone._allStages.Add((stage.Clone(), tag));

            clone._accumulatedTags = _accumulatedTags;
            clone.RebuildPhaseMap();
            return clone;
        }

        public ModPipeline CloneExcluding<T>() where T : IModStage
        {
            var clone = new ModPipeline();
            foreach (var (stage, tag) in _allStages)
            {
                if (stage is T) continue;
                clone._allStages.Add((stage.Clone(), tag));
            }
            clone.RebuildPhaseMap();
            foreach (var (_, tag) in clone._allStages)
                clone._accumulatedTags |= tag;
            return clone;
        }

        public ModPipeline CloneExcludingPhase(StagePhase phase)
        {
            var clone = new ModPipeline();
            foreach (var (stage, tag) in _allStages)
            {
                if (stage.Phase == phase) continue;
                clone._allStages.Add((stage.Clone(), tag));
            }
            clone.RebuildPhaseMap();
            foreach (var (_, tag) in clone._allStages)
                clone._accumulatedTags |= tag;
            return clone;
        }

        private void RebuildPhaseMap()
        {
            _byPhase.Clear();
            foreach (var (stage, _) in _allStages)
            {
                if (!_byPhase.TryGetValue(stage.Phase, out var list))
                {
                    list = new List<IModStage>();
                    _byPhase[stage.Phase] = list;
                }
                list.Add(stage);
            }
        }
    }
}
