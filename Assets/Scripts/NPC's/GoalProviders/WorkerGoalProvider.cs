using UnityEngine;

public class WorkerGoalProvider : AgentGoalProvider
{
    public override AgentGoal GetGoal()
    {
        return new AgentGoal("BuildMissingStations", true, 5);
    }
}
