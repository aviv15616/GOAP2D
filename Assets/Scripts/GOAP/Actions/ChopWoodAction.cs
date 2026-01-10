using UnityEngine;

public class ChopWoodAction : GoapAction
{
    public int woodPerChop = 1;
    public float chopTime = 1.2f;

    private Vector3 _target;
    private float _t;

    public override bool CanPlan(WorldState s) { return s.woodExists; }
    public override void ApplyPlanEffects(ref WorldState s) { s.woodCarried += woodPerChop; }

    public override bool StartAction(GoapAgent agent)
    {
        var st = agent.FindNearestStation(StationType.Wood);
        if (st == null) return false;

        _target = st.InteractPos;
        _t = 0f;
        return true;
    }

    public override bool Perform(GoapAgent agent, float dt)
    {
        if (!_started)
        {
            _started = true;
            if (!StartAction(agent)) return true;
        }

        if (!agent.mover.MoveTowards(_target, dt)) return false;

        _t += dt;
        if (_t >= chopTime)
        {
            agent.wood += woodPerChop;
            return true;
        }
        return false;
    }

    public override void ResetRuntime()
    {
        base.ResetRuntime();
        _t = 0f;
    }
}
