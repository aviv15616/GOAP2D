// BuildSpot.cs
using UnityEngine;

[DisallowMultipleComponent]
public class BuildSpot : MonoBehaviour
{
    [Header("Which station is allowed here?")]
    public StationType forType = StationType.Bed;

    [Tooltip("If true, a station is currently occupying this spot (or reserved).")]
    public bool occupied;

    [Tooltip("Optional: a specific anchor for the station interaction point.")]
    public Transform stationAnchor;

    public Vector2 Pos => (Vector2)transform.position;
    public Vector3 AnchorPos => stationAnchor != null ? stationAnchor.position : transform.position;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, 0.25f);
        if (stationAnchor != null)
            Gizmos.DrawWireSphere(stationAnchor.position, 0.15f);
    }
#endif
}
