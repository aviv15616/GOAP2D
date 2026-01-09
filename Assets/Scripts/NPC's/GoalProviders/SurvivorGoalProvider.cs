// Assets/Scripts/NPC's/GoalProviders/SurvivorGoalProvider.cs
using UnityEngine;

public class SurvivorGoalProvider : AgentGoalProvider
{
    private GoapAgent agent;

    private void Awake() => agent = GetComponent<GoapAgent>();

    public override AgentGoal GetGoal()
    {
        if (agent == null) agent = GetComponent<GoapAgent>();
        if (agent == null) return null;

        if (agent.Stamina < 20f) return new AgentGoal("LowStamina", false, 100);

        if (agent.InventoryWood <= 0) return new AgentGoal("HasWood", true, 60);

        // survivor prefers Food then Fire
        if (StationManager.Instance != null && !StationManager.Instance.HasStation("Food"))
            return new AgentGoal("HasStation_Food", true, 50);

        if (StationManager.Instance != null && !StationManager.Instance.HasStation("Fire"))
            return new AgentGoal("HasStation_Fire", true, 40);

        return null;
    }
}
