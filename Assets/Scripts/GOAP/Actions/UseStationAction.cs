using UnityEngine;

public class UseStationAction : GoapAction
{
    public NeedType need;
    public float useTime = 1.5f;
    public float restoreAmount = 40f;

    private Vector3 _target;
    private float _t;

    public override bool CanPlan(WorldState s)
    {
        if (need == NeedType.Sleep) return s.bedExists;
        if (need == NeedType.Hunger) return s.potExists;
        if (need == NeedType.Warmth) return s.fireExists;
        return false;
    }

    public override void ApplyPlanEffects(ref WorldState s)
    {
        if (need == NeedType.Sleep) s.sleepSatisfied = true;
        else if (need == NeedType.Hunger) s.hungerSatisfied = true;
        else if (need == NeedType.Warmth) s.warmthSatisfied = true;
    }

    public override bool IsStillValid(GoapAgent agent)
    {
        StationType t = StationType.Bed;
        if (need == NeedType.Sleep) t = StationType.Bed;
        else if (need == NeedType.Hunger) t = StationType.Pot;
        else if (need == NeedType.Warmth) t = StationType.Fire;

        return agent.FindNearestStation(t) != null;
    }

    public override bool StartAction(GoapAgent agent)
    {
        StationType t = StationType.Bed;
        if (need == NeedType.Sleep) t = StationType.Bed;
        else if (need == NeedType.Hunger) t = StationType.Pot;
        else if (need == NeedType.Warmth) t = StationType.Fire;

        var st = agent.FindNearestStation(t);
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
    }
}
