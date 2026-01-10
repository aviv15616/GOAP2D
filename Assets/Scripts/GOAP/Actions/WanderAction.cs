using UnityEngine;

public class WanderAction : GoapAction
{
    [Header("Wander")]
    public float wanderRadius = 6f;
    public float arriveDistance = 0.2f;
    public float waitAtPointSeconds = 0.6f;

    private Vector3 _target;
    private float _waitT;

    public override bool CanPlan(WorldState s) => true;
    public override void ApplyPlanEffects(ref WorldState s) { }

    public override bool StartAction(GoapAgent agent)
    {
        _waitT = 0f;

        Vector2 basePos = agent.transform.position;
        Vector2 offset = Random.insideUnitCircle * wanderRadius;

        _target = basePos + offset;
        return true;
    }

    public override bool Perform(GoapAgent agent, float dt)
    {
        if (!_started)
        {
            _started = true;
            StartAction(agent);
        }

        bool arrived = agent.mover.MoveTowards(_target, dt, arriveDistance);

        if (!arrived) return false;

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
