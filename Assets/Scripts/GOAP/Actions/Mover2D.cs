using UnityEngine;

public class Mover2D : MonoBehaviour
{
    public float speed = 2.5f;
    public float arriveDistance = 0.15f;

    public bool MoveTowards(Vector3 target, float dt)
    {
        Vector3 pos = transform.position;
        Vector3 to = target - pos;
        to.z = 0f;

        float dist = to.magnitude;
        if (dist <= arriveDistance) return true;

        float step = speed * dt;
        transform.position = pos + to.normalized * Mathf.Min(step, dist);
        return false;
    }
}
