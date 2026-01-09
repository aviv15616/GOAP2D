// Assets/Scripts/World/StationManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class StationManager : MonoBehaviour
{
    public static StationManager Instance;

    [Header("Prefabs")]
    public GameObject waterPrefab;
    public GameObject foodPrefab;
    public GameObject firePrefab;

    [Header("Limits")]
    public int maxWater = 2;
    public int maxFood = 2;
    public int maxFire = 2;

    [Header("Placement")]
    [Tooltip("Grid for snapping. Optional (can be null).")]
    public Grid grid;
    [Tooltip("Optional tilemap that must contain a tile under the station (ground).")]
    public Tilemap groundTilemap;

    [Tooltip("How close you can place stations to each other.")]
    public float minStationDistance = 1.2f;

    [Tooltip("Overlap check radius/size for placement validity.")]
    public float overlapRadius = 0.45f;

    [Tooltip("Anything on these layers blocks placement (bed/wood/rocks/characters, etc).")]
    public LayerMask blockingLayers;

    private readonly List<GameObject> waterStations = new();
    private readonly List<GameObject> foodStations = new();
    private readonly List<GameObject> fireStations = new();

    private void Awake()
    {
        Instance = this;

        // אוספים תחנות קיימות בסצנה (אם יש)
        foreach (var st in FindObjectsByType<StationState>(FindObjectsSortMode.None))
            RegisterStation(st.stationType, st.gameObject);
    }

    public bool IsAtMax(string type)
    {
        return type switch
        {
            "Water" => waterStations.Count >= maxWater,
            "Food" => foodStations.Count >= maxFood,
            "Fire" => fireStations.Count >= maxFire,
            _ => true
        };
    }

    public bool HasStation(string type) => GetList(type)?.Count > 0;

    private List<GameObject> GetList(string type)
    {
        return type switch
        {
            "Water" => waterStations,
            "Food" => foodStations,
            "Fire" => fireStations,
            _ => null
        };
    }

    public void RegisterStation(string type, GameObject station)
    {
        var list = GetList(type);
        if (list == null || station == null) return;
        if (!list.Contains(station)) list.Add(station);
    }

    public Vector3 Snap(Vector3 worldPos)
    {
        if (grid == null) return worldPos;
        Vector3Int cell = grid.WorldToCell(worldPos);
        return grid.GetCellCenterWorld(cell);
    }

    public bool CanPlaceStation(string type, Vector3 worldPos)
    {
        if (string.IsNullOrEmpty(type)) return false;
        if (IsAtMax(type)) return false;

        worldPos = Snap(worldPos);

        // (אופציונלי) חייב להיות על קרקע
        if (groundTilemap != null)
        {
            Vector3Int cell = groundTilemap.WorldToCell(worldPos);
            if (!groundTilemap.HasTile(cell))
                return false;
        }

        // בדיקת חפיפה עם אובייקטים שחוסמים בנייה
        if (Physics2D.OverlapCircle(worldPos, overlapRadius, blockingLayers) != null)
            return false;

        // לא לשים תחנה ממש צמוד לתחנה אחרת
        if (minStationDistance > 0f)
        {
            foreach (var existing in FindObjectsByType<StationState>(FindObjectsSortMode.None))
            {
                if (existing == null) continue;
                if (Vector2.Distance(existing.transform.position, worldPos) < minStationDistance)
                    return false;
            }
        }

        return true;
    }

    public bool TryBuildStation(string type, Vector3 desiredPos, out GameObject built)
    {
        built = null;

        Vector3 pos = Snap(desiredPos);
        if (!CanPlaceStation(type, pos))
            return false;

        GameObject prefab = type switch
        {
            "Water" => waterPrefab,
            "Food" => foodPrefab,
            "Fire" => firePrefab,
            _ => null
        };
        if (prefab == null) return false;

        built = Instantiate(prefab, pos, Quaternion.identity);

        // מוודא StationState
        var state = built.GetComponent<StationState>();
        if (state == null) state = built.AddComponent<StationState>();
        state.stationType = type;
        state.inUse = false;

        RegisterStation(type, built);
        return true;
    }

    // שימוש בתחנות (נשאר כמו שהיה אצלך)
    public bool RequestUse(GameObject station)
    {
        var state = station != null ? station.GetComponent<StationState>() : null;
        if (state == null) return true;
        if (state.inUse) return false;
        state.inUse = true;
        return true;
    }

    public void ReleaseUse(GameObject station)
    {
        var state = station != null ? station.GetComponent<StationState>() : null;
        if (state != null) state.inUse = false;
    }
    // ------------------------------
    // Compatibility API (for existing UseStation/Sensor code)
    // ------------------------------
    public List<GameObject> GetActiveStations(string type)
    {
        return GetList(type);
    }

    public Transform GetNearestStation(string type, Vector3 fromPosition)
    {
        var list = GetList(type);
        if (list == null || list.Count == 0) return null;

        GameObject best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < list.Count; i++)
        {
            var go = list[i];
            if (go == null) continue;

            float d = Vector3.Distance(fromPosition, go.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = go;
            }
        }

        return best != null ? best.transform : null;
    }

}
