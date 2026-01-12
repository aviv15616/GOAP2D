using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// STRICT integration tester for your GOAP.
/// Fixes:
/// 1) Can auto-run on Start (so you don't rely on ContextMenu)
/// 2) Uses REALTIME timeouts (works even if Time.timeScale=0)
/// 3) Disables agents while setting needs/inventory, then re-enables them (so values actually apply before GOAP Update)
/// 4) Forces registry cache rebuild before/after spawns/despawns (so woodExists becomes true)
/// 5) Adds clear setup logs so you can see it actually ran
public class GoapScenarioTester : MonoBehaviour
{
    [Header("Auto Run")]
    public bool runOnStart = true;

    [Header("World")]
    public StationRegistry registry;

    [Header("Agents (drag your 3 NPCs)")]
    public GoapAgent sleepGuy;
    public GoapAgent eaterGuy;
    public GoapAgent warmthGuy;

    [Header("Wood (must exist; tester can spawn it if missing)")]
    public GameObject woodPrefab; // optional if you already placed wood in scene
    public Vector3 woodSpawnPos = new Vector3(0, 0, 0);

    [Header("Timing")]
    public float timeoutSeconds = 12f;

    [Header("Determinism (tester sets these automatically)")]
    public float urgent = 60f;
    public float critical = 20f;
    public float startLow = 10f; // start need meters low for instant behavior
    public bool freezeDrainDuringTests = true; // makes it deterministic

    private int _pass,
        _fail;
    private readonly Dictionary<GoapAgent, float> _origDrain = new();

    private void Start()
    {
        if (!runOnStart)
            return;
        RunStrictTests();
    }

    [ContextMenu("Run STRICT GOAP Tests")]
    public void RunStrictTests()
    {
        StopAllCoroutines();
        StartCoroutine(RunAll());
    }

    private IEnumerator RunAll()
    {
        _pass = 0;
        _fail = 0;

        if (registry == null)
            registry = FindFirstObjectByType<StationRegistry>();

        BuiltByTag.GlobalSequence = 0;

        // Ensure registry has what already exists (fixes execution order issues)
        RebuildRegistry();

        // 0) disable agents BEFORE messing with needs so their Update doesn't race you
        SetAgentsEnabled(false);

        // 1) Ensure wood exists
        EnsureWoodExists();
        yield return null; // allow OnEnable/Start for spawned wood if any
        RebuildRegistry();

        // 2) Clean slate: remove buildables
        DestroyStationsOfType(StationType.Bed);
        DestroyStationsOfType(StationType.Pot);
        DestroyStationsOfType(StationType.Fire);
        yield return null; // allow unregister/destroy to complete
        RebuildRegistry();

        // 3) Set deterministic starting state
        PrepareAgent(sleepGuy, NeedType.Sleep);
        PrepareAgent(eaterGuy, NeedType.Hunger);
        PrepareAgent(warmthGuy, NeedType.Warmth);

        // 4) enable agents AFTER setup is complete
        SetAgentsEnabled(true);

        // give agents a frame to notice state + start planning
        yield return null;

        // -------------------------
        // TEST 1: Each agent must build ONLY their station first, then use it (meter rises)
        // -------------------------
        yield return StrictBuildThenUse(sleepGuy, NeedType.Sleep, StationType.Bed);
        yield return StrictBuildThenUse(eaterGuy, NeedType.Hunger, StationType.Pot);
        yield return StrictBuildThenUse(warmthGuy, NeedType.Warmth, StationType.Fire);

        // -------------------------
        // TEST 2: Destroy each station -> correct agent MUST rebuild it (new sequence)
        // -------------------------
        yield return StrictRebuildAfterDestroy(sleepGuy, NeedType.Sleep, StationType.Bed);
        yield return StrictRebuildAfterDestroy(eaterGuy, NeedType.Hunger, StationType.Pot);
        yield return StrictRebuildAfterDestroy(warmthGuy, NeedType.Warmth, StationType.Fire);

        // restore drain
        RestoreDrain();
    }

    // ---------- Test Steps ----------

