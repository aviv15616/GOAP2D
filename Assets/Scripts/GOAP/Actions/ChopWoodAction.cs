using System.Collections.Generic;
using UnityEngine;

public class ChopWoodAction : GoapAction
{
    public int woodPerChop = 1;
    public float chopTime = 1.2f;

    private Vector2 _goal;
    private float _t;

    private List<Vector2> _path;
    private int _pathIndex;

    public override bool CanPlan(WorldState s) => s.woodExists;
    public override void ApplyPlanEffects(ref WorldState s) => s.woodCarried += woodPerChop;

    public override float EstimateCost(GoapAgent agent, WorldState currentState)
    {
        var st = agent.FindNearestStation(StationType.Wood);
        if (st == null) return 9999f;

        float speed = Mathf.Max(0.01f, agent.mover.speed);
        float travel = Vector2.Distance(agent.transform.position, st.InteractPos) / speed;
        return travel + chopTime;
    }

    public override bool StartAction(GoapAgent agent)
    {
        var st = agent.FindNearestStation(StationType.Wood);
        if (st == null) return false;

        _goal = st.InteractPos;
        _t = 0f;

        _pathIndex = 0;
        _path = agent.nav != null ? agent.nav.FindPath(agent.transform.position, _goal) : null;

        return true;
    }

    public override bool Perform(GoapAgent agent, float dt)
    {
        if (!_started)
        {
            _started = true;
            if (!StartAction(agent)) return true; // fail => replan
        }

        if (_path != null && _path.Count > 0)
        {
            if (!agent.mover.FollowPath(_path, ref _pathIndex, dt)) return false;
        }
        else
        {
            if (!agent.mover.MoveTowards(_goal, dt)) return false;
        }

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
        _path = null;
        _pathIndex = 0;
    }
}
