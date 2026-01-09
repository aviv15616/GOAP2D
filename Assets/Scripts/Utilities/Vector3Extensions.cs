using UnityEngine;

public static class Vector3Extensions
{
    public static Vector2 XY(this Vector3 v) => new Vector2(v.x, v.y);
}