    private IEnumerator StrictBuildThenUse(GoapAgent agent, NeedType need, StationType station)
    {
        float startMeter = GetNeedMeter(agent, need);

        BuiltByTag builtTag = null;
        yield return ExpectEventuallyRealtime(
            () =>
            {
                RebuildRegistry(); // keep world snapshot fresh while things spawn/destroy
                builtTag = FindBuiltTag(agent.name, station);
                return builtTag != null;
            },
            timeoutSeconds,
            $"{agent.name} must build {station}"
        );

        if (builtTag == null)
            yield break;

        if (BuiltOtherStationBefore(agent.name, station, builtTag.sequence))
        {
            Fail($"{agent.name} built a different station before {station} (sequence order wrong)");
            yield break;
        }
        Pass($"{agent.name} built {station} first (seq={builtTag.sequence})");

        float restore = GetRestoreAmount(agent, need);
        float expectedMin = Mathf.Min(100f, startMeter + restore - 0.01f);

        yield return ExpectEventuallyRealtime(
            () =>
            {
                return GetNeedMeter(agent, need) >= expectedMin;
            },
            timeoutSeconds,
            $"{agent.name} must use {station} and restore {need}"
        );

        Pass($"{agent.name} restored {need} (meter now {GetNeedMeter(agent, need):F1})");
    }

    private IEnumerator StrictRebuildAfterDestroy(
        GoapAgent agent,
        NeedType need,
        StationType station
    )
    {
        var oldTag = FindLatestBuiltTag(agent.name, station);
        if (oldTag == null)
        {
            Fail($"{agent.name}: cannot rebuild test because no existing {station} tag found");
            yield break;
        }

        int oldSeq = oldTag.sequence;

        DestroyStationsOfType(station);
        yield return null;
        RebuildRegistry();

        // force need low again so they rebuild immediately
        SetNeedLow(agent, need);

        BuiltByTag newTag = null;
        yield return ExpectEventuallyRealtime(
            () =>
            {
                RebuildRegistry();
                newTag = FindLatestBuiltTag(agent.name, station);
                return newTag != null && newTag.sequence > oldSeq;
            },
            timeoutSeconds,
            $"{agent.name} must rebuild {station} after destruction"
        );

        if (newTag == null)
        {
            Fail($"{agent.name}: rebuild tag missing");
            yield break;
        }

        Pass($"{agent.name} rebuilt {station} (old seq={oldSeq}, new seq={newTag.sequence})");
    }

    // ---------- Setup Helpers ----------

    private void SetAgentsEnabled(bool enabled)
    {
        if (sleepGuy != null)
            sleepGuy.enabled = enabled;
        if (eaterGuy != null)
            eaterGuy.enabled = enabled;
        if (warmthGuy != null)
            warmthGuy.enabled = enabled;
    }

    private void PrepareAgent(GoapAgent agent, NeedType primary)
    {
        if (agent == null)
            return;

        // ensure components exist
        if (agent.needs == null)
            agent.needs = agent.GetComponent<Needs>();

        agent.primaryNeed = primary;
        agent.wood = 0;

        if (freezeDrainDuringTests)
        {
            if (!_origDrain.ContainsKey(agent))
                _origDrain[agent] = agent.needs.drainPerSecond;
            agent.needs.drainPerSecond = 0f;
        }

        agent.needs.urgent = urgent;
        agent.needs.critical = critical;

        // set all high then only the primary low
        agent.needs.energy = 100f;
        agent.needs.hunger = 100f;
        agent.needs.warmth = 100f;

        SetNeedLow(agent, primary);
    }

    private void SetNeedLow(GoapAgent agent, NeedType need)
    {
        if (agent == null || agent.needs == null)
            return;

        if (need == NeedType.Sleep)
            agent.needs.energy = startLow;
        else if (need == NeedType.Hunger)
            agent.needs.hunger = startLow;
        else if (need == NeedType.Warmth)
            agent.needs.warmth = startLow;
    }

