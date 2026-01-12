// BuildSpotManager.cs (UPDATED: picks best spot by TRAVEL TIME, supports per-agent reservations)
//
// Works with your existing BuildSpot:
// - BuildSpot.forType
// - BuildSpot.Pos
// - BuildSpot.occupied
//
// Key additions:
// - TryGetBestBuildSpotPos(type, agent, from, out pos): for planning cost estimation
// - TryReserveBestSpot(type, agent, from, out pos): for runtime (prevents two NPCs building same spot)
// - ReleaseSpot(...) now respects ownership (won't free someone else's reservation by mistake)

using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class BuildSpotManager : MonoBehaviour
{
    [Tooltip("If empty, auto-fills from children BuildSpot components.")]
    public BuildSpot[] spots;

    // Tracks who reserved which spot (without modifying BuildSpot)
    private readonly Dictionary<BuildSpot, GoapAgent> _ownerBySpot = new();

    private void Awake()
    {
        if (spots == null || spots.Length == 0)
            spots = GetComponentsInChildren<BuildSpot>(true);

        // Clean nulls + ensure dictionary doesn't hold dead refs
        _ownerBySpot.Clear();
    }

    /// <summary>
    /// Legacy: returns the first spot of that type (can be "global/fixed" and cause bad planning).
    /// Kept for compatibility, but DO NOT use for GOAP decisions.
    /// </summary>
    public Vector2 GetSpotPosition(StationType type)
    {
        var spot = GetSpot(type);
        return spot != null ? spot.Pos : (Vector2)transform.position;
    }

    /// <summary>
    /// Planning helper: choose the best (reachable) spot by TRAVEL TIME from 'from'.
    /// Ignores occupancy by default so planning can still estimate routes.
    /// </summary>
    public bool TryGetBestBuildSpotPos(
        StationType type,
        GoapAgent agent,
        Vector2 from,
        out Vector2 worldPos,
        bool ignoreOccupied = true
    )
    {
        worldPos = default;

        if (agent == null)
            return false;

        bool found = false;
        float bestT = 9999f;

        if (spots == null || spots.Length == 0)
            return false;

        for (int i = 0; i < spots.Length; i++)
        {
            var s = spots[i];
            if (s == null)
                continue;
            if (s.forType != type)
                continue;

            if (!ignoreOccupied && s.occupied)
                continue;

            float t = agent.EstimateTravelTime(from, s.Pos);
            if (t >= 9999f)
                continue; // unreachable

            if (t < bestT)
            {
                bestT = t;
                worldPos = s.Pos;
                found = true;
            }
        }

        return found;
    }

    /// <summary>
    /// Runtime: reserves the best FREE spot by TRAVEL TIME from the agent.
    /// This prevents multiple NPCs from building on the same spot.
    /// </summary>
    public bool TryReserveBestSpot(
        StationType type,
        GoapAgent agent,
        Vector2 from,
        out Vector2 worldPos
    )
    {
        worldPos = default;

        if (agent == null)
            return false;
        if (spots == null || spots.Length == 0)
            return false;

        BuildSpot best = null;
        float bestT = 9999f;

        for (int i = 0; i < spots.Length; i++)
        {
            var s = spots[i];
            if (s == null)
                continue;
            if (s.forType != type)
                continue;

            // already occupied by someone else
            if (s.occupied)
            {
                // if it's occupied but owned by THIS agent, allow "re-reserve" (idempotent)
                if (_ownerBySpot.TryGetValue(s, out var owner) && owner == agent)
                {
                    worldPos = s.Pos;
                    return true;
                }
                continue;
            }

            float t = agent.EstimateTravelTime(from, s.Pos);
            if (t >= 9999f)
                continue;

            if (t < bestT)
            {
                bestT = t;
                best = s;
            }
        }

        if (best == null)
            return false;

        best.occupied = true;
        _ownerBySpot[best] = agent;
        worldPos = best.Pos;
        return true;
    }

    /// <summary>
    /// Backward compatible reservation (first spot of type).
    /// Prefer TryReserveBestSpot(...) instead.
    /// </summary>
    public bool TryReserveSpot(StationType type, GoapAgent agent, out Vector2 worldPos)
    {
        worldPos = default;

        var spot = GetSpot(type);
        if (spot == null)
            return false;

        if (spot.occupied)
        {
            if (_ownerBySpot.TryGetValue(spot, out var owner) && owner == agent)
            {
                worldPos = spot.Pos;
                return true;
            }
            return false;
        }

        spot.occupied = true;
        _ownerBySpot[spot] = agent;
        worldPos = spot.Pos;
        return true;
    }

    /// <summary>
    /// Releases the spot reserved by this agent.
    /// If agent is null, releases the first spot of that type (legacy).
    /// </summary>
    public void ReleaseSpot(StationType type, GoapAgent agent)
    {
        var spot = GetSpot(type);
        if (spot == null)
            return;

        // If we track an owner, only that owner may release.
        if (_ownerBySpot.TryGetValue(spot, out var owner))
        {
            if (agent != null && owner != agent)
                return; // don't release someone else's spot
            _ownerBySpot.Remove(spot);
        }

        spot.occupied = false;
    }

    public void EnsureInit()
    {
        if (spots == null || spots.Length == 0)
            spots = GetComponentsInChildren<BuildSpot>(true);
    }

    private BuildSpot GetSpot(StationType type)
    {
        if (spots == null)
            return null;

        for (int i = 0; i < spots.Length; i++)
        {
            var s = spots[i];
            if (s == null)
                continue;
            if (s.forType != type)
                continue;
            return s;
        }

        return null;
    }
}
