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
    private bool _arrived;

    // planning cache (so ApplyPlanEffects can advance simulated pos)
    private Vector2 _cachedPlanTarget;
    private bool _cachedPlanTargetValid;

    // -------------------------
    // PLANNING
    // -------------------------

    public override bool CanPlan(WorldState s) => s.woodExists;

    public override void ApplyPlanEffects(ref WorldState s)
    {
        s.woodCarried += woodPerChop;

        // Advance simulated position to the wood station we chopped at
        if (_cachedPlanTargetValid)
            s.pos = _cachedPlanTarget;
    }

    public override float EstimateCost(GoapAgent agent, WorldState currentState)
    {
        _cachedPlanTargetValid = false;

        if (agent == null) return 9999f;

        // Choose BEST wood by travel time from simulated position
        if (!agent.TryGetBestStationPos(StationType.Wood, currentState.pos, out var bestPos))
            return 9999f;

        _cachedPlanTarget = bestPos;
        _cachedPlanTargetValid = true;

        float travel = agent.EstimateTravelTime(currentState.pos, bestPos);

        // UseStation-style aggregation: travel + duration
        return travel + Mathf.Max(0f, duration);
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
        if (agent == null) return false;

        // Runtime: also pick best-by-travel-time (matches planning)
        if (!agent.TryGetBestStationPos(StationType.Wood, agent.transform.position, out var bestPos))
            return false;

        _goal = bestPos;

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

            _elapsed = 0f; // start chop timer after arrival
        }

        // 2) WAIT after arrival (duration)
        _elapsed += dt;
        if (_elapsed < duration)
            return false;

        // 3) APPLY once
        agent.wood += woodPerChop;
        return true;
    }

    public override void ResetRuntime()
    {
        base.ResetRuntime();
        _path = null;
        _pathIndex = 0;
        _arrived = false;
        _cachedPlanTargetValid = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_path == null || _path.Count < 2)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < _path.Count - 1; i++)
            Gizmos.DrawLine(_path[i], _path[i + 1]);

        if (_pathIndex < _path.Count)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_path[_pathIndex], 0.12f);
        }
    }
#endif
}
