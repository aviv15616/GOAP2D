using UnityEngine;

public class LazyGoalProvider : AgentGoalProvider
{
    public override AgentGoal GetGoal()
    {
        var agent = GetComponent<GoapAgent>();
        if (agent == null) return null;

        if (agent.beliefs.HasState("LowStamina"))
            return new AgentGoal("LowStamina", false, priority: 10);

        return new AgentGoal("Idle", true, priority: 1);
    }
}
