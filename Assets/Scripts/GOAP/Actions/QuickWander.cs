using UnityEngine;

public class QuickWander : MonoBehaviour
{
    public float speed = 2f;
    public float radius = 3f;
    public float reach = 0.2f;

    private Rigidbody2D rb;
    private Vector2 target;
    private bool hasTarget;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (!hasTarget)
        {
            Vector2 basePos = rb ? rb.position : (Vector2)transform.position;
            target = basePos + Random.insideUnitCircle * radius;
            hasTarget = true;
        }

        Vector2 pos = rb ? rb.position : (Vector2)transform.position;
        if (Vector2.Distance(pos, target) <= reach)
        {
            hasTarget = false;
            return;
        }

        Vector2 next = Vector2.MoveTowards(pos, target, speed * Time.deltaTime);

        if (rb) rb.MovePosition(next);
        else transform.position = next;
    }
}
