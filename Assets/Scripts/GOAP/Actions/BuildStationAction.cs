// BuildStationAction.cs (FIXED: auto-finds Stations root + instantiates under it)
using System.Collections.Generic;
using UnityEngine;

public class BuildStationAction : GoapAction
{
    public StationType buildType = StationType.Bed;

    [Tooltip("Prefab to spawn (must have Station.cs on it).")]
    public GameObject stationPrefab;

    public int woodCost = 2;
    public float buildTime = 1.5f;

    [Header("Stop Margin")]
    [Tooltip("How far from the build spot the NPC may stop (bigger = stops sooner).")]
    public float stopDistance = 0.9f;

    private Vector2 _buildWorld;
    private float _t;

    private List<Vector2> _path;
    private int _pathIndex;

    private bool _reserved;
    private BuildSpotManager _mgr;

    // Cached stations root (auto-found once)
    private Transform _stationsRoot;

    private Transform GetStationsRoot()
    {
        if (_stationsRoot != null) return _stationsRoot;

        // Preferred: a GameObject named "Stations"
        var stationsGO = GameObject.Find("Stations");
        if (stationsGO != null)
        {
            _stationsRoot = stationsGO.transform;
            return _stationsRoot;
        }

        // Fallback: create it so we never spawn at scene root
        _stationsRoot = new GameObject("Stations").transform;
        return _stationsRoot;
    }

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
        if (agent.spotManager == null) return 9999f;

        Vector2 target = agent.spotManager.GetSpotPosition(buildType);
        float travel = (agent.nav != null)
            ? agent.nav.EstimatePathTime(agent.transform.position, target, agent.mover.speed)
            : Vector2.Distance(agent.transform.position, target) / Mathf.Max(0.01f, agent.mover.speed);

        return travel + buildTime;
    }

    public override bool StartAction(GoapAgent agent)
    {
        if (stationPrefab == null) return false;
        if (agent.spotManager == null) return false;

        _mgr = agent.spotManager;

        if (!_mgr.TryReserveSpot(buildType, agent, out _buildWorld))
            return false;

        _reserved = true;
        _t = 0f;

        _pathIndex = 0;
        _path = agent.nav != null ? agent.nav.FindPath(agent.transform.position, _buildWorld) : null;

        return true;
    }

    public override bool Perform(GoapAgent agent, float dt)
    {
        if (!_started)
        {
            _started = true;
            if (!StartAction(agent)) return true;
        }

        float arrive = Mathf.Max(agent.mover.arriveDistance, stopDistance);

        if (_path != null && _path.Count > 0)
        {
            if (!agent.mover.FollowPath(_path, ref _pathIndex, dt, arrive)) return false;
        }
        else
        {
            if (!agent.mover.MoveTowards(_buildWorld, dt, arrive)) return false;
        }

        _t += dt;
        if (_t >= buildTime)
        {
            if (agent.wood < woodCost) { Release(agent); return true; }

            agent.wood -= woodCost;

            Vector3 spawn = new Vector3(_buildWorld.x, _buildWorld.y, agent.spawnZ);

            // ✅ always instantiate under Stations root (auto-found/created)
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

        return false;
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
        _t = 0f;
        _path = null;
        _pathIndex = 0;
        _reserved = false;
        _mgr = null;
    }
}
