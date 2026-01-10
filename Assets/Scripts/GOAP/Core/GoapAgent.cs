using System.Collections.Generic;
using UnityEngine;

public class GoapAgent : MonoBehaviour
{
    [Header("Personality")]
    public NeedType primaryNeed = NeedType.Sleep;

    [Header("Refs (drag from scene or auto-find)")]
    public StationRegistry registry;      // World object has this
    public BuildValidator buildValidator; // World object has this

    [Header("Components")]
    public Needs needs;
    public Mover2D mover;

    [Header("Inventory")]
    public int wood = 0;

    [Header("Spawn Z for new stations")]
    public float spawnZ = 0f;

    [Header("Actions (components on this NPC)")]
    public List<GoapAction> actions = new();

    private readonly GoapPlanner _planner = new();
    private Stack<GoapAction> _plan;

    private NeedType _currentNeed;

    private void Awake()
    {
        if (needs == null) needs = GetComponent<Needs>();
        if (mover == null) mover = GetComponent<Mover2D>();

        if (registry == null) registry = FindFirstObjectByType<StationRegistry>();
        if (buildValidator == null) buildValidator = FindFirstObjectByType<BuildValidator>();

        if (actions.Count == 0)
            GetComponents(actions); // grab all GoapAction components on this NPC
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // meters drain
        needs.Tick(dt);

        // choose which need to satisfy now (critical overrides)
        NeedType desiredNeed = ChooseNeed();

        // if need changed (ex: became critical), replan immediately
        if (desiredNeed != _currentNeed)
        {
            _currentNeed = desiredNeed;
            ForceReplan();
        }

        // if no plan, plan now
        if (_plan == null || _plan.Count == 0)
        {
            BuildPlanFor(_currentNeed);
            if (_plan == null || _plan.Count == 0)
            {
                // optional: idle/wander later
                return;
            }
        }

        // execute top action
        var a = _plan.Peek();

        // if station disappeared etc => replan
        if (!a.IsStillValid(this))
        {
            ForceReplan();
            return;
        }

        bool done = a.Perform(this, dt);
        if (done)
        {
            _plan.Pop();
            a.ResetRuntime();
        }
    }

    private void ForceReplan()
    {
        if (_plan != null)
        {
            foreach (var a in actions) a.ResetRuntime();
        }
        _plan = null;
    }

    private NeedType ChooseNeed()
    {
        // critical override
        if (needs.hunger <= needs.critical) return NeedType.Hunger;
        if (needs.warmth <= needs.critical) return NeedType.Warmth;
        if (needs.energy <= needs.critical) return NeedType.Sleep;

        // otherwise: personality need if it's urgent
        if (primaryNeed == NeedType.Sleep && needs.energy <= needs.urgent) return NeedType.Sleep;
        if (primaryNeed == NeedType.Hunger && needs.hunger <= needs.urgent) return NeedType.Hunger;
        if (primaryNeed == NeedType.Warmth && needs.warmth <= needs.urgent) return NeedType.Warmth;

        // fallback: any urgent need
        if (needs.hunger <= needs.urgent) return NeedType.Hunger;
        if (needs.warmth <= needs.urgent) return NeedType.Warmth;
        if (needs.energy <= needs.urgent) return NeedType.Sleep;

        // nothing urgent
        return primaryNeed;
    }

    private void BuildPlanFor(NeedType need)
    {
        var s = MakeWorldStateSnapshot(need);
        var goal = new Goals.GoalSatisfied(need);

        _plan = _planner.Plan(s, actions, goal);

        // debug
        if (_plan == null)
            Debug.LogWarning($"{name}: No plan for {need}");
        else
            Debug.Log($"{name}: Planned {_plan.Count} steps for {need}");
    }

    private WorldState MakeWorldStateSnapshot(NeedType targetNeed)
    {
        var s = new WorldState();

        s.woodExists = AnyStationExists(StationType.Wood);
        s.bedExists = AnyStationExists(StationType.Bed);
        s.potExists = AnyStationExists(StationType.Pot);
        s.fireExists = AnyStationExists(StationType.Fire);

        s.woodCarried = wood;

        // IMPORTANT: only the target need starts "unsatisfied"
        s.sleepSatisfied = targetNeed != NeedType.Sleep;
        s.hungerSatisfied = targetNeed != NeedType.Hunger;
        s.warmthSatisfied = targetNeed != NeedType.Warmth;

        return s;
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
            if (d < bestDist)
            {
                bestDist = d;
                best = st;
            }
        }

        return best;
    }
}
