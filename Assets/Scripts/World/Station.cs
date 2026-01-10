// Station.cs  (ONLY the parts you need to add/change)
using UnityEngine;

public enum StationType { Wood, Bed, Pot, Fire }

public class Station : MonoBehaviour
{
    public StationType type;

    [Tooltip("If true, station can be used. If false, it's considered missing/unbuilt.")]
    public bool built = true;

    [Tooltip("Optional: where the NPC should stand to interact/build. If null, uses this transform.")]
    public Transform interactionPoint;

    [Tooltip("Optional: visuals root to hide/show when built toggles. If null, toggles renderers/colliders under this object.")]
    public GameObject visualsRoot;

    // ✅ NEW: bound build spot (so we can free it if station is destroyed/disabled)
    [HideInInspector] public BuildSpot boundSpot;

    public Vector3 InteractPos => (interactionPoint != null) ? interactionPoint.position : transform.position;
    public bool Exists => gameObject.activeInHierarchy && built;

    private void OnEnable() => StationRegistry.Instance?.Register(this);

    private void OnDisable()
    {
        StationRegistry.Instance?.Unregister(this);

        // ✅ NEW: free the spot if this station goes away
        if (boundSpot != null)
        {
            boundSpot.occupied = false;
            boundSpot = null;
        }
    }

    public void SetBuilt(bool value)
    {
        built = value;

        if (visualsRoot != null)
        {
            visualsRoot.SetActive(value);
            return;
        }

        // your existing fallback toggling renderers/colliders...
    }
}
