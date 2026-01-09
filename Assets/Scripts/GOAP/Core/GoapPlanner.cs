using System.Collections.Generic;

public class GoapPlanner
{
    public Queue<GoapAction> Plan(
        GoapAgent agent,
        List<GoapAction> actions,
        AgentBeliefs beliefs,
        AgentGoal goal)
    {
        var usable = new List<GoapAction>();

        foreach (var a in actions)
            if (a.CheckProceduralPrecondition(agent))
                usable.Add(a);

        var path = new Queue<GoapAction>();

        foreach (var a in usable)
        {
            if (SatisfiesGoal(a, goal))
            {
                path.Enqueue(a);
                return path;
            }
        }

        return null;
    }

    private bool SatisfiesGoal(GoapAction action, AgentGoal goal)
    {
        if (!action.Effects.ContainsKey(goal.Key))
            return false;

        return action.Effects[goal.Key] == goal.DesiredValue;
    }
}
