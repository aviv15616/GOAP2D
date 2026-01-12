using System.Collections.Generic;
using UnityEngine;

public class StationRegistry : MonoBehaviour
{
    public static StationRegistry Instance { get; private set; }

    private readonly List<Station> _stations = new();
    public IReadOnlyList<Station> AllStations => _stations;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RebuildCache(); // optional safety at startup
    }

    [ContextMenu("Rebuild Station Cache")]
    public void RebuildCache()
    {
        _stations.Clear();

        var all = FindObjectsByType<Station>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var s in all)
            Register(s);
    }

    public void Register(Station s)
    {
        if (s == null)
            return;
        if (!_stations.Contains(s))
            _stations.Add(s);
    }

    public void Unregister(Station s)
    {
        if (s == null)
            return;
        _stations.Remove(s);
    }
}
