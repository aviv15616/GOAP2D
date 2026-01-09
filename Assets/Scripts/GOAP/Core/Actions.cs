using System.Collections.Generic;
using UnityEngine;

public abstract class GoapAction : MonoBehaviour
{
    public string ActionName;
    public float Cost = 1f;

    public Dictionary<string, bool> Preconditions = new Dictionary<string, bool>();
    public Dictionary<string, bool> Effects = new Dictionary<string, bool>();

    public bool IsRunning { get; protected set; }

    public virtual bool CheckProceduralPrecondition(GoapAgent agent) { return true; }

    public virtual void DoReset()
    {
        IsRunning = false;
    }

    public bool SatisfiesGoal(AgentGoal goal, GoapAgent agent)
    {
        if (goal == null) return false;

        if (!Effects.ContainsKey(goal.Key))
        {
            if (agent != null && agent.debug)
                Debug.Log($"[GOAL_MISS] {agent.name}:{GetType().Name} has no effect key '{goal.Key}'");
            return false;
        }

        bool matches = Effects[goal.Key] == goal.DesiredValue;
        if (!matches && agent != null && agent.debug)
            Debug.Log($"[GOAL_MISMATCH] {agent.name}:{GetType().Name} effect {goal.Key}={Effects[goal.Key]} but goal wants {goal.DesiredValue}");

        return matches;
    }

    public abstract void Perform(GoapAgent agent);
}
