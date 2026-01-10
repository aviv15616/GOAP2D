using System.Collections.Generic;
using UnityEngine;

public class UseStationAction : GoapAction
{
    public NeedType need;
    public float restoreAmount = 40f;

    [Header("Stop Margin")]
    [Tooltip("How far from station interact pos the NPC may stop.")]
    public float stopDistance = 0.75f;

    private Vector2 _goal;
    private List<Vector2> _path;
    private int _pathIndex;

    private bool _arrived;

    // -------------------------
    // PLANNING
    // -------------------------

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

    public override float EstimateCost(GoapAgent agent, WorldState currentState)
    {
        if (agent == null || agent.nav == null || agent.mover == null)
            return 9999f;

        Station st = need switch
        {
            NeedType.Sleep => agent.FindNearestStation(StationType.Bed),
            NeedType.Hunger => agent.FindNearestStation(StationType.Pot),
            NeedType.Warmth => agent.FindNearestStation(StationType.Fire),
            _ => null
        };

        if (st == null) return 9999f;

        // travel time only
        return agent.nav.EstimatePathTime(
            agent.transform.position,
            st.InteractPos,
            agent.mover.speed
        );
    }

    // -------------------------
    // RUNTIME
    // -------------------------

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
        _pathIndex = 0;

        _path = agent.nav != null
            ? agent.nav.FindPath(agent.transform.position, _goal)
            : null;

        _arrived = false;
        return true;
    }

    public override bool Perform(GoapAgent agent, float dt)
    {
        if (!EnsureStarted(agent))
            return true; // fail-fast skip

        float arrive = Mathf.Max(agent.mover.arriveDistance, stopDistance);

        // 1) MOVE first
        if (!_arrived)
        {
            if (_path != null && _path.Count > 0)
                _arrived = agent.mover.FollowPath(_path, ref _pathIndex, dt, arrive);
            else
                _arrived = agent.mover.MoveTowards(_goal, dt, arrive);

            if (!_arrived)
                return false;

            // arrived -> start "use" timer now
            _elapsed = 0f;
        }

        // 2) WAIT after arrival
        if (!WaitAfterArrival(dt))
            return false;

        // 3) APPLY once
        agent.needs.AddMeter(need, restoreAmount);
        return true;
    }

    public override void ResetRuntime()
    {
        base.ResetRuntime();
        _path = null;
        _pathIndex = 0;
        _arrived = false;
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_path == null || _path.Count < 2)
            return;

        Gizmos.color = Color.cyan;

        for (int i = 0; i < _path.Count - 1; i++)
        {
            Gizmos.DrawLine(_path[i], _path[i + 1]);
        }

        // current target segment
        if (_pathIndex < _path.Count)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_path[_pathIndex], 0.12f);
        }
    }
#endif

}
