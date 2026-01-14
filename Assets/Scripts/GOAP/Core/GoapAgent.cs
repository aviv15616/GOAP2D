// GoapAgent.cs (UPDATED: uses travel-time based dynamic build-spot selection)
// NOTE: This file assumes BuildSpotManager exposes TryGetBestBuildSpotPos(...)
// and TryReserveBestSpot(...)/ReleaseSpot(...). See BuildSpotManager.cs below.

using System;
using System.Collections.Generic;
using UnityEngine;

public class GoapAgent : MonoBehaviour
{
    [Header("Personality")]
    public NeedType primaryNeed = NeedType.Sleep;

    [Header("Refs (drag from scene or auto-find)")]
    public StationRegistry registry;
    public BuildValidator buildValidator;
    public GridNav2D nav;
    public BuildSpotManager spotManager;

    [Header("Components")]
    public Needs needs;
    public Mover2D mover;

    [Header("Inventory")]
    public int wood = 0;

    [Header("Planning Safety")]
    public int maxWoodForPlanning = 12;

    [Header("Spawn Z for new stations")]
    public float spawnZ = 0f;

    [Header("Idle Wander")]
    public bool enableIdleWander = true;
    public float idleRadius = 6f;
    public float idlePickEvery = 2f;

    [Header("Actions (components on this NPC)")]
    public List<GoapAction> actions = new();

    [Header("Replan / Snapshot")]
    public float snapshotEvery = 1f;
    public bool alwaysSwitchToLowerScore = true;

    [Header("Replan Hysteresis")]
    [Range(0f, 0.9f)]
    public float replanMargin = 0.15f;

    [Header("Station vs Build Preference")]
    [Tooltip(
        "If existing station is not better than BUILD-ROUTE (wood->chop->build), treat station as NOT existing so planner may build instead."
    )]
    [Range(0f, 1f)]
    public float stationVsBuildMargin = 0.10f;

    [Header("Score Weights")]
    public float costWeight = 1f;
    public float urgencyWeight = 1f;
    public float urgencyFloor = 0.10f;
    public float primaryPreferenceMultiplier = 0.8f;
    public float otherPreferenceMultiplier = 1.0f;

    [Header("Debug Logs")]
    public bool enableGoapLogs = true;
    public bool logActionChanges = true;
    public float logMinInterval = 0.25f;

    private readonly GoapPlanner _planner = new();
    private Stack<GoapAction> _plan;

    private NeedType _currentNeed;
    private GoapAction _runningAction;
    private bool _noPlanLogged;

    private Vector2 _idleTarget;
    private float _idleTimer;
    private bool _idleLogged;
    public GoapAction CurrentAction => (_plan != null && _plan.Count > 0) ? _plan.Peek() : null;
    

    // Runtime episode timing (per-goal)
    private NeedType _episodeNeed = NeedType.None;
    private float _episodeStart = -1f;
    private string _episodeFirstPlanSig = "";

    public event Action<string, string> OnPlanChanged;
    public event Action<string, string> OnActionChanged;

    public string DebugGoal { get; private set; } = "-";
    public string DebugAction { get; private set; } = "-";

    private GoapDebugLogger _dbg;

    private float _currentPlanCost = -1f;
    private float _currentScore = float.PositiveInfinity;

    private float _nextSnapshotAt = 0f;

