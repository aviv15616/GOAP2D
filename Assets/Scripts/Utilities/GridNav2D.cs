using System.Collections.Generic;
using UnityEngine;

/// A* grid navigation using TilemapBoundsProvider for the world rectangle.
/// Walkable cells are those inside bounds AND not overlapping obstacle layers.
public class GridNav2D : MonoBehaviour
{
    [Header("Bounds")]
    public TilemapBoundsProvider boundsProvider;

    [Header("Grid")]
    [Tooltip("World units per grid cell. Usually 0.5, 1, or your tile size.")]
    public float cellSize = 1f;

    [Header("Obstacles")]
    public LayerMask obstacleLayers;
    [Tooltip("Radius used to test if a grid cell is blocked (should be smaller than cellSize/2).")]
    public float blockedCheckRadius = 0.35f;

    [Header("Neighbors")]
    public bool allowDiagonal = false;

    [Header("Safety (prevents Unity freeze)")]
    [Tooltip("Hard cap on expanded nodes per path. If exceeded, path returns null.")]
    public int maxExpandedNodes = 4000;

    [Tooltip("Hard cap on open list size. If exceeded, path returns null.")]
    public int maxOpenSize = 8000;

    [Tooltip("Hard cap on total grid cells (width*height). If exceeded, path returns null.")]
    public int maxGridCells = 120000; // e.g. 300x400

    private Bounds WorldBounds => boundsProvider != null
        ? boundsProvider.WorldBounds
        : new Bounds(Vector3.zero, new Vector3(50, 50, 0));

    private Vector2 Origin => new Vector2(WorldBounds.min.x, WorldBounds.min.y);

    private Vector2 CellCenter(int gx, int gy)
    {
        Vector2 o = Origin;
        return new Vector2(o.x + (gx + 0.5f) * cellSize, o.y + (gy + 0.5f) * cellSize);
    }

    private void WorldToGrid(Vector2 w, out int gx, out int gy)
    {
        Vector2 o = Origin;
        gx = Mathf.FloorToInt((w.x - o.x) / cellSize);
        gy = Mathf.FloorToInt((w.y - o.y) / cellSize);
    }

    private bool InBoundsCell(int gx, int gy)
    {
        Vector2 p = CellCenter(gx, gy);
        return boundsProvider == null || boundsProvider.Contains(p);
    }

    private bool IsWalkableCell(int gx, int gy)
    {
        if (!InBoundsCell(gx, gy)) return false;
        Vector2 p = CellCenter(gx, gy);
        return Physics2D.OverlapCircle(p, blockedCheckRadius, obstacleLayers) == null;
    }

    private int Heuristic(int ax, int ay, int bx, int by)
    {
        int dx = Mathf.Abs(ax - bx);
        int dy = Mathf.Abs(ay - by);

        if (!allowDiagonal) return dx + dy;

        int min = Mathf.Min(dx, dy);
        int max = Mathf.Max(dx, dy);
        return 14 * min + 10 * (max - min);
    }

    private IEnumerable<(int nx, int ny, int stepCost)> Neighbors(int x, int y)
    {
        yield return (x + 1, y, 10);
        yield return (x - 1, y, 10);
        yield return (x, y + 1, 10);
        yield return (x, y - 1, 10);

        if (!allowDiagonal) yield break;

        yield return (x + 1, y + 1, 14);
        yield return (x + 1, y - 1, 14);
        yield return (x - 1, y + 1, 14);
        yield return (x - 1, y - 1, 14);
    }

    private struct NodeKey
    {
        public int x, y;
        public NodeKey(int x, int y) { this.x = x; this.y = y; }
        public override int GetHashCode() => (x * 73856093) ^ (y * 19349663);
        public override bool Equals(object obj) => obj is NodeKey o && o.x == x && o.y == y;
    }

