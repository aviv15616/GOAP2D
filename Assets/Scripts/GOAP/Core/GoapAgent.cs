// GoapAgent.cs (UPDATED: periodic snapshot + score-based replanning using urgency + preference + plan cost)
// Logs: only PLAN/REPLAN + ACT (no STAT/IDLE spam)
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
    [Tooltip("Caps the planner's woodCarried state-space (prevents infinite planning).")]
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
    [Tooltip("How often (seconds) the NPC re-evaluates worldstate + best plan.")]
    public float snapshotEvery = 1f;

    [Tooltip("If true: whenever a lower Score plan exists, switch immediately.")]
    public bool alwaysSwitchToLowerScore = true;

    [Header("Replan Hysteresis")]
    [Tooltip("Switch only if newScore is at least (1-margin) better than current. Example: 0.15 = must be 15% better.")]
    [Range(0f, 0.9f)]
    public float replanMargin = 0.15f;


    [Header("Score Weights (relative, tune later)")]
    [Tooltip("Score = (costWeight*PlanCost) * PreferenceMultiplier / (urgencyFloor + urgencyWeight*Urgency). Lower is better.")]
    public float costWeight = 1f;

    [Tooltip("How much urgency reduces score (bigger => urgency matters more).")]
    public float urgencyWeight = 1f;

    [Tooltip("Prevents division by near-zero urgency.")]
    public float urgencyFloor = 0.10f;

    [Tooltip("Multiplier applied to score if the plan satisfies the primaryNeed (smaller => preferred).")]
    public float primaryPreferenceMultiplier = 0.8f;

    [Tooltip("Multiplier applied to score if the plan satisfies a non-primary need.")]
    public float otherPreferenceMultiplier = 1.0f;

    [Header("Debug Logs")]
    public bool enableGoapLogs = true;
    public bool logActionChanges = true;

    [Tooltip("Minimum time between identical log signatures (per NPC).")]
    public float logMinInterval = 0.25f;

    private readonly GoapPlanner _planner = new();
    private Stack<GoapAction> _plan;

    private NeedType _currentNeed;
    private GoapAction _runningAction;
    private bool _noPlanLogged;

    // idle runtime
    private Vector2 _idleTarget;
    private float _idleTimer;
    private bool _idleLogged;

    // UI hooks
    public event Action<string, string> OnPlanChanged;   // (goal, action)
    public event Action<string, string> OnActionChanged; // (goal, action)

    public string DebugGoal { get; private set; } = "-";
    public string DebugAction { get; private set; } = "-";

    // logging
    private GoapDebugLogger _dbg;

    // last selected plan score/cost (for comparisons + logs)
    private float _currentPlanCost = -1f;
    private float _currentScore = float.PositiveInfinity;

    // snapshot timer
    private float _nextSnapshotAt = 0f;

    private void Awake()
    {
        if (needs == null) needs = GetComponent<Needs>();
        if (mover == null) mover = GetComponent<Mover2D>();

        if (registry == null) registry = FindFirstObjectByType<StationRegistry>();
        if (buildValidator == null) buildValidator = FindFirstObjectByType<BuildValidator>();
        if (nav == null) nav = FindFirstObjectByType<GridNav2D>();
        if (spotManager == null) spotManager = FindFirstObjectByType<BuildSpotManager>();

        if (actions.Count == 0) GetComponents(actions);

        _currentNeed = primaryNeed;

        _idleTarget = transform.position;
        _idleTimer = 0f;

        _dbg = new GoapDebugLogger(name, logMinInterval);

        _nextSnapshotAt = Time.time; // run immediately
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        needs.Tick(dt);

        // -------------------------
        // Periodic snapshot / best-plan selection
        // -------------------------
        if (Time.time >= _nextSnapshotAt)
        {
            _nextSnapshotAt = Time.time + Mathf.Max(0.05f, snapshotEvery);
            EvaluateAndMaybeSwitchPlan();
        }

        // If we still have no need + no plan, just idle wander
        if (_currentNeed == NeedType.None || _plan == null || _plan.Count == 0)
        {
            if (!_idleLogged)
                _idleLogged = true;

            if (enableIdleWander)
                TickIdleWander(dt);

            return;
        }
        _idleLogged = false;

        // -------------------------
        // Execute current plan
        // -------------------------
        var a = _plan.Peek();

        if (_runningAction != a)
        {
            _runningAction = a;

            DebugGoal = _currentNeed.ToString();
            DebugAction = ActionPrettyName(a);

            OnActionChanged?.Invoke(DebugGoal, DebugAction);

            if (enableGoapLogs && logActionChanges)
                LogInfo("ACT", $"Goal={DebugGoal} | Now={DebugAction} | {MeterLine()}");
        }

        if (!a.IsStillValid(this))
        {
            ClearPlan("action invalid");

            // replan immediately instead of waiting 1–2 seconds
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
                // next snapshot tick will pick next best plan (or idle)
            }
        }
    }


    // -------------------------
    // Snapshot + scoring
    // -------------------------

    private void EvaluateAndMaybeSwitchPlan()
    {
        // Compute urgencies (0..1)
        float uSleep = needs != null ? needs.GetUrgency(NeedType.Sleep) : 0f;
        float uHunger = needs != null ? needs.GetUrgency(NeedType.Hunger) : 0f;
        float uWarmth = needs != null ? needs.GetUrgency(NeedType.Warmth) : 0f;

        // If nothing urgent at all => go idle (clear plan)
        if (uSleep <= 0f && uHunger <= 0f && uWarmth <= 0f)
        {
            ClearPlan("no urgent need");
            _currentNeed = NeedType.None;
            return;
        }

        // Evaluate all candidate needs with urgency > 0
        var candidates = new List<(NeedType need, float urgency)>
        {
            (NeedType.Sleep,  uSleep),
            (NeedType.Hunger, uHunger),
            (NeedType.Warmth, uWarmth),
        };

        float bestScore = float.PositiveInfinity;
        float bestCost = -1f;
        NeedType bestNeed = NeedType.None;
        Stack<GoapAction> bestPlan = null;

        foreach (var (need, urg) in candidates)
        {
            if (urg <= 0f) continue;

            var snapshot = MakeWorldStateSnapshot(need);
            var goal = new Goals.GoalSatisfied(need);

            var plan = _planner.Plan(snapshot, actions, goal, this);
            if (plan == null || plan.Count == 0) continue;

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
            // no plan possible for any urgent need
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

        // Should we switch?
        bool haveNoCurrent = (_plan == null || _plan.Count == 0 || _currentNeed == NeedType.None);
        float m = Mathf.Clamp01(replanMargin);
        bool better = bestScore < _currentScore * (1f - m);

        if (haveNoCurrent || (alwaysSwitchToLowerScore && better))
        {
            bool isSwitch = !haveNoCurrent;

            // reset runtime on all actions so we don't keep old timers/path indices
            foreach (var act in actions)
                if (act != null) act.ResetRuntime();

            _plan = bestPlan;
            _currentNeed = bestNeed;
            _runningAction = null;

            _currentPlanCost = bestCost;
            _currentScore = bestScore;

            string reason = haveNoCurrent ? "New plan created" : "Replan (better score)";
            LogPlan(reason, bestNeed, bestCost, bestScore, bestPlan);

            NotifyPlanChanged(reason);
        }
        else
        {
            // Keep current plan, but keep values coherent if we never set them yet
            if (_currentScore == float.PositiveInfinity && _plan != null && _plan.Count > 0)
            {
                // best effort compute
                var snap = MakeWorldStateSnapshot(_currentNeed);
                _currentPlanCost = ComputePlanCost(_plan, snap);
                _currentScore = ComputeScore(_currentNeed, needs.GetUrgency(_currentNeed), _currentPlanCost);
            }
        }
    }

    private float ComputeScore(NeedType need, float urgency01, float planCostSeconds)
    {
        float pref = (need == primaryNeed) ? primaryPreferenceMultiplier : otherPreferenceMultiplier;

        // Lower is better.
        // - Plan cost increases score.
        // - Higher urgency reduces score.
        // - Preference reduces score for the primary need.
        float denom = Mathf.Max(0.0001f, urgencyFloor + urgencyWeight * Mathf.Clamp01(urgency01));
        float score = (costWeight * Mathf.Max(0f, planCostSeconds)) * pref / denom;
        return score;
    }

    private float ComputePlanCost(Stack<GoapAction> plan, WorldState startSnapshot)
    {
        if (plan == null || plan.Count == 0) return 9999f;

        float sum = 0f;
        var s = startSnapshot;

        // Stack enumerates from top->bottom (first executed -> last)
        foreach (var a in plan)
        {
            if (a == null) continue;

            float step = a.EstimateCost(this, s);
            if (float.IsNaN(step) || step < 0f) step = 0f;
            sum += step;

            a.ApplyPlanEffects(ref s);
            s.woodCarried = Mathf.Clamp(s.woodCarried, 0, Mathf.Max(0, maxWoodForPlanning));
        }

        return sum;
    }

    private void ClearPlan(string reason)
    {
        if (_plan != null)
        {
            foreach (var a in actions)
                if (a != null) a.ResetRuntime();
        }

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

        if (_idleTimer <= 0f || Vector2.Distance(transform.position, _idleTarget) <= mover.arriveDistance * 2f)
        {
            _idleTimer = Mathf.Max(0.05f, idlePickEvery);

            Vector2 center = transform.position;
            Vector2 rand = UnityEngine.Random.insideUnitCircle * Mathf.Max(0.01f, idleRadius);
            _idleTarget = center + rand;
        }

        mover.MoveTowards(_idleTarget, dt);
    }

    // -------------------------
    // World state snapshot helpers
    // -------------------------

    private WorldState MakeWorldStateSnapshot(NeedType targetNeed)
    {
        return new WorldState
        {
            woodExists = AnyStationExists(StationType.Wood),
            bedExists = AnyStationExists(StationType.Bed),
            potExists = AnyStationExists(StationType.Pot),
            fireExists = AnyStationExists(StationType.Fire),

            woodCarried = Mathf.Clamp(wood, 0, Mathf.Max(0, maxWoodForPlanning)),

            sleepSatisfied = targetNeed != NeedType.Sleep,
            hungerSatisfied = targetNeed != NeedType.Hunger,
            warmthSatisfied = targetNeed != NeedType.Warmth
        };
    }

    private bool AnyStationExists(StationType t)
    {
        if (registry == null || registry.AllStations == null) return false;

        foreach (var st in registry.AllStations)
            if (st != null && st.type == t && st.Exists)
                return true;

        return false;
    }

    public Station FindNearestStation(StationType type)
    {
        if (registry == null || registry.AllStations == null) return null;

        Station best = null;
        float bestDist = float.MaxValue;
        Vector2 me = transform.position;

        foreach (var st in registry.AllStations)
        {
            if (st == null) continue;
            if (!st.Exists) continue;
            if (st.type != type) continue;

            float d = Vector2.Distance(me, st.InteractPos);
            if (d < bestDist) { bestDist = d; best = st; }
        }
        return best;
    }

    // -------------------------
    // Logging
    // -------------------------

    private string MeterLine() => $"E={needs.energy:F0} H={needs.hunger:F0} W={needs.warmth:F0} | Wood={wood}";

    private static StationType NeedToStation(NeedType n) => n switch
    {
        NeedType.Sleep => StationType.Bed,
        NeedType.Hunger => StationType.Pot,
        NeedType.Warmth => StationType.Fire,
        _ => StationType.Bed
    };

    private string ActionPrettyName(GoapAction a)
    {
        if (a == null) return "<null>";
        if (a is ChopWoodAction c) return $"ChopWood(+{c.woodPerChop})";
        if (a is BuildStationAction b) return $"Build({b.buildType}, cost={b.woodCost})";
        if (a is UseStationAction u) return $"Use({NeedToStation(u.need)}, +{u.restoreAmount:F0})";
        return a.GetType().Name;
    }

    private string PlanToString(Stack<GoapAction> plan)
    {
        if (plan == null) return "<null>";
        if (plan.Count == 0) return "<empty>";

        var parts = new List<string>(plan.Count);
        foreach (var a in plan) parts.Add(ActionPrettyName(a));
        return string.Join(" -> ", parts);
    }

    private void LogPlan(string reason, NeedType goalNeed, float planCost, float score, Stack<GoapAction> plan)
    {
        if (!enableGoapLogs || _dbg == null) return;

        string goal = goalNeed.ToString();
        string planStr = PlanToString(plan);
        string meters = MeterLine();
        int count = (plan == null) ? 0 : plan.Count;

        string msg =
            $"{reason}\n" +
            $"Goal={goal} | planCost={planCost:F2}s | score={score:F3}\n" +
            $"{meters}\n" +
            $"Plan({count}): {planStr}";

        string sig = $"PLAN|{goal}|{reason}|{planStr}|{planCost:F2}|{score:F3}|{meters}";
        _dbg.Log("PLAN", msg, sig);
    }

    private void LogInfo(string tag, string msg)
    {
        if (!enableGoapLogs || _dbg == null) return;
        _dbg.Log(tag, msg);
    }
}