    private void Awake()
    {
        if (needs == null)
            needs = GetComponent<Needs>();
        if (mover == null)
            mover = GetComponent<Mover2D>();

        if (registry == null)
            registry = FindFirstObjectByType<StationRegistry>();
        if (buildValidator == null)
            buildValidator = FindFirstObjectByType<BuildValidator>();
        if (nav == null)
            nav = FindFirstObjectByType<GridNav2D>();
        if (spotManager == null)
            spotManager = FindFirstObjectByType<BuildSpotManager>();

        if (actions.Count == 0)
            GetComponents(actions);

        _currentNeed = primaryNeed;

        _idleTarget = transform.position;
        _idleTimer = 0f;

        _dbg = new GoapDebugLogger(name, logMinInterval);
        _nextSnapshotAt = Time.time;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        needs.Tick(dt);

        if (Time.time >= _nextSnapshotAt)
        {
            _nextSnapshotAt = Time.time + Mathf.Max(0.05f, snapshotEvery);
            EvaluateAndMaybeSwitchPlan();
        }

        if (_currentNeed == NeedType.None || _plan == null || _plan.Count == 0)
        {
            if (!_idleLogged)
                _idleLogged = true;
            if (enableIdleWander)
                TickIdleWander(dt);
            return;
        }
        _idleLogged = false;

        var a = _plan.Peek();

        if (_runningAction != a)
        {
            _runningAction = a;

            DebugGoal = _currentNeed.ToString();
            DebugAction = ActionPrettyName(a);

            OnActionChanged?.Invoke(DebugGoal, DebugAction);
        }

        if (!a.IsStillValid(this))
        {
            ClearPlan("action invalid");
            _nextSnapshotAt = Time.time;
            EvaluateAndMaybeSwitchPlan();
            return;
        }

        bool done = a.Perform(this, dt);
        if (done)
        {
            _plan.Pop();
            a.ResetRuntime();

            if (_plan == null || _plan.Count == 0)
            {
                _runningAction = null;

                // ✅ Episode finished -> log REAL time
                if (_episodeStart >= 0f)
                {
                    float real = Time.time - _episodeStart;
                    LogInfo(
                        "PLAN_REAL",
                        $"Goal={_episodeNeed} | realTime={real:F2}s | firstPlan={_episodeFirstPlanSig}"
                    );

                    _episodeStart = -1f;
                    _episodeNeed = NeedType.None;
                    _episodeFirstPlanSig = "";
                }
            }
        }
    }

    // -------------------------
    // Snapshot + scoring
    // -------------------------

    private void EvaluateAndMaybeSwitchPlan()
    {
        float uSleep = needs != null ? needs.GetUrgency(NeedType.Sleep) : 0f;
        float uHunger = needs != null ? needs.GetUrgency(NeedType.Hunger) : 0f;
        float uWarmth = needs != null ? needs.GetUrgency(NeedType.Warmth) : 0f;

        if (uSleep <= 0f && uHunger <= 0f && uWarmth <= 0f)
        {
            ClearPlan("no urgent need");
            _currentNeed = NeedType.None;
            return;
        }

        var candidates = new List<(NeedType need, float urgency)>
        {
            (NeedType.Sleep, uSleep),
            (NeedType.Hunger, uHunger),
            (NeedType.Warmth, uWarmth),
        };

        float bestScore = float.PositiveInfinity;
        float bestCost = -1f;
        NeedType bestNeed = NeedType.None;
        Stack<GoapAction> bestPlan = null;

        foreach (var (need, urg) in candidates)
        {
            if (urg <= 0f)
                continue;

            var snapshot = MakeWorldStateSnapshot(need);
            var goal = new Goals.GoalSatisfied(need);

            var plan = _planner.Plan(snapshot, actions, goal, this);
            if (plan == null || plan.Count == 0)
                continue;

            float cost = ComputePlanCost(plan, snapshot);
            float score = ComputeScore(need, urg, cost);

            if (score < bestScore)
            {
                bestScore = score;
                bestCost = cost;
                bestNeed = need;
                bestPlan = plan;
            }
        }

        if (bestPlan == null)
        {
            if (!_noPlanLogged)
            {
                _noPlanLogged = true;
                LogInfo("NOPLAN", $"Could not find plan | {MeterLine()}");
            }
            ClearPlan("no plan");
            _currentNeed = NeedType.None;
            return;
        }

        _noPlanLogged = false;

        bool haveNoCurrent = (_plan == null || _plan.Count == 0 || _currentNeed == NeedType.None);
        float m = Mathf.Clamp01(replanMargin);
        bool better = bestScore < _currentScore * (1f - m);

        if (haveNoCurrent || (alwaysSwitchToLowerScore && better))
        {
            foreach (var act in actions)
                if (act != null)
                    act.ResetRuntime();

            _plan = bestPlan;
            _currentNeed = bestNeed;
            _runningAction = null;

            _currentPlanCost = bestCost;
            _currentScore = bestScore;
            bool newEpisode = (_episodeStart < 0f) || (_episodeNeed != bestNeed);
            if (newEpisode)
            {
                _episodeNeed = bestNeed;
                _episodeStart = Time.time;
                _episodeFirstPlanSig = PlanToString(bestPlan);
            }

            string reason = haveNoCurrent ? "New plan created" : "Replan (better score)";
            spotManager?.EnsureInit();

            LogPlan(reason, bestNeed, bestCost, bestScore, bestPlan);
            NotifyPlanChanged(reason);
        }
    }