    /// Returns a list of world positions (cell centers) from start to goal (excluding start).
    public List<Vector2> FindPath(Vector2 startWorld, Vector2 goalWorld)
    {
        if (boundsProvider == null)
        {
            Debug.LogWarning("[GridNav2D] boundsProvider is not assigned.");
            return null;
        }

        // Guard: if bounds are huge, grid explodes => return null instead of freezing.
        var b = WorldBounds;
        int gridW = Mathf.CeilToInt(b.size.x / Mathf.Max(0.001f, cellSize));
        int gridH = Mathf.CeilToInt(b.size.y / Mathf.Max(0.001f, cellSize));
        if (gridW * gridH > maxGridCells)
        {
            Debug.LogWarning($"[GridNav2D] Grid too large ({gridW}x{gridH}={gridW * gridH}). Check TilemapBoundsProvider or increase cellSize.");
            return null;
        }

        WorldToGrid(startWorld, out int sx, out int sy);
        WorldToGrid(goalWorld, out int gx, out int gy);

        if (!IsWalkableCell(sx, sy))
        {
            if (!TryFindNearestWalkable(sx, sy, 6, out sx, out sy)) return null;
        }

        if (!IsWalkableCell(gx, gy))
        {
            if (!TryFindNearestWalkable(gx, gy, 6, out gx, out gy)) return null;
        }

        var startKey = new NodeKey(sx, sy);
        var goalKey = new NodeKey(gx, gy);

        var open = new List<NodeKey>(256);
        var openSet = new HashSet<NodeKey>();
        var cameFrom = new Dictionary<NodeKey, NodeKey>(1024);
        var gScore = new Dictionary<NodeKey, int>(1024);
        var fScore = new Dictionary<NodeKey, int>(1024);

        open.Add(startKey);
        openSet.Add(startKey);
        gScore[startKey] = 0;
        fScore[startKey] = Heuristic(sx, sy, gx, gy);

        int expanded = 0;

        while (open.Count > 0)
        {
            expanded++;
            if (expanded > maxExpandedNodes) return null;
            if (open.Count > maxOpenSize) return null;

            // Pick lowest f
            int bestIdx = 0;
            int bestF = int.MaxValue;
            for (int i = 0; i < open.Count; i++)
            {
                int f = fScore.TryGetValue(open[i], out var vv) ? vv : int.MaxValue;
                if (f < bestF) { bestF = f; bestIdx = i; }
            }

            NodeKey current = open[bestIdx];
            open.RemoveAt(bestIdx);
            openSet.Remove(current);

            if (current.Equals(goalKey))
                return ReconstructPath(cameFrom, current, startWorld);

            foreach (var (nx, ny, stepCost) in Neighbors(current.x, current.y))
            {
                if (!IsWalkableCell(nx, ny)) continue;

                var nk = new NodeKey(nx, ny);
                int tentativeG = gScore[current] + stepCost;

                if (!gScore.TryGetValue(nk, out int oldG) || tentativeG < oldG)
                {
                    cameFrom[nk] = current;
                    gScore[nk] = tentativeG;
                    fScore[nk] = tentativeG + Heuristic(nx, ny, gx, gy);

                    if (!openSet.Contains(nk))
                    {
                        open.Add(nk);
                        openSet.Add(nk);
                    }
                }
            }
        }

        return null;
    }

    private List<Vector2> ReconstructPath(Dictionary<NodeKey, NodeKey> cameFrom, NodeKey current, Vector2 startWorld)
    {
        var rev = new List<Vector2>();
        while (cameFrom.TryGetValue(current, out var prev))
        {
            rev.Add(CellCenter(current.x, current.y));
            current = prev;
        }
        rev.Reverse();

        if (rev.Count > 0 && Vector2.Distance(rev[0], startWorld) < cellSize * 0.25f)
            rev.RemoveAt(0);

        return rev;
    }

    private bool TryFindNearestWalkable(int cx, int cy, int radius, out int wx, out int wy)
    {
        wx = cx; wy = cy;
        for (int r = 0; r <= radius; r++)
        {
            for (int y = -r; y <= r; y++)
            {
                for (int x = -r; x <= r; x++)
                {
                    if (Mathf.Abs(x) != r && Mathf.Abs(y) != r) continue;
                    int tx = cx + x;
                    int ty = cy + y;
                    if (IsWalkableCell(tx, ty))
                    {
                        wx = tx; wy = ty;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    // IMPORTANT: Keep this cheap. Do NOT call FindPath here.
    public float EstimatePathTime(Vector2 from, Vector2 to, float speed)
    {
        if (speed <= 0.001f) return 9999f;
        return Vector2.Distance(from, to) / speed;
    }
}
