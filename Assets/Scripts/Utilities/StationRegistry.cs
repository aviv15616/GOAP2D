using System.Collections.Generic;
using UnityEngine;

public class StationRegistry : MonoBehaviour
{
    public static StationRegistry Instance { get; private set; }

    private readonly List<Station> _stations = new();
    public IReadOnlyList<Station> AllStations => _stations;

    private void Awake()
    {
        Instance = this;
        RebuildCache(); // ✅ important
    }

    private void OnEnable()
    {
        if (Instance == null) Instance = this;
        RebuildCache(); // ✅ important
    }

    [ContextMenu("Rebuild Station Cache")]
    public void RebuildCache()
    {
        _stations.Clear();

        // include inactive too, so you can toggle things
        var all = FindObjectsByType<Station>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var s in all)
            Register(s);
    }

    public void Register(Station s)
    {
        if (s != null && !_stations.Contains(s))
            _stations.Add(s);
    }

    public void Unregister(Station s)
    {
        if (s != null)
            _stations.Remove(s);
    }
}
