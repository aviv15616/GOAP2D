// GoapAction.cs (FIXED: supports "move first, then wait" cleanly for derived actions)
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
    public abstract void ApplyPlanEffects(ref WorldState s);

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

    /// <summary>
    /// Called once when the action begins.
    /// Return false to fail-fast (agent will skip this action and replan).
    /// </summary>
    public virtual bool StartAction(GoapAgent agent) => true;

    /// <summary>
    /// Default Perform: just waits duration seconds.
    /// Most concrete actions should override Perform to "move first, then wait".
    /// </summary>
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

    /// <summary>
    /// Helper for derived actions: call this only AFTER arrival.
    /// Returns true when wait time has completed.
    /// </summary>
    protected bool WaitAfterArrival(float dt)
    {
        _elapsed += dt;
        return _elapsed >= duration;
    }

    /// <summary>
    /// Helper for derived actions: call this on first frame to initialize.
    /// Returns false if StartAction failed.
    /// </summary>
    protected bool EnsureStarted(GoapAgent agent)
    {
        if (_started) return true;

        _started = true;
        _elapsed = 0f;
        return StartAction(agent);
    }
}
