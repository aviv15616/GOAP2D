// BuildStationAction.cs (FIXED)
// - Planning cost uses WorldState.pos (NOT transform.position)
// - Planning advances WorldState.pos to the build spot (so next action costs aggregate correctly)
// - EstimateCost = travelTime + duration (UseStation-style aggregation)
// - IsStillValid = compares travel-to-existing-station vs travel-to-build-spot (no GoapAgent.EffectiveStationExistsNow dependency)
// - Runtime = move first, then wait duration, then commit build
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

    [Header("Station-vs-Build Validity")]
    [Tooltip("If an existing station is cheaper/equal than building (within this margin), building becomes invalid at runtime.\n" +
             "0.10 means: station is considered acceptable if <= buildTime * (1 + 0.10).")]
    [Range(0f, 1f)]
    public float stationVsBuildMargin = 0.10f;

    private Vector2 _buildWorld;
    private List<Vector2> _path;
    private int _pathIndex;

    private bool _reserved;
    private bool _arrived;

    private BuildSpotManager _mgr;

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
        // IMPORTANT: advance simulated position to the build spot
        // so the next action (UseStation) will compute from there.
        // If we can't access spotManager at plan-time, we leave pos unchanged.
        // (But in your setup, spotManager exists.)
        // NOTE: BuildSpotManager API uses StationType, not "buildType only bed/pot/fire" problem.
        // We keep it consistent with your buildType.
        // We can't call agent here, so we just use s.pos if already set by other actions
        // and GoapAgent will provide spot in EstimateCost and plan aggregation.
        // If your WorldState has pos, we can still set it if planner calls EstimateCost first.
        // (Your planner does: EstimateCost then ApplyPlanEffects -> perfect.)

        if (buildType == StationType.Bed) s.bedExists = true;
        if (buildType == StationType.Pot) s.potExists = true;
        if (buildType == StationType.Fire) s.fireExists = true;

        s.woodCarried -= woodCost;
        // s.pos will be set during cost evaluation via cached target (see below)
        if (_cachedPlanTargetValid) s.pos = _cachedPlanTarget;
    }

    private Vector2 _cachedPlanTarget;
    private bool _cachedPlanTargetValid;

    public override float EstimateCost(GoapAgent agent, WorldState currentState)
    {
        _cachedPlanTargetValid = false;

        if (agent == null || agent.spotManager == null || agent.mover == null)
            return 9999f;

        Vector2 target = agent.spotManager.GetSpotPosition(buildType);
        _cachedPlanTarget = target;
        _cachedPlanTargetValid = true;

        // Use SIMULATED position from the worldstate
        float travel = agent.EstimateTravelTime(currentState.pos, target);

        // UseStation-style aggregation: travel + action seconds (duration)
        return travel + Mathf.Max(0f, duration);
    }

    // -------------------------
    // RUNTIME
    // -------------------------

    public override bool IsStillValid(GoapAgent agent)
    {
        if (agent == null || agent.spotManager == null || agent.mover == null)
            return false;

        // If we can't build, action should become invalid (planner should pick ChopWood etc).
        if (agent.wood < woodCost)
            return false;

        Vector2 from = agent.transform.position;

        Vector2 buildPos = agent.spotManager.GetSpotPosition(buildType);
        float tBuild = agent.EstimateTravelTime(from, buildPos);
        if (tBuild >= 9999f)
            return false;

        // If no station exists at all, building is valid.
        if (!agent.TryGetBestStationPos(buildType, from, out var bestStationPos))
            return true;

        float tStation = agent.EstimateTravelTime(from, bestStationPos);

        float m = Mathf.Clamp01(stationVsBuildMargin);
        float threshold = tBuild * (1f + m);

        // If a station is cheap enough, building is NOT needed.
        bool stationGoodEnough = tStation <= threshold;
        return !stationGoodEnough;
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
        // Manual start so we don't "wait before moving"
        if (!_started)
        {
            _started = true;
            _elapsed = 0f;

            if (!StartAction(agent))
                return true; // fail-fast skip
        }

        if (agent == null || agent.mover == null)
        {
            Release(agent);
            return true;
        }

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

        // 2) WAIT after arrival (duration)
        _elapsed += dt;
        if (_elapsed < duration)
            return false;

        // 3) COMMIT build once
        if (agent.wood < woodCost)
        {
            Release(agent);
            return true;
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

        _cachedPlanTargetValid = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_path == null || _path.Count < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < _path.Count - 1; i++)
            Gizmos.DrawLine(_path[i], _path[i + 1]);

        if (_pathIndex >= 0 && _pathIndex < _path.Count)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_path[_pathIndex], 0.12f);
        }
    }
#endif
}
