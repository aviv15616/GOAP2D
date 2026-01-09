using UnityEngine;

public class Sensor : MonoBehaviour
{
    private GoapAgent agent;

    [Header("Ranges")]
    public float interactionRange = 1.2f;
    public float stationDetectionRange = 4f;

    private void Awake()
    {
        agent = GetComponent<GoapAgent>();
        if (agent == null)
            Debug.LogError("[Sensor] No GOAP Agent found on this NPC!");
    }

    private void Update()
    {
        UpdateStaminaBeliefs();
        UpdateWoodBeliefs();
        UpdateNearbyStations();
    }

    // -----------------------------
    //  BELIEF: LOW STAMINA
    // -----------------------------
    private void UpdateStaminaBeliefs()
    {
        // אם יש לך משתנה סטמינה בהמשך — מחליפים כאן
        if (agent.Stamina < 20)
            agent.beliefs.AddState("LowStamina");
        else
            agent.beliefs.RemoveState("LowStamina");
    }

    // -----------------------------
    //  BELIEF: HAS WOOD
    // -----------------------------
    private void UpdateWoodBeliefs()
    {
        if (agent.InventoryWood > 0)
            agent.beliefs.AddState("HasWood");
        else
            agent.beliefs.RemoveState("HasWood");
    }

    // -----------------------------
    //  BELIEFS: NEAR STATION X
    // -----------------------------
    private void UpdateNearbyStations()
    {
        // Fire
        if (StationManager.Instance.HasStation("Fire"))
        {
            Transform fire = StationManager.Instance.GetNearestStation("Fire", transform.position);
            if (fire != null && Vector2.Distance(transform.position, fire.position) < stationDetectionRange)
                agent.beliefs.AddState("NearFire");
            else
                agent.beliefs.RemoveState("NearFire");
        }

        // Food
        if (StationManager.Instance.HasStation("Food"))
        {
            Transform food = StationManager.Instance.GetNearestStation("Food", transform.position);
            if (food != null && Vector2.Distance(transform.position, food.position) < stationDetectionRange)
                agent.beliefs.AddState("NearFood");
            else
                agent.beliefs.RemoveState("NearFood");
        }

        // Water
        if (StationManager.Instance.HasStation("Water"))
        {
            Transform water = StationManager.Instance.GetNearestStation("Water", transform.position);
            if (water != null && Vector2.Distance(transform.position, water.position) < stationDetectionRange)
                agent.beliefs.AddState("NearWater");
            else
                agent.beliefs.RemoveState("NearWater");
        }
    }
}