    private float  ComputeScore(NeedType need, float urgency01, float planCostSeconds)
    {
        float pref =
            (need == primaryNeed) ? primaryPreferenceMultiplier : otherPreferenceMultiplier;
        float denom = Mathf.Max(0.0001f, urgencyFloor + urgencyWeight * Mathf.Clamp01(urgency01));
        return (costWeight * Mathf.Max(0f, planCostSeconds)) * pref / denom;
    }

    private float ComputePlanCost(Stack<GoapAction> plan, WorldState startSnapshot)
    {
        if (plan == null || plan.Count == 0)
            return 9999f;

        float sum = 0f;
        var s = startSnapshot;

        foreach (var a in plan)
        {
            if (a == null)
                continue;

            float step = a.EstimateCost(this, s);
            if (float.IsNaN(step) || step < 0f)
                step = 0f;
            sum += step;

            a.ApplyPlanEffects(this, ref s);

            s.woodCarried = Mathf.Clamp(s.woodCarried, 0, Mathf.Max(0, maxWoodForPlanning));
        }

        return sum;
    }

    private void ClearPlan(string reason)
    {
        if (_plan != null)
            foreach (var a in actions)
                if (a != null)
                    a.ResetRuntime();

        _plan = null;
        _runningAction = null;
        _currentPlanCost = -1f;
        _currentScore = float.PositiveInfinity;
    }

    private void NotifyPlanChanged(string reason)
    {
        DebugGoal = _currentNeed.ToString();
        var top = (_plan != null && _plan.Count > 0) ? _plan.Peek() : null;
        DebugAction = top != null ? ActionPrettyName(top) : "None";
        OnPlanChanged?.Invoke(DebugGoal, DebugAction);
    }

    // -------------------------
    // Idle wander
    // -------------------------

    private void TickIdleWander(float dt)
    {
        _idleTimer -= dt;

        if (
            _idleTimer <= 0f
            || Vector2.Distance(transform.position, _idleTarget) <= mover.arriveDistance * 2f
        )
        {
            _idleTimer = Mathf.Max(0.05f, idlePickEvery);
            _idleTarget =
                (Vector2)transform.position
                + UnityEngine.Random.insideUnitCircle * Mathf.Max(0.01f, idleRadius);
        }

        mover.MoveTowards(_idleTarget, dt);
    }

    // -------------------------
    // Snapshot helpers (KEY)
    // -------------------------

    private WorldState MakeWorldStateSnapshot(NeedType targetNeed)
    {
        int carried = Mathf.Clamp(wood, 0, Mathf.Max(0, maxWoodForPlanning));

        var snap = new WorldState
        {
            pos = transform.position,
            woodCarried = carried,

            woodExists = AnyStationExists(StationType.Wood),

            bedExists = AnyStationExists(StationType.Bed),
            potExists = AnyStationExists(StationType.Pot),
            fireExists = AnyStationExists(StationType.Fire),

            sleepSatisfied = targetNeed != NeedType.Sleep,
            hungerSatisfied = targetNeed != NeedType.Hunger,
            warmthSatisfied = targetNeed != NeedType.Warmth,
        };

        snap.bedExists = EffectiveStationExists(StationType.Bed, snap);
        snap.potExists = EffectiveStationExists(StationType.Pot, snap);
        snap.fireExists = EffectiveStationExists(StationType.Fire, snap);

        return snap;
    }

    private bool EffectiveStationExists(StationType type, WorldState snap)
    {
        bool rawExists = AnyStationExists(type);

        if (!rawExists)
        {
            return false;
        }

        if (spotManager == null || mover == null)
        {
            return true;
        }

        Vector2 from = snap.pos;

        bool hasStation = TryGetNearestStationTravelTime(type, from, out float tStation);
        if (!hasStation)
        {
            return false;
        }

        float tBuildRoute = EstimateBuildRouteTime(type, snap);

        if (tBuildRoute >= 9999f)
        {
            return true;
        }

        float margin = Mathf.Clamp01(stationVsBuildMargin);
        bool stationWins = tStation <= tBuildRoute * (1f + margin);

        return stationWins;
    }

