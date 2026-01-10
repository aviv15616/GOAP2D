using System.Collections.Generic;
using UnityEngine;

public class UseStationAction : GoapAction
{
    public NeedType need;
    public float useTime = 1.5f;
    public float restoreAmount = 40f;

    private Vector2 _goal;
    private float _t;

    private List<Vector2> _path;
    private int _pathIndex;

    public override bool CanPlan(WorldState s)
    {
        return need switch
        {
            NeedType.Sleep => s.bedExists,
            NeedType.Hunger => s.potExists,
            NeedType.Warmth => s.fireExists,
            _ => false
        };
    }

    public override void ApplyPlanEffects(ref WorldState s)
    {
        if (need == NeedType.Sleep) s.sleepSatisfied = true;
        if (need == NeedType.Hunger) s.hungerSatisfied = true;
        if (need == NeedType.Warmth) s.warmthSatisfied = true;
    }

    public override bool IsStillValid(GoapAgent agent)
    {
        return need switch
        {
            NeedType.Sleep => agent.FindNearestStation(StationType.Bed) != null,
            NeedType.Hunger => agent.FindNearestStation(StationType.Pot) != null,
            NeedType.Warmth => agent.FindNearestStation(StationType.Fire) != null,
            _ => false
        };
    }

    public override float EstimateCost(GoapAgent agent, WorldState currentState)
    {
        Station st = need switch
        {
            NeedType.Sleep => agent.FindNearestStation(StationType.Bed),
            NeedType.Hunger => agent.FindNearestStation(StationType.Pot),
            NeedType.Warmth => agent.FindNearestStation(StationType.Fire),
            _ => null
        };
        if (st == null) return 9999f;

        float speed = Mathf.Max(0.01f, agent.mover.speed);
        float travel = Vector2.Distance(agent.transform.position, st.InteractPos) / speed;
        return travel + useTime;
    }

    public override bool StartAction(GoapAgent agent)
    {
        Station st = need switch
        {
            NeedType.Sleep => agent.FindNearestStation(StationType.Bed),
            NeedType.Hunger => agent.FindNearestStation(StationType.Pot),
            NeedType.Warmth => agent.FindNearestStation(StationType.Fire),
            _ => null
        };
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
            if (!StartAction(agent)) return true;
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
        if (_t >= useTime)
        {
            agent.needs.AddMeter(need, restoreAmount);
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
