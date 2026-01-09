using UnityEngine;

public class SurvivorGoalProvider : AgentGoalProvider
{
    public override AgentGoal GetGoal()
    {
        // אין אש – עדיפות עליונה
        if (!StationManager.Instance.HasStation("Fire"))
            return new AgentGoal("UseFire", true, 5);

        // צריך מים
        if (!StationManager.Instance.HasStation("Water"))
            return new AgentGoal("UseWater", true, 4);

        // צריך אוכל
        if (!StationManager.Instance.HasStation("Food"))
            return new AgentGoal("UseFood", true, 3);

        // הכל בסדר → Idle
        return new AgentGoal("Idle", true, 1);
    }
}
