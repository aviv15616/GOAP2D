using UnityEngine;

[RequireComponent(typeof(Animator))]
public class WASDMove2D : MonoBehaviour
{
    public float speed = 3f;

    Animator anim;
    Vector2 lastDir = Vector2.down;

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

        // Movement
        transform.position += (Vector3)(dir * speed * Time.deltaTime);

        bool moving = dir != Vector2.zero;
        anim.SetBool("IsMoving", moving);

        if (moving)
        {
            lastDir = dir;
            anim.speed = 1f;
            anim.SetFloat("MoveX", dir.x);
            anim.SetFloat("MoveY", dir.y);
        }
        else
        {
            // Freeze animation on current frame
            anim.speed = 0f;
            anim.SetFloat("MoveX", lastDir.x);
            anim.SetFloat("MoveY", lastDir.y);
        }
    }
}