    private float EstimateBuildRouteTime(StationType type, WorldState snap)
    {
        BuildStationAction build = FindBuildAction(type);
        if (build == null)
        {
            return 9999f;
        }

        ChopWoodAction chop = FindChopAction();
        if (chop == null)
        {
            return 9999f;
        }

        int needWood = Mathf.Max(0, build.woodCost);
        int haveWood = Mathf.Clamp(snap.woodCarried, 0, Mathf.Max(0, maxWoodForPlanning));

        Vector2 from = snap.pos;

        // ✅ FIX: choose best build spot by travel time (not a fixed/global one)
        if (
            spotManager == null
            || !spotManager.TryGetBestBuildSpotPos(type, this, from, out Vector2 buildPos)
        )
        {
            return 9999f;
        }

        float speed = (mover != null) ? Mathf.Max(0.01f, mover.speed) : 1f;

        if (haveWood >= needWood)
        {
            float tToBuild =
                (nav != null)
                    ? nav.EstimatePathTime(from, buildPos, speed)
                    : Vector2.Distance(from, buildPos) / speed;

            if (tToBuild >= 9999f)
                return 9999f;
            return tToBuild + Mathf.Max(0f, build.duration);
        }

        if (!TryGetNearestStationInteractPos(StationType.Wood, from, out Vector2 woodPos))
        {
            return 9999f;
        }

        float tToWood =
            (nav != null)
                ? nav.EstimatePathTime(from, woodPos, speed)
                : Vector2.Distance(from, woodPos) / speed;

        if (tToWood >= 9999f)
        {
            return 9999f;
        }

        int missing = Mathf.Max(0, needWood - haveWood);
        int per = Mathf.Max(1, chop.woodPerChop);
        int chops = Mathf.CeilToInt(missing / (float)per);

        float tChop = chops * Mathf.Max(0f, chop.duration);

        float tWoodToBuild =
            (nav != null)
                ? nav.EstimatePathTime(woodPos, buildPos, speed)
                : Vector2.Distance(woodPos, buildPos) / speed;

        if (tWoodToBuild >= 9999f)
            return 9999f;

        return tToWood + tChop + tWoodToBuild + Mathf.Max(0f, build.duration);
    }

    private BuildStationAction FindBuildAction(StationType t)
    {
        if (actions == null)
            return null;
        foreach (var a in actions)
            if (a is BuildStationAction b && b.buildType == t)
                return b;
        return null;
    }

    private ChopWoodAction FindChopAction()
    {
        if (actions == null)
            return null;
        foreach (var a in actions)
            if (a is ChopWoodAction c)
                return c;
        return null;
    }

    // -------------------------
    // Travel-time based selection (KEY)
    // -------------------------

    public float EstimateTravelTime(Vector2 from, Vector2 to)
    {
        float speed = (mover != null) ? Mathf.Max(0.01f, mover.speed) : 1f;

        float t =
            (nav != null)
                ? nav.EstimatePathTime(from, to, speed)
                : Vector2.Distance(from, to) / speed;

        // ✅ runtime stops early when inside arriveDistance
        float arrive = (mover != null) ? mover.arriveDistance : 0.15f;
        t -= arrive / speed;

        return Mathf.Max(0f, t);
    }

    public float EstimateTravelTime(Vector2 from, Vector2 to, float arrive)
    {
        float speed = (mover != null) ? Mathf.Max(0.01f, mover.speed) : 1f;
        arrive = Mathf.Max(0.001f, arrive);

        if (nav != null)
            return nav.EstimatePathTimeLikeMover(from, to, speed, arrive);

        float d = Vector2.Distance(from, to);
        return Mathf.Max(0f, d - arrive) / speed;
    }

    private bool TryGetNearestStationTravelTime(StationType type, Vector2 from, out float bestTime)
    {
        bestTime = 9999f;

        if (registry == null || registry.AllStations == null)
            return false;

        bool found = false;
        foreach (var st in registry.AllStations)
        {
            if (st == null)
                continue;
            if (!st.Exists)
                continue;
            if (st.type != type)
                continue;

            float t = EstimateTravelTime(from, st.InteractPos);
            if (t < bestTime)
            {
                bestTime = t;
                found = true;
            }
        }

        return found;
    }

