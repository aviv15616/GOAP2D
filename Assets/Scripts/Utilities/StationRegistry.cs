using System.Collections.Generic;
using UnityEngine;

public class StationRegistry : MonoBehaviour
{
    public static StationRegistry Instance { get; private set; }

    private readonly List<Station> _stations = new();

    private void Awake() => Instance = this;

    public void Register(Station s)
    {
        if (s != null && !_stations.Contains(s)) _stations.Add(s);
    }

    public void Unregister(Station s)
    {
        if (s != null) _stations.Remove(s);
    }

    public IReadOnlyList<Station> AllStations => _stations;
}
