using UnityEngine;

public class Build : GoapAction
{
    [Header("Build")]
    [Tooltip("Must be exactly: Water / Food / Fire")]
    public string stationType;

    public Transform buildMarker;
    public float buildOffset = 1.0f;
    public int woodCost = 1;

    private void Awake()
    {
        Preconditions["HasWood"] = true;

        if (!string.IsNullOrEmpty(stationType))
            Effects[$"HasStation_{stationType}"] = true;
    }

    public override bool CheckProceduralPrecondition(GoapAgent agent)
    {
        if (agent == null) return false;
        if (StationManager.Instance == null) return false;
        if (string.IsNullOrEmpty(stationType)) return false;
        if (agent.InventoryWood < woodCost) return false;
        if (StationManager.Instance.IsAtMax(stationType)) return false;

        Vector3 desired = (buildMarker != null)
            ? buildMarker.position
            : agent.transform.position + (Vector3)(Random.insideUnitCircle.normalized * buildOffset);

        desired = StationManager.Instance.Snap(desired);
        return StationManager.Instance.CanPlaceStation(stationType, desired);
    }

    public override void Perform(GoapAgent agent)
    {
        if (agent == null || StationManager.Instance == null)
        {
            IsRunning = false;
            return;
        }

        Vector3 desired = (buildMarker != null)
            ? buildMarker.position
            : agent.transform.position + (Vector3)(Random.insideUnitCircle.normalized * buildOffset);

        if (!StationManager.Instance.TryBuildStation(stationType, desired, out _))
        {
            IsRunning = false;
            return;
        }

        agent.InventoryWood -= woodCost;

        agent.beliefs.Set("HasWood", agent.InventoryWood > 0);
        agent.beliefs.Set($"HasStation_{stationType}", true);

        IsRunning = false;
    }
}
