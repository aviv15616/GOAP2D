using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapBoundsPrinter : MonoBehaviour
{
    public Tilemap tilemap;

    [ContextMenu("Print Tilemap World Bounds")]
    public void PrintBounds()
    {
        if (tilemap == null)
            tilemap = GetComponent<Tilemap>();
        if (tilemap == null)
        {
            return;
        }

        // cell bounds -> world bounds
        var cellBounds = tilemap.cellBounds;
        var min = tilemap.CellToWorld(cellBounds.min);
        var max = tilemap.CellToWorld(cellBounds.max);

        float width = Mathf.Abs(max.x - min.x);
        float height = Mathf.Abs(max.y - min.y);
    }
}
