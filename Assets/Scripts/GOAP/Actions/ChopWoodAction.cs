using System.Collections.Generic;
using UnityEngine;

public class ChopWoodAction : GoapAction
{
    public int woodPerChop = 1;

    [Header("Stop Margin")]
    [Tooltip("How far from the wood interact pos the NPC may stop.")]
    public float stopDistance = 0.75f;

    private Vector2 _goal;
    private List<Vector2> _path;
    private int _pathIndex;

    private bool _arrived; // move phase completed?

    // -------------------------
    // PLANNING
    // -------------------------

    public override bool CanPlan(WorldState s) => s.woodExists;

    public override void ApplyPlanEffects(ref WorldState s)
    {
        s.woodCarried += woodPerChop;
    }

    public override float EstimateCost(GoapAgent agent, WorldState currentState)
    {
        if (agent == null || agent.nav == null || agent.mover == null)
            return 9999f;

        var st = agent.FindNearestStation(StationType.Wood);
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
        return agent != null && agent.FindNearestStation(StationType.Wood) != null;
    }

    public override bool StartAction(GoapAgent agent)
    {
        var st = agent.FindNearestStation(StationType.Wood);
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
        // start once
        if (!EnsureStarted(agent))
            return true; // fail-fast skip

        float arrive = Mathf.Max(agent.mover.arriveDistance, stopDistance);

        // 1) MOVE first (no waiting before movement)
        if (!_arrived)
        {
            if (_path != null && _path.Count > 0)
            {
                _arrived = agent.mover.FollowPath(_path, ref _pathIndex, dt, arrive);
            }
            else
            {
                _arrived = agent.mover.MoveTowards(_goal, dt, arrive);
            }

            if (!_arrived)
                return false;

            // Just arrived: reset timer so duration measures "chop time"
            _elapsed = 0f;
        }

        // 2) WAIT after arrival (the actual chopping)
        if (!WaitAfterArrival(dt))
            return false;

        // 3) APPLY once (agent will pop action immediately after true)
        agent.wood += woodPerChop;
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
