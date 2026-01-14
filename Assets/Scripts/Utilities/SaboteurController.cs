using UnityEngine;

public class SaboteurController : MonoBehaviour
{
    [Header("Toggle")]
    public bool enableSabotage = true;

    [Header("Refs")]
    public GoapAgent me;
    public StationRegistry registry;

    [Header("Timing")]
    public float scanEvery = 0.35f;
    public float mustLeadBySeconds = 0.20f;

    [Header("Move/Destroy")]
    public float destroyDistance = 0.35f;
    public float sabotageCooldown = 1.0f;

    float _scanT;
    float _cooldownT;

    bool _sabotaging;
    Station _targetStation;
    Vector2 _targetPos;

    void Awake()
    {
        if (!me) me = GetComponent<GoapAgent>();
        if (!registry) registry = me ? me.registry : FindFirstObjectByType<StationRegistry>();
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (!enableSabotage || me == null || registry == null)
            return;

        _cooldownT -= dt;

        if (_sabotaging)
        {
            TickSabotage(dt);
            return;
        }

        // רק לסרוק מדי פעם
        _scanT -= dt;
        if (_scanT > 0) return;
        _scanT = scanEvery;

        if (_cooldownT > 0) return;

        // למצוא קורבן + תחנה שאפשר להקדים
        if (TryPickSabotageTarget(out var st, out var pos))
        {
            _targetStation = st;
            _targetPos = pos;
            BeginSabotage();
        }
    }

    void BeginSabotage()
    {
        _sabotaging = true;

        // מכבים את ה-GOAP כדי שלא יתכנן בזמן הסבוטאז'
        me.enabled = false;
    }


    void EndSabotage()
    {
        _sabotaging = false;
        _targetStation = null;

        _cooldownT = sabotageCooldown;

        // מחזירים את ה-GOAP
        me.enabled = true;
    }

    void TickSabotage(float dt)
    {
        if (_targetStation == null || !_targetStation.Exists)
        {
            EndSabotage();
            return;
        }

        // לזוז לתחנה
        if (me.mover != null)
        {
            me.mover.MoveTowards(_targetPos, dt, destroyDistance);
        }

        // אם קרוב מספיק -> להרוס
        float d = Vector2.Distance(transform.position, _targetPos);
        if (d <= destroyDistance)
        {
            Destroy(_targetStation.gameObject);

            // אם אצלך צריך עדכון registry cache:
            // registry.RebuildCache();  (רק אם יש פונקציה כזאת אצלך)

            EndSabotage();
        }
    }

    bool TryPickSabotageTarget(out Station bestStation, out Vector2 bestPos)
    {
        bestStation = null;
        bestPos = default;

        var agents = FindObjectsByType<GoapAgent>(FindObjectsSortMode.None);
        if (agents == null || agents.Length == 0)
            return false;

        float bestSlack = -9999f; // כמה שניות אני מקדים אותו

        foreach (var a in agents)
        {
            if (a == null || a == me) continue;

            // מחפשים NPC שהפעולה הנוכחית שלו היא UseStationAction
            var act = a.CurrentAction as UseStationAction;
            if (act == null) continue;

            StationType type = act.RuntimeStationType;

            // למצוא את התחנה שהקורבן הולך אליה בפועל (Station reference)
            if (!TryGetBestStation(type, a.transform.position, out var victimStation))
                continue;

            Vector2 stationPos = victimStation.InteractPos;

            // ETA של הקורבן אל התחנה
            float victimEta = a.EstimateTravelTime(a.transform.position, stationPos);

            // ETA שלי אל אותה תחנה
            float myEta = me.EstimateTravelTime(me.transform.position, stationPos);

            float slack = victimEta - myEta; // חיובי = אני מקדים
            if (slack > mustLeadBySeconds && slack > bestSlack)
            {
                bestSlack = slack;
                bestStation = victimStation;
                bestPos = stationPos;
            }
        }

        return bestStation != null;
    }

    bool TryGetBestStation(StationType type, Vector2 from, out Station best)
    {
        best = null;
        float bestTime = 9999f;

        foreach (var st in registry.AllStations)
        {
            if (st == null) continue;
            if (!st.Exists) continue;
            if (st.type != type) continue;

            float t = me.EstimateTravelTime(from, st.InteractPos);
            if (t < bestTime)
            {
                bestTime = t;
                best = st;
            }
        }
        return best != null;
    }
}
