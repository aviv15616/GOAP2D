// GoapAction.cs (FIXED: supports agent-aware planning effects + old signature)
using UnityEngine;

public abstract class GoapAction : MonoBehaviour
{
    [Header("Planning")]
    [Tooltip("Cost used by the planner (NOT runtime duration).")]
    public float planCost = 1f;

    [Header("Runtime")]
    [Tooltip("How long the *interaction/work* phase takes in seconds (use after arrival).")]
    public float duration = 1f;

    protected bool _started;
    protected float _elapsed;

    // -------------------------
    // PLANNING
    // -------------------------
    public abstract bool CanPlan(WorldState s);

    // ✅ The original abstract method (ALL actions must implement)
    public abstract void ApplyPlanEffects(ref WorldState s);

    // ✅ Optional agent-aware version used by GoapPlanner / GoapAgent for pos simulation etc.
    // Default behavior: just call the ref-only version.
    public virtual void ApplyPlanEffects(GoapAgent agent, ref WorldState s)
    {
        ApplyPlanEffects(ref s);
    }

    // Used by GoapPlanner ONLY
    public virtual float EstimateCost(GoapAgent agent, WorldState currentState) => planCost;

    // -------------------------
    // RUNTIME
    // -------------------------
    public virtual void ResetRuntime()
    {
        _started = false;
        _elapsed = 0f;
    }

    public virtual bool IsStillValid(GoapAgent agent) => true;

    public virtual bool StartAction(GoapAgent agent) => true;

    public virtual bool Perform(GoapAgent agent, float dt)
    {
        if (!_started)
        {
            _started = true;
            _elapsed = 0f;

            if (!StartAction(agent))
                return true; // fail-fast: skip action
        }

        _elapsed += dt;
        return _elapsed >= duration;
    }

    protected bool WaitAfterArrival(float dt)
    {
        _elapsed += dt;
        return _elapsed >= duration;
    }

    protected bool EnsureStarted(GoapAgent agent)
    {
        if (_started) return true;

        _started = true;
        _elapsed = 0f;
        return StartAction(agent);
    }
}
