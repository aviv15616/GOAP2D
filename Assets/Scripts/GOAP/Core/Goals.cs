public enum GoalType { RestoreSleep, RestoreHunger, RestoreWarmth }

public interface IGoal
{
    bool IsSatisfied(WorldState s);
}

public static class Goals
{
    public static GoalType ChooseGoal(Beliefs b, GoalType primary)
    {
        // 1) critical overrides personality
        if (b.Get(BeliefKey.IsStarving)) return GoalType.RestoreHunger;
        if (b.Get(BeliefKey.IsFreezing)) return GoalType.RestoreWarmth;
        if (b.Get(BeliefKey.IsExhausted)) return GoalType.RestoreSleep;

        // 2) otherwise: if primary need is active, take it
        if (primary == GoalType.RestoreSleep && b.Get(BeliefKey.IsTired)) return primary;
        if (primary == GoalType.RestoreHunger && b.Get(BeliefKey.IsHungry)) return primary;
        if (primary == GoalType.RestoreWarmth && b.Get(BeliefKey.IsCold)) return primary;

        // 3) fallback: any non-critical need
        if (b.Get(BeliefKey.IsHungry)) return GoalType.RestoreHunger;
        if (b.Get(BeliefKey.IsCold)) return GoalType.RestoreWarmth;
        if (b.Get(BeliefKey.IsTired)) return GoalType.RestoreSleep;

        // 4) nothing urgent
        return primary;
    }

    // ✅ planner goal: "the chosen need is satisfied"
    public class GoalSatisfied : IGoal
    {
        private readonly NeedType _need;

        public GoalSatisfied(NeedType need)
        {
            _need = need;
        }

        public bool IsSatisfied(WorldState s)
        {
            return _need switch
            {
                NeedType.Sleep => s.sleepSatisfied,
                NeedType.Hunger => s.hungerSatisfied,
                NeedType.Warmth => s.warmthSatisfied,
                _ => false
            };
        }
    }
}
