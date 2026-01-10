using UnityEngine;

public abstract class GoapAction : MonoBehaviour
{
    [Header("Planning (static cost)")]
    public float planCost = 1f;

    public abstract bool CanPlan(WorldState s);
    public abstract void ApplyPlanEffects(ref WorldState s);

    public virtual void ResetRuntime() { _started = false; }
    public virtual bool IsStillValid(GoapAgent agent) { return true; }

    public abstract bool StartAction(GoapAgent agent);
    public abstract bool Perform(GoapAgent agent, float dt);

    protected bool _started;
}
