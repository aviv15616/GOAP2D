using UnityEngine;

public abstract class GoapAction : MonoBehaviour
{
    [Header("Planning")]
    [Tooltip("Fallback cost if action can't estimate travel time.")]
    public float planCost = 1f;

    public abstract bool CanPlan(WorldState s);
    public abstract void ApplyPlanEffects(ref WorldState s);

    // NEW: planner will call this. Return "seconds".
    public virtual float EstimateCost(GoapAgent agent, WorldState currentState) => planCost;

    // runtime
    public virtual void ResetRuntime() { _started = false; }
    public virtual bool IsStillValid(GoapAgent agent) => true;

    public abstract bool StartAction(GoapAgent agent);
    public abstract bool Perform(GoapAgent agent, float dt);

    protected bool _started;
}
