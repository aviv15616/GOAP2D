// BuildTester2D.cs (REPLACE with this)
using UnityEngine;

public class BuildTester2D : MonoBehaviour
{
    public BuildValidator validator;

    [Header("Prefabs ONLY (recommended)")]
    public GameObject bedPrefab;
    public GameObject potPrefab;
    public GameObject firePrefab;

    [Header("Spawn")]
    public float spawnZ = 0f;

    [Tooltip("Force spawned station Rigidbody2D to Static to avoid pushing NPCs.")]
    public bool forceSpawnedRigidbodyStatic = true;

    [Header("Controls")]
    public KeyCode bedKey = KeyCode.Alpha1;
    public KeyCode potKey = KeyCode.Alpha2;
    public KeyCode fireKey = KeyCode.Alpha3;

    private StationType _selected = StationType.Bed;

    private void Update()
    {
        if (validator == null) return;

        if (Input.GetKeyDown(bedKey)) _selected = StationType.Bed;
        if (Input.GetKeyDown(potKey)) _selected = StationType.Pot;
        if (Input.GetKeyDown(fireKey)) _selected = StationType.Fire;

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 desired = GetMouseWorld2D();
            TryBuild(_selected, desired);
        }
    }

    private Vector2 GetMouseWorld2D()
    {
        var cam = Camera.main;
        Vector3 m = Input.mousePosition;

        // stable 2D plane pick
        float zDist = -cam.transform.position.z;
        Vector3 w = cam.ScreenToWorldPoint(new Vector3(m.x, m.y, zDist));

        return new Vector2(w.x, w.y);
    }

    private void TryBuild(StationType type, Vector2 desired)
    {
        if (!validator.TryFindValidPosition(desired, out Vector2 pos))
        {
            Debug.Log($"Build FAIL ({type}) - too close to colliders or outside tilemap.");
            return;
        }

        GameObject prefab = type switch
        {
            StationType.Bed => bedPrefab,
            StationType.Pot => potPrefab,
            StationType.Fire => firePrefab,
            _ => null
        };

        if (prefab == null)
        {
            Debug.LogError($"No prefab assigned for {type}.");
            return;
        }

        Vector3 spawnPos = new Vector3(pos.x, pos.y, spawnZ);
        var go = Instantiate(prefab, spawnPos, Quaternion.identity);

        if (forceSpawnedRigidbodyStatic)
        {
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null) rb.bodyType = RigidbodyType2D.Static;
        }

        Debug.Log($"Built {type} at {spawnPos}");
    }
}
