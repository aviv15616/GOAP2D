// BuildSpotManager.cs
using UnityEngine;

[DisallowMultipleComponent]
public class BuildSpotManager : MonoBehaviour
{
    [Tooltip("If empty, auto-fills from children BuildSpot components.")]
    public BuildSpot[] spots;

    private void Awake()
    {
        if (spots == null || spots.Length == 0)
            spots = GetComponentsInChildren<BuildSpot>(true);
    }

    public Vector2 GetSpotPosition(StationType type)
    {
        var spot = GetSpot(type);
        return spot != null ? spot.Pos : (Vector2)transform.position;
    }

    public bool TryReserveSpot(StationType type, GoapAgent agent, out Vector2 worldPos)
    {
        worldPos = default;

        var spot = GetSpot(type);
        if (spot == null) return false;
        if (spot.occupied) return false;

        spot.occupied = true;
        worldPos = spot.Pos;
        return true;
    }

    public void ReleaseSpot(StationType type, GoapAgent agent)
    {
        var spot = GetSpot(type);
        if (spot == null) return;

        spot.occupied = false;
    }

    private BuildSpot GetSpot(StationType type)
    {
        if (spots == null) return null;

        for (int i = 0; i < spots.Length; i++)
        {
            var s = spots[i];
            if (s == null) continue;
            if (s.forType != type) continue;
            return s;
        }

        return null;
    }
}
