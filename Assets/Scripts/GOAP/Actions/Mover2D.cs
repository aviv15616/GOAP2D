using System.Collections.Generic;
using UnityEngine;

public class Mover2D : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 2.5f;
    public float arriveDistance = 0.15f;

    [Header("Bounds Confinement")]
    public TilemapBoundsProvider boundsProvider;

    [Tooltip("How far inside the bounds the agent must stay")]
    public float boundsMargin = 0.05f;

    [Header("Animation")]
    public Animator animator;
    public string moveXParam = "MoveX";
    public string moveYParam = "MoveY";

    [Header("Facing")]
    public float facingDeadZone = 0.001f;

    private Rigidbody2D _rb;
    private Vector2 _lastFacing = Vector2.down;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null)
            _rb = gameObject.AddComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.freezeRotation = true;

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
    }

    // ---------------- Public API ----------------

    public bool MoveTowards(Vector2 target, float dt) => MoveTowards(target, dt, arriveDistance);

    public bool FollowPath(List<Vector2> path, ref int index, float dt) =>
        FollowPath(path, ref index, dt, arriveDistance);

    // ---------------- Core Movement ----------------

    public bool MoveTowards(Vector2 target, float dt, float customArriveDistance)
    {
        dt = Mathf.Min(dt, 0.05f);
        float arrive = Mathf.Max(0.001f, customArriveDistance);

        Vector2 pos = _rb.position;
        Vector2 to = target - pos;

        // Arrived → freeze animation
        if (to.sqrMagnitude <= arrive * arrive)
        {
            FreezeAnimation();
            return true;
        }

        float dist = Mathf.Sqrt(to.sqrMagnitude);
        Vector2 dir = to / dist;

        if (dir.sqrMagnitude > facingDeadZone * facingDeadZone)
            _lastFacing = SnapToCardinal(dir);

        // animation
        animator.SetFloat(moveXParam, _lastFacing.x);
        animator.SetFloat(moveYParam, _lastFacing.y);
        animator.speed = 1f;

        float step = speed * dt;
        Vector2 next = pos + dir * Mathf.Min(step, dist);

        next = ClampIntoBounds(next);

        _rb.MovePosition(next);
        return false;
    }

    public bool FollowPath(List<Vector2> path, ref int index, float dt, float customArriveDistance)
    {
        if (path == null || path.Count == 0)
        {
            FreezeAnimation();
            return true;
        }

        float arrive = Mathf.Max(0.001f, customArriveDistance);
        float arriveSqr = arrive * arrive;

        index = Mathf.Clamp(index, 0, path.Count - 1);

        while (index < path.Count)
        {
            Vector2 d = path[index] - _rb.position;
            if (d.sqrMagnitude > arriveSqr)
                break;
            index++;
        }

        if (index >= path.Count)
        {
            FreezeAnimation();
            return true;
        }

        return MoveTowards(path[index], dt, arrive);
    }

    // ---------------- Animation ----------------

    private void FreezeAnimation()
    {
        animator.speed = 0f;
        animator.SetFloat(moveXParam, _lastFacing.x);
        animator.SetFloat(moveYParam, _lastFacing.y);
    }

    // ---------------- Bounds ----------------

    private Vector2 ClampIntoBounds(Vector2 p)
    {
        if (boundsProvider == null)
            return p;

        Bounds b = boundsProvider.WorldBounds;

        float minX = b.min.x + boundsMargin;
        float maxX = b.max.x - boundsMargin;
        float minY = b.min.y + boundsMargin;
        float maxY = b.max.y - boundsMargin;

        return new Vector2(Mathf.Clamp(p.x, minX, maxX), Mathf.Clamp(p.y, minY, maxY));
    }

    private static Vector2 SnapToCardinal(Vector2 v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
            return v.x >= 0 ? Vector2.right : Vector2.left;
        else
            return v.y >= 0 ? Vector2.up : Vector2.down;
    }
}
