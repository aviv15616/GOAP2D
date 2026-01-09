// Assets/Scripts/GOAP/StationManager.cs
// FIX: מוסיף GetActiveStations + GetNearestStation כדי לתקן את UseStation/Sensor
using System.Collections.Generic;
using UnityEngine;

public class StationManager : MonoBehaviour
{
    public static StationManager Instance;

    [System.Serializable]
    public class StationSpot
    {
        public Transform spot;
        public bool occupied;
    }

    [Header("Max stations of each type")]
    public int maxWater = 2;
    public int maxFood = 2;
    public int maxFire = 2;

    [Header("Available build spots")]
    public List<StationSpot> waterSpots = new List<StationSpot>();
    public List<StationSpot> foodSpots = new List<StationSpot>();
    public List<StationSpot> fireSpots = new List<StationSpot>();

    private List<GameObject> waterStations = new List<GameObject>();
    private List<GameObject> foodStations = new List<GameObject>();
    private List<GameObject> fireStations = new List<GameObject>();

    private void Awake()
    {
        Instance = this;
    }

    // ------------------------------
    //  GET / QUERY
    // ------------------------------
    public bool HasStation(string type)
    {
        switch (type)
        {
            case "Water": return waterStations.Count > 0;
            case "Food": return foodStations.Count > 0;
            case "Fire": return fireStations.Count > 0;
            default: return false;
        }
    }

    public bool IsAtMax(string type)
    {
        switch (type)
        {
            case "Water": return waterStations.Count >= maxWater;
            case "Food": return foodStations.Count >= maxFood;
            case "Fire": return fireStations.Count >= maxFire;
            default: return true;
        }
    }

    // FIX: מה ש-UseStation מחפש
    public List<GameObject> GetActiveStations(string type)
    {
        switch (type)
        {
            case "Water": return waterStations;
            case "Food": return foodStations;
            case "Fire": return fireStations;
            default: return null;
        }
    }

    // FIX: מה ש-Sensor מחפש (מחזיר Transform של התחנה הקרובה)
    public Transform GetNearestStation(string type, Vector3 fromPosition)
    {
        var list = GetActiveStations(type);
        if (list == null || list.Count == 0) return null;

        GameObject best = null;
        float bestDist = float.MaxValue;

        foreach (var go in list)
        {
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

    // ------------------------------
    //  BUILD LOGIC
    // ------------------------------
    public Transform GetFreeBuildSpot(string type)
    {
        List<StationSpot> list = null;
        switch (type)
        {
            case "Water": list = waterSpots; break;
            case "Food": list = foodSpots; break;
            case "Fire": list = fireSpots; break;
        }

        if (list == null) return null;

        foreach (var s in list)
        {
            if (!s.occupied)
                return s.spot;
        }

        return null;
    }

    public void RegisterStation(string type, GameObject station)
    {
        switch (type)
        {
            case "Water": waterStations.Add(station); break;
            case "Food": foodStations.Add(station); break;
            case "Fire": fireStations.Add(station); break;
        }
    }

    // ------------------------------
    //  USAGE LOGIC (1 NPC max!)
    // ------------------------------
    public bool RequestUse(GameObject station)
    {
        var state = station != null ? station.GetComponent<StationState>() : null;
        if (state == null) return false;
        if (state.inUse) return false;

        state.inUse = true;
        return true;
    }

    public void ReleaseUse(GameObject station)
    {
        var state = station != null ? station.GetComponent<StationState>() : null;
        if (state != null)
            state.inUse = false;
    }
}
