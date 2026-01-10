// BuildStationAction.cs (FIXED: move first, then wait/build; no initial "stand still" pause)
using System.Collections.Generic;
using UnityEngine;

public class BuildStationAction : GoapAction
{
    public StationType buildType = StationType.Bed;

    [Tooltip("Prefab to spawn (must have Station.cs on it).")]
    public GameObject stationPrefab;

    public int woodCost = 2;

    [Header("Stop Margin")]
    [Tooltip("How far from the build spot the NPC may stop (bigger = stops sooner).")]
    public float stopDistance = 0.9f;

    private Vector2 _buildWorld;
    private List<Vector2> _path;
    private int _pathIndex;

    private bool _reserved;
    private bool _arrived;

    private BuildSpotManager _mgr;

    // Cached stations root (auto-found once)
    private Transform _stationsRoot;

    private Transform GetStationsRoot()
    {
        if (_stationsRoot != null) return _stationsRoot;

        var stationsGO = GameObject.Find("Stations");
        if (stationsGO != null)
        {
            _stationsRoot = stationsGO.transform;
            return _stationsRoot;
        }

        _stationsRoot = new GameObject("Stations").transform;
        return _stationsRoot;
    }

    // -------------------------
    // PLANNING
    // -------------------------

    public override bool CanPlan(WorldState s)
    {
        bool exists = buildType switch
        {
            StationType.Bed => s.bedExists,
            StationType.Pot => s.potExists,
            StationType.Fire => s.fireExists,
            _ => true
        };

        return !exists && s.woodCarried >= woodCost;
    }

    public override void ApplyPlanEffects(ref WorldState s)
    {
        if (buildType == StationType.Bed) s.bedExists = true;
        if (buildType == StationType.Pot) s.potExists = true;
        if (buildType == StationType.Fire) s.fireExists = true;

        s.woodCarried -= woodCost;
    }

    public override float EstimateCost(GoapAgent agent, WorldState currentState)
    {
        if (agent == null || agent.spotManager == null || agent.mover == null) return 9999f;

        Vector2 target = agent.spotManager.GetSpotPosition(buildType);

        // travel time only
        if (agent.nav != null)
            return agent.nav.EstimatePathTime(agent.transform.position, target, agent.mover.speed);

        float speed = Mathf.Max(0.01f, agent.mover.speed);
        return Vector2.Distance(agent.transform.position, target) / speed;
    }

    // -------------------------
    // RUNTIME
    // -------------------------

    public override bool IsStillValid(GoapAgent agent)
    {
        if (agent == null) return false;

        bool existsNow = buildType switch
        {
            StationType.Bed => agent.FindNearestStation(StationType.Bed) != null,
            StationType.Pot => agent.FindNearestStation(StationType.Pot) != null,
            StationType.Fire => agent.FindNearestStation(StationType.Fire) != null,
            _ => false
        };

        return !existsNow;
    }

    public override bool StartAction(GoapAgent agent)
    {
        if (stationPrefab == null) return false;
        if (agent == null || agent.spotManager == null) return false;

        _mgr = agent.spotManager;

        if (!_mgr.TryReserveSpot(buildType, agent, out _buildWorld))
            return false;

        _reserved = true;
        _arrived = false;

        _pathIndex = 0;
        _path = agent.nav != null ? agent.nav.FindPath(agent.transform.position, _buildWorld) : null;

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
                _arrived = agent.mover.MoveTowards(_buildWorld, dt, arrive);

            if (!_arrived)
                return false;

            // arrived -> start build timer now
            _elapsed = 0f;
        }

        // 2) WAIT after arrival (build time)
        if (!WaitAfterArrival(dt))
            return false;

        // 3) COMMIT build once
        if (agent.wood < woodCost)
        {
            Release(agent);
            return true; // finish; agent will replan
        }

        agent.wood -= woodCost;

        Vector3 spawn = new Vector3(_buildWorld.x, _buildWorld.y, agent.spawnZ);

        Transform parent = GetStationsRoot();
        var go = Object.Instantiate(stationPrefab, spawn, Quaternion.identity, parent);

        var st = go.GetComponent<Station>();
        if (st != null)
        {
            st.type = buildType;
            st.built = true;
        }

        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null) rb.bodyType = RigidbodyType2D.Static;

        Release(agent);
        return true;
    }

    private void Release(GoapAgent agent)
    {
        if (!_reserved) return;
        _reserved = false;

        if (_mgr != null)
            _mgr.ReleaseSpot(buildType, agent);
    }

    public override void ResetRuntime()
    {
        if (_reserved && _mgr != null)
            _mgr.ReleaseSpot(buildType, null);

        base.ResetRuntime();
        _path = null;
        _pathIndex = 0;
        _reserved = false;
        _arrived = false;
        _mgr = null;
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
