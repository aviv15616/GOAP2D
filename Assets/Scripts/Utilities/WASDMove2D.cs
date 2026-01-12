using UnityEngine;

public class WASDMove2D : MonoBehaviour
{
    public float speed = 3f;

    void Update()
    {
        float x = 0f;
        float y = 0f;

        if (Input.GetKey(KeyCode.A))
            x -= 1f;
        if (Input.GetKey(KeyCode.D))
            x += 1f;
        if (Input.GetKey(KeyCode.S))
            y -= 1f;
        if (Input.GetKey(KeyCode.W))
            y += 1f;

        Vector2 dir = new Vector2(x, y);
        if (dir.sqrMagnitude > 1f)
            dir.Normalize();

        transform.position += (Vector3)(dir * speed * Time.deltaTime);
    }
}
