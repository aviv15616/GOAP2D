using System.Collections.Generic;
using UnityEngine;

/// Put Rigidbody2D (Kinematic) + Collider2D (non-trigger) on the NPC.
/// Disable NPC-vs-NPC collisions via layer matrix.
public class Mover2D : MonoBehaviour
{
    public float speed = 2.5f;
    public float arriveDistance = 0.15f;

    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null) _rb = gameObject.AddComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.freezeRotation = true;
    }

    public bool MoveTowards(Vector2 target, float dt)
    {
        dt = Mathf.Min(dt, 0.05f);

        Vector2 pos = _rb.position;
        Vector2 to = target - pos;

        if (to.magnitude <= arriveDistance) return true;

        float step = speed * dt;
        Vector2 next = pos + to.normalized * Mathf.Min(step, to.magnitude);
        _rb.MovePosition(next);
        return false;
    }

    public bool FollowPath(List<Vector2> path, ref int index, float dt)
    {
        if (path == null || path.Count == 0) return true;
        index = Mathf.Clamp(index, 0, path.Count - 1);

        // advance if already close to current node
        while (index < path.Count && Vector2.Distance(_rb.position, path[index]) <= arriveDistance)
            index++;

        if (index >= path.Count) return true;

        MoveTowards(path[index], dt);
        return false;
    }
}
