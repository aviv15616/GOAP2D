// Assets/Scripts/GOAP/Core/Sensor.cs
using UnityEngine;

public class Sensor : MonoBehaviour
{
    private GoapAgent agent;

    [Header("Stamina")]
    public float lowStaminaThreshold = 20f;

    private void Awake() => agent = GetComponent<GoapAgent>();

    private void Update()
    {
        if (agent == null) agent = GetComponent<GoapAgent>();
        if (agent == null) return;

        // keep beliefs fresh BEFORE planning (set Script Execution Order: Sensor BEFORE GoapAgent)
        agent.beliefs.Set("HasWood", agent.InventoryWood > 0);
        agent.beliefs.Set("LowStamina", agent.Stamina < lowStaminaThreshold);

        if (StationManager.Instance != null)
        {
            agent.beliefs.Set("HasStation_Water", StationManager.Instance.HasStation("Water"));
            agent.beliefs.Set("HasStation_Food", StationManager.Instance.HasStation("Food"));
            agent.beliefs.Set("HasStation_Fire", StationManager.Instance.HasStation("Fire"));
        }
    }
}
