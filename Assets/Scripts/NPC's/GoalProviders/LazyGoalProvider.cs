using UnityEngine;

public class LazyGoalProvider : AgentGoalProvider
{
    public override AgentGoal GetGoal()
    {
        // אם stamina נמוכה – הוא ישן
        var agent = GetComponent<GoapAgent>();
        if (agent.beliefs.HasState("LowStamina"))
            return new AgentGoal("Sleep", true, 10);

        // בדיקות תחנות בקיצוניות
        if (!StationManager.Instance.HasStation("Fire"))
            return new AgentGoal("UseFire", true, 5);

        if (!StationManager.Instance.HasStation("Water"))
            return new AgentGoal("UseWater", true, 3);

        if (!StationManager.Instance.HasStation("Food"))
            return new AgentGoal("UseFood", true, 2);

        return new AgentGoal("Idle", true, 1);
    }
}
