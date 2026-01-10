using UnityEngine;

public class WanderAction : GoapAction
{
    [Header("Wander")]
    public float wanderRadius = 6f;
    public float arriveDistance = 0.2f;
    public float waitAtPointSeconds = 0.6f;

    private Vector3 _target;
    private float _waitT;

    public override bool CanPlan(WorldState s)
    {
        // Always allowed (idle fallback)
        return true;
    }

    public override void ApplyPlanEffects(ref WorldState s)
    {
        // No world-state changes
    }

    public override bool StartAction(GoapAgent agent)
    {
        _waitT = 0f;

        // pick a random point near the agent
        Vector2 basePos = agent.transform.position;
        Vector2 candidate = basePos + Random.insideUnitCircle * wanderRadius;

        // if you have BuildValidator with tilemap-bounds check, reuse it to keep wander inside the map
        if (agent.buildValidator != null)
        {
            if (!agent.buildValidator.TryFindValidPosition(candidate, out var found))
            {
                // fallback: just stay
                _target = agent.transform.position;
                return true;
            }

            _target = new Vector3(found.x, found.y, agent.transform.position.z);
            return true;
        }

        // fallback: no validator -> free wander
        _target = new Vector3(candidate.x, candidate.y, agent.transform.position.z);
        return true;
    }

    public override bool Perform(GoapAgent agent, float dt)
    {
        if (!_started)
        {
            _started = true;
            if (!StartAction(agent)) return true;
        }

        // move
        bool arrived = agent.mover.MoveTowards(_target, dt);

        if (!arrived) return false;

        // wait a bit then finish (so planner can pick it again)
        _waitT += dt;
        return _waitT >= waitAtPointSeconds;
    }

    public override void ResetRuntime()
    {
        base.ResetRuntime();
        _waitT = 0f;
        _target = Vector3.zero;
    }
}
