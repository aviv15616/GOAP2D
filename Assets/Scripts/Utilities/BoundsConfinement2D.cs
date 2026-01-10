using UnityEngine;

public class BoundsConfinement2D : MonoBehaviour
{
    [Tooltip("Drag your Tilemap (that has TilemapBoundsProvider) here")]
    public TilemapBoundsProvider boundsProvider;

    [Tooltip("Optional: set if you move with Rigidbody2D")]
    public Rigidbody2D rb;

    [Tooltip("How far inside the bounds to keep the agent (prevents edge jitter)")]
    public float padding = 0.05f;

    private void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void LateUpdate()
    {
        if (boundsProvider == null) return;

        Bounds b = boundsProvider.WorldBounds;

        float minX = b.min.x + padding;
        float maxX = b.max.x - padding;
        float minY = b.min.y + padding;
        float maxY = b.max.y - padding;

        if (rb != null)
        {
            Vector2 p = rb.position;
            Vector2 clamped = new Vector2(
                Mathf.Clamp(p.x, minX, maxX),
                Mathf.Clamp(p.y, minY, maxY)
            );

            if (clamped != p)
            {
                rb.position = clamped;
                rb.linearVelocity = Vector2.zero; // critical: stop physics from pushing out again
                rb.angularVelocity = 0f;
            }
        }
        else
        {
            Vector3 p = transform.position;
            float x = Mathf.Clamp(p.x, minX, maxX);
            float y = Mathf.Clamp(p.y, minY, maxY);

            if (x != p.x || y != p.y)
                transform.position = new Vector3(x, y, p.z);
        }
    }
}
