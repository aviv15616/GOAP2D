// BuildValidator.cs (REPLACE with this)
// Valid if: inside tilemap AND no collider (NPC/Station/etc) within a minimum clearance radius.
using UnityEngine;

public class BuildValidator : MonoBehaviour
{
    public TilemapBoundsProvider bounds;

    [Header("Keep-away rule (collider based)")]
    [Tooltip("Layers to keep distance from: NPC, Stations, Obstacles. EXCLUDE Ground.")]
    public LayerMask keepAwayLayers;

    [Tooltip("How far the NEW station must be from any collider in keepAwayLayers.")]
    public float minDistanceFromColliders = 1.5f;

    [Tooltip("Approx radius of the station footprint (added to minDistance).")]
    public float stationFootprintRadius = 0.5f;

    [Header("Search")]
    public float searchStep = 0.5f;
    public int searchRings = 8;

    private readonly Collider2D[] _hits = new Collider2D[16];

    public bool CanBuildAt(Vector2 pos)
    {
        // Rule 1: inside tilemap bounds
        if (bounds == null || !bounds.Contains(pos))
            return false;

        // Rule 2: no colliders too close (collider-based, real-time)
        float r = minDistanceFromColliders + stationFootprintRadius;

        int count = 0;
        Collider2D[] found = Physics2D.OverlapCircleAll(pos, r, keepAwayLayers);
        if (found != null)
        {
            count = found.Length;
        }
        return count == 0;
    }

    public bool TryFindValidPosition(Vector2 desired, out Vector2 found)
    {
        if (CanBuildAt(desired))
        {
            found = desired;
            return true;
        }

        for (int ring = 1; ring <= searchRings; ring++)
        {
            float d = ring * searchStep;

            Vector2[] offsets =
            {
                new Vector2(d, 0),
                new Vector2(-d, 0),
                new Vector2(0, d),
                new Vector2(0, -d),
                new Vector2(d, d),
                new Vector2(d, -d),
                new Vector2(-d, d),
                new Vector2(-d, -d),
            };

            foreach (var off in offsets)
            {
                Vector2 p = desired + off;
                if (CanBuildAt(p))
                {
                    found = p;
                    return true;
                }
            }
        }

        found = desired;
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Visualize the clearance radius around the object for debugging
        Gizmos.DrawWireSphere(
            transform.position,
            minDistanceFromColliders + stationFootprintRadius
        );
    }
#endif
}
