using UnityEngine;

[RequireComponent(typeof(Animator))]
public class WASDMove2D : MonoBehaviour
{
    public float speed = 3f;

    Animator anim;

    Vector2 lastMoveDir = Vector2.down; // default facing

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        float x = 0f;
        float y = 0f;

        if (Input.GetKey(KeyCode.A)) x = -1f;
        if (Input.GetKey(KeyCode.D)) x = 1f;
        if (Input.GetKey(KeyCode.S)) y = -1f;
        if (Input.GetKey(KeyCode.W)) y = 1f;

        Vector2 dir = new Vector2(x, y);

        if (dir.sqrMagnitude > 1f)
            dir.Normalize();

        // MOVE
        transform.position += (Vector3)(dir * speed * Time.deltaTime);

        // ANIMATION
        bool isMoving = dir != Vector2.zero;
        anim.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            lastMoveDir = dir;
            anim.SetFloat("MoveX", dir.x);
            anim.SetFloat("MoveY", dir.y);
        }
        else
        {
            // Keep last facing direction when idle
            anim.SetFloat("MoveX", lastMoveDir.x);
            anim.SetFloat("MoveY", lastMoveDir.y);
        }
    }
}
