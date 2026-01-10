// GoapAgent.cs (FIXED: run GOAP + Rigidbody movement in FixedUpdate to eliminate startup speed burst)
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

    private readonly GoapPlanner _planner = new();
    private Stack<GoapAction> _plan;

    private NeedType _currentNeed;
    private GoapAction _runningAction;
    private bool _noPlanLogged;

    // idle runtime
    private Vector2 _idleTarget;
    private float _idleTimer;
    private bool _idleLogged;

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
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        needs.Tick(dt);

        // -------------------------
        // IDLE branch (wander)
        // -------------------------
        NeedType desiredNeed = ChooseNeed();
        if (desiredNeed == NeedType.None)
        {
            if (_plan != null && _plan.Count > 0) ForceReplan();

            if (!_idleLogged)
            {
                _idleLogged = true;
                Debug.Log($"[IDLE] {name} | {MeterLine()}");
            }

            if (enableIdleWander)
                TickIdleWander(dt);

            return;
        }
        _idleLogged = false;

        // -------------------------
        // GOAP planning/execution
        // -------------------------
        if (desiredNeed != _currentNeed)
        {
            _currentNeed = desiredNeed;
            ForceReplan();
            LogInfo("GOAL", "Need changed -> forcing replan");
        }

        if (_plan == null || _plan.Count == 0)
        {
            BuildPlanFor(_currentNeed);

            if (_plan == null || _plan.Count == 0)
            {
                if (!_noPlanLogged)
                {
                    _noPlanLogged = true;
                    Debug.LogWarning($"[NO_PLAN] {name} | Need={_currentNeed} | {MeterLine()}");
                }
                return;
            }

            _noPlanLogged = false;
            LogPlan("New plan created");
        }

        var a = _plan.Peek();

        if (_runningAction != a)
        {
            _runningAction = a;
            LogInfo("ACT_START", ActionPrettyName(a));
        }

        if (!a.IsStillValid(this))
        {
            LogInfo("INVALID", ActionPrettyName(a) + " became invalid -> replan");
            ForceReplan();
            return;
        }

        bool done = a.Perform(this, dt);
        if (done)
        {
            LogInfo("ACT_DONE", ActionPrettyName(a));
            _plan.Pop();
            a.ResetRuntime();

            if (_plan == null || _plan.Count == 0)
                _runningAction = null;
        }
    }

    private void TickIdleWander(float dt)
    {
        _idleTimer -= dt;

        if (_idleTimer <= 0f || Vector2.Distance(transform.position, _idleTarget) <= mover.arriveDistance * 2f)
        {
            _idleTimer = Mathf.Max(0.05f, idlePickEvery);

            Vector2 center = transform.position;
            Vector2 rand = Random.insideUnitCircle * Mathf.Max(0.01f, idleRadius);
            _idleTarget = center + rand;
        }

        mover.MoveTowards(_idleTarget, dt);
    }

    private void ForceReplan()
    {
        if (_plan != null)
            foreach (var a in actions) a.ResetRuntime();

        _plan = null;
        _runningAction = null;
        _noPlanLogged = false;
    }

    private NeedType ChooseNeed()
    {
        if (needs.hunger <= needs.critical) return NeedType.Hunger;
        if (needs.warmth <= needs.critical) return NeedType.Warmth;
        if (needs.energy <= needs.critical) return NeedType.Sleep;

        if (primaryNeed == NeedType.Sleep && needs.energy <= needs.urgent) return NeedType.Sleep;
        if (primaryNeed == NeedType.Hunger && needs.hunger <= needs.urgent) return NeedType.Hunger;
        if (primaryNeed == NeedType.Warmth && needs.warmth <= needs.urgent) return NeedType.Warmth;

        if (needs.hunger <= needs.urgent) return NeedType.Hunger;
        if (needs.warmth <= needs.urgent) return NeedType.Warmth;
        if (needs.energy <= needs.urgent) return NeedType.Sleep;

        return NeedType.None;
    }

    private void BuildPlanFor(NeedType need)
    {
        var s = MakeWorldStateSnapshot(need);
        var goal = new Goals.GoalSatisfied(need);

        _plan = _planner.Plan(s, actions, goal, this);

        if (_plan == null || _plan.Count == 0)
            _plan = null;
    }

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

    private void LogPlan(string reason)
    {
        Debug.Log($"[PLAN] {name} | Need={_currentNeed} | {reason} | {MeterLine()} | Path: {PlanToString(_plan)}");
    }

    private void LogInfo(string tag, string msg)
    {
        Debug.Log($"[{tag}] {name} | Need={_currentNeed} | {msg} | {MeterLine()}");
    }
}
