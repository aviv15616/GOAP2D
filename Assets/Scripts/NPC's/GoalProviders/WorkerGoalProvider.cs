// Assets/Scripts/NPC's/GoalProviders/WorkerGoalProvider.cs
using UnityEngine;

public class WorkerGoalProvider : AgentGoalProvider
{
    private GoapAgent agent;

    private void Awake() => agent = GetComponent<GoapAgent>();

    public override AgentGoal GetGoal()
    {
        if (agent == null) agent = GetComponent<GoapAgent>();
        if (agent == null) return null;

        if (agent.Stamina < 20f) return new AgentGoal("LowStamina", false, 100);

        // get wood if none
        if (agent.InventoryWood <= 0) return new AgentGoal("HasWood", true, 50);

        // build Water first
        if (StationManager.Instance != null && !StationManager.Instance.HasStation("Water"))
            return new AgentGoal("HasStation_Water", true, 40);

        return null;
    }
}
