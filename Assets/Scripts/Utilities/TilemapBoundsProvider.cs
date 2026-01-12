using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class TilemapBoundsProvider : MonoBehaviour
{
    public Tilemap tilemap;
    private Bounds _worldBounds;

    private void Awake()
    {
        if (tilemap == null)
            tilemap = GetComponent<Tilemap>();
        RecalculateBounds();
    }

    [ContextMenu("Recalculate Bounds")]
    public void RecalculateBounds()
    {
        if (tilemap == null)
            tilemap = GetComponent<Tilemap>();
        if (tilemap == null)
        {
            _worldBounds = new Bounds(Vector3.zero, Vector3.zero);
            return;
        }

        // IMPORTANT: tighten to only painted tiles
        tilemap.CompressBounds();

        // This is tight to actual tile content in LOCAL space
        Bounds lb = tilemap.localBounds;

        // Convert local bounds -> world bounds (supports scale/rotation safely)
        Vector3 c = tilemap.transform.TransformPoint(lb.center);

        // Transform extents vectors to world and build AABB extents
        Vector3 ex = tilemap.transform.TransformVector(new Vector3(lb.extents.x, 0f, 0f));
        Vector3 ey = tilemap.transform.TransformVector(new Vector3(0f, lb.extents.y, 0f));

        float worldExtX = Mathf.Abs(ex.x) + Mathf.Abs(ey.x);
        float worldExtY = Mathf.Abs(ex.y) + Mathf.Abs(ey.y);

        // Small inset to avoid float-edge "outside" flicker
        const float EPS = 0.01f;

        _worldBounds = new Bounds(
            c,
            new Vector3(
                Mathf.Max(0.1f, (worldExtX * 2f) - EPS),
                Mathf.Max(0.1f, (worldExtY * 2f) - EPS),
                1f
            )
        );
    }

    public bool Contains(Vector2 p) =>
        p.x >= _worldBounds.min.x
        && p.x <= _worldBounds.max.x
        && p.y >= _worldBounds.min.y
        && p.y <= _worldBounds.max.y;

    public Bounds WorldBounds => _worldBounds;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(_worldBounds.center, _worldBounds.size);
    }
}