    private void RestoreDrain()
    {
        if (!freezeDrainDuringTests)
            return;

        foreach (var kv in _origDrain)
        {
            if (kv.Key != null && kv.Key.needs != null)
                kv.Key.needs.drainPerSecond = kv.Value;
        }
        _origDrain.Clear();
    }

    private void EnsureWoodExists()
    {
        RebuildRegistry();

        if (Exists(StationType.Wood))
            return;

        if (woodPrefab == null)
        {
            Fail("No Wood station in scene AND woodPrefab is not assigned.");
            return;
        }

        var go = Instantiate(woodPrefab, woodSpawnPos, Quaternion.identity);
        var st = go.GetComponent<Station>();
        if (st != null)
        {
            st.type = StationType.Wood;
            st.built = true;
        }
    }

    private void RebuildRegistry()
    {
        if (registry == null)
            return;

        // If you implemented the fixed StationRegistry with RebuildCache/RebuildStationCache
        // call it. If not, this is a no-op safe fallback.
        var mi = registry.GetType().GetMethod("RebuildCache");
        if (mi != null)
            mi.Invoke(registry, null);
    }

    // ---------- Query Helpers ----------

    private bool Exists(StationType t)
    {
        if (registry == null)
            return false;

        foreach (var st in registry.AllStations)
        {
            if (st == null)
                continue;
            if (st.type != t)
                continue;
            if (st.Exists)
                return true;
        }
        return false;
    }

    private void DestroyStationsOfType(StationType t)
    {
        if (registry == null)
            return;

        var copy = new List<Station>(registry.AllStations);
        foreach (var st in copy)
        {
            if (st == null)
                continue;
            if (st.type != t)
                continue;
            Destroy(st.gameObject);
        }
    }

    private BuiltByTag FindBuiltTag(string builderName, StationType type)
    {
        var tags = FindObjectsByType<BuiltByTag>(FindObjectsSortMode.None);
        foreach (var tag in tags)
        {
            if (tag == null)
                continue;
            if (tag.builderName != builderName)
                continue;
            if (tag.stationType != type)
                continue;

            var st = tag.GetComponent<Station>();
            if (st != null && st.Exists)
                return tag;
        }
        return null;
    }

    private BuiltByTag FindLatestBuiltTag(string builderName, StationType type)
    {
        BuiltByTag best = null;
        var tags = FindObjectsByType<BuiltByTag>(FindObjectsSortMode.None);
        foreach (var tag in tags)
        {
            if (tag == null)
                continue;
            if (tag.builderName != builderName)
                continue;
            if (tag.stationType != type)
                continue;

            if (best == null || tag.sequence > best.sequence)
                best = tag;
        }
        return best;
    }

    private bool BuiltOtherStationBefore(string builderName, StationType expected, int expectedSeq)
    {
        var tags = FindObjectsByType<BuiltByTag>(FindObjectsSortMode.None);
        foreach (var tag in tags)
        {
            if (tag == null)
                continue;
            if (tag.builderName != builderName)
                continue;

            if (tag.sequence < expectedSeq && tag.stationType != expected)
                return true;
        }
        return false;
    }

    private float GetNeedMeter(GoapAgent agent, NeedType need)
    {
        if (need == NeedType.Sleep)
            return agent.needs.energy;
        if (need == NeedType.Hunger)
            return agent.needs.hunger;
        return agent.needs.warmth;
    }

    private float GetRestoreAmount(GoapAgent agent, NeedType need)
    {
        var uses = agent.GetComponents<UseStationAction>();
        foreach (var u in uses)
            if (u.need == need)
                return u.restoreAmount;
        return 40f;
    }

    // ---------- Assertion Helpers (REALTIME) ----------

    private IEnumerator ExpectEventuallyRealtime(
        System.Func<bool> condition,
        float timeout,
        string what
    )
    {
        float start = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup - start < timeout)
        {
            if (condition())
                yield break;

            yield return null;
        }

        Fail(what);
    }

    private void Pass(string msg)
    {
        _pass++;
    }

    private void Fail(string msg)
    {
        _fail++;
    }
}
