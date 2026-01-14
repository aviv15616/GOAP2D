using System.Collections.Generic;
using UnityEngine;

public class UseStationAction : GoapAction
{
    public NeedType need = NeedType.Sleep;
    public float restoreAmount = 35f;
    public float stopDistance = 0.25f;

    private Vector2 _goal;
    private List<Vector2> _path;
    private int _pathIndex;
    private bool _arrived;
    public Vector2 RuntimeGoal => _goal;
    public StationType RuntimeStationType => NeedStationType();
    public bool RuntimeHasGoal => _goal != Vector2.zero; // מספיק טוב אצלך כי _goal מתמלא ב-StartAction

    private StationType NeedStationType() =>
        need switch
        {
            NeedType.Sleep => StationType.Bed,
            NeedType.Hunger => StationType.Pot,
            NeedType.Warmth => StationType.Fire,
            _ => StationType.Bed,
        };

    public override bool CanPlan(WorldState s)
    {
        // UseStation only makes sense if station "exists" in snapshot
        return need switch
        {
            NeedType.Sleep => s.bedExists,
            NeedType.Hunger => s.potExists,
            NeedType.Warmth => s.fireExists,
            _ => false,
        };
    }

    public override float EstimateCost(GoapAgent agent, WorldState currentState)
    {
        if (agent == null)
            return 9999f;

        StationType stType = NeedStationType();

        // Try resolve a REAL station (from registry)
        if (agent.TryGetBestStationPos(stType, currentState.pos, out var bestPos))
        {
            float arrive =
                (agent.mover != null)
                    ? Mathf.Max(agent.mover.arriveDistance, stopDistance)
                    : Mathf.Max(0.15f, stopDistance);

            float travel = agent.EstimateTravelTime(currentState.pos, bestPos, arrive);

            // ✅ match runtime: travel + interaction duration (NO planCost)
            return travel + Mathf.Max(0f, duration);
        }

        // Simulated station case (planner says it "exists" in snapshot)
        if (CanPlan(currentState))
        {
            // ✅ match runtime: just interaction duration (NO planCost)
            return Mathf.Max(0f, duration);
        }

        return 9999f;
    }

    public override void ApplyPlanEffects(GoapAgent agent, ref WorldState s)
    {
        StationType stType = NeedStationType();

        if (agent != null && agent.TryGetBestStationPos(stType, s.pos, out var bestPos))
        {
            // ✅ simulate that we walked there
            s.pos = bestPos;
        }

        // Satisfy the relevant need in the simulated world
        if (need == NeedType.Sleep)
            s.sleepSatisfied = true;
        if (need == NeedType.Hunger)
            s.hungerSatisfied = true;
        if (need == NeedType.Warmth)
            s.warmthSatisfied = true;
    }

    public override bool IsStillValid(GoapAgent agent)
    {
        if (agent == null)
            return false;
        return agent.FindNearestStation(NeedStationType()) != null;
    }

    public override bool StartAction(GoapAgent agent)
    {
        if (agent == null)
            return false;

        StationType stType = NeedStationType();

        // Runtime: best-by-travel-time from REAL position
        if (!agent.TryGetBestStationPos(stType, agent.transform.position, out var bestPos))
            return false;

        _goal = bestPos;
        _pathIndex = 0;
        _path = agent.nav != null ? agent.nav.FindPath(agent.transform.position, _goal) : null;
        _arrived = false;

        return true;
    }

    public override void ApplyPlanEffects(ref WorldState s)
    {
        // Minimal effects (planner might call agent-aware version, but this must exist)
        if (need == NeedType.Sleep)
            s.sleepSatisfied = true;
        if (need == NeedType.Hunger)
            s.hungerSatisfied = true;
        if (need == NeedType.Warmth)
            s.warmthSatisfied = true;
    }

    public override bool Perform(GoapAgent agent, float dt)
    {
        if (!EnsureStarted(agent))
            return true;

        float arrive = Mathf.Max(agent.mover.arriveDistance, stopDistance);

        // 1) MOVE
        if (!_arrived)
        {
            if (_path != null && _path.Count > 0)
                _arrived = agent.mover.FollowPath(_path, ref _pathIndex, dt, arrive);
            else
                _arrived = agent.mover.MoveTowards(_goal, dt, arrive);

            if (!_arrived)
                return false;

            _elapsed = 0f; // start use timer
        }

        // 2) WAIT (use)
        _elapsed += dt;
        if (_elapsed < duration)
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
}
