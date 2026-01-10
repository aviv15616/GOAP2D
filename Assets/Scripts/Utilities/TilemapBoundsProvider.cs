using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapBoundsProvider : MonoBehaviour
{
    public Tilemap tilemap;
    private Bounds _worldBounds;

    private void Awake()
    {
        if (tilemap == null) tilemap = GetComponent<Tilemap>();
        RecalculateBounds();
    }

    [ContextMenu("Recalculate Bounds")]
    public void RecalculateBounds()
    {
        var b = tilemap.cellBounds;
        var min = tilemap.CellToWorld(b.min);
        var max = tilemap.CellToWorld(b.max);

        var center = (min + max) * 0.5f;
        var size = new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), 1f);
        _worldBounds = new Bounds(center, size);
    }

    public bool Contains(Vector2 p) =>
        p.x >= _worldBounds.min.x && p.x <= _worldBounds.max.x &&
        p.y >= _worldBounds.min.y && p.y <= _worldBounds.max.y;

    public Bounds WorldBounds => _worldBounds;
}