    public bool TryGetBestStationPos(StationType type, Vector2 from, out Vector2 bestPos)
    {
        bestPos = default;

        if (registry == null || registry.AllStations == null)
            return false;

        bool found = false;
        float bestTime = 9999f;

        foreach (var st in registry.AllStations)
        {
            if (st == null)
                continue;
            if (!st.Exists)
                continue;
            if (st.type != type)
                continue;

            float t = EstimateTravelTime(from, st.InteractPos);
            if (t < bestTime)
            {
                bestTime = t;
                bestPos = st.InteractPos;
                found = true;
            }
        }

        return found;
    }

    private bool TryGetNearestStationInteractPos(
        StationType type,
        Vector2 from,
        out Vector2 bestPos
    )
    {
        bestPos = default;

        if (registry == null || registry.AllStations == null)
            return false;

        bool found = false;
        float bestTime = 9999f;

        foreach (var st in registry.AllStations)
        {
            if (st == null)
                continue;
            if (!st.Exists)
                continue;
            if (st.type != type)
                continue;

            float t = EstimateTravelTime(from, st.InteractPos);
            if (t < bestTime)
            {
                bestTime = t;
                bestPos = st.InteractPos;
                found = true;
            }
        }

        return found;
    }

    private bool AnyStationExists(StationType t)
    {
        if (registry == null || registry.AllStations == null)
            return false;
        foreach (var st in registry.AllStations)
            if (st != null && st.type == t && st.Exists)
                return true;
        return false;
    }

    public Station FindNearestStation(StationType type)
    {
        if (registry == null || registry.AllStations == null)
            return null;

        Station best = null;
        float bestDist = float.MaxValue;
        Vector2 me = transform.position;

        foreach (var st in registry.AllStations)
        {
            if (st == null)
                continue;
            if (!st.Exists)
                continue;
            if (st.type != type)
                continue;

            float d = Vector2.Distance(me, st.InteractPos);
            if (d < bestDist)
            {
                bestDist = d;
                best = st;
            }
        }
        return best;
    }

    // -------------------------
    // Logging
    // -------------------------

    private string MeterLine() =>
        $"E={needs.energy:F0} H={needs.hunger:F0} W={needs.warmth:F0} | Wood={wood}";

    private static StationType NeedToStation(NeedType n) =>
        n switch
        {
            NeedType.Sleep => StationType.Bed,
            NeedType.Hunger => StationType.Pot,
            NeedType.Warmth => StationType.Fire,
            _ => StationType.Bed,
        };

    private string ActionPrettyName(GoapAction a)
    {
        if (a == null)
            return "<null>";
        if (a is ChopWoodAction c)
            return $"ChopWood(+{c.woodPerChop})";
        if (a is BuildStationAction b)
            return $"Build({b.buildType}, cost={b.woodCost})";
        if (a is UseStationAction u)
            return $"Use({NeedToStation(u.need)}, +{u.restoreAmount:F0})";
        return a.GetType().Name;
    }

    private string PlanToString(Stack<GoapAction> plan)
    {
        if (plan == null)
            return "<null>";
        if (plan.Count == 0)
            return "<empty>";

        var parts = new List<string>(plan.Count);
        foreach (var a in plan)
            parts.Add(ActionPrettyName(a));
        return string.Join(" -> ", parts);
    }

    private void LogPlan(
        string reason,
        NeedType goalNeed,
        float planCost,
        float score,
        Stack<GoapAction> plan
    )
    {
        if (!enableGoapLogs || _dbg == null)
            return;

        string goal = goalNeed.ToString();
        string planStr = PlanToString(plan);
        string meters = MeterLine();
        int count = (plan == null) ? 0 : plan.Count;

        string msg =
            $"{reason}\n"
            + $"Goal={goal} | planCost={planCost:F2}s | score={score:F3}\n"
            + $"{meters}\n"
            + $"Plan({count}): {planStr}";

        string sig = $"PLAN|{goal}|{reason}|{planStr}|{planCost:F2}|{score:F3}|{meters}";
        _dbg.Log("PLAN", msg, sig);
    }

    private void LogInfo(string tag, string msg)
    {
        if (!enableGoapLogs || _dbg == null)
            return;
        _dbg.Log(tag, msg);
    }
}
