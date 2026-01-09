using UnityEngine;

public class Build : GoapAction
{
    public string stationType; // "Water" / "Food" / "Fire"
    public GameObject prefab;

    private Transform buildSpot;

    private void Awake()
    {
        Preconditions.Add("HasWood", true);
        Effects.Add("BuiltStation", true);
    }

    public override bool CheckProceduralPrecondition(GoapAgent agent)
    {
        if (StationManager.Instance.IsAtMax(stationType))
            return false;

        buildSpot = StationManager.Instance.GetFreeBuildSpot(stationType);
        return buildSpot != null;
    }

    public override void Perform(GoapAgent agent)
    {
        IsRunning = true;

        agent.MoveTowards(buildSpot.position);

        if (Vector2.Distance(agent.transform.position, buildSpot.position) < 0.3f)
        {
            GameObject st = GameObject.Instantiate(prefab, buildSpot.position, Quaternion.identity);
            StationManager.Instance.RegisterStation(stationType, st);

            agent.beliefs.Set("HasWood", false);
            IsRunning = false;
        }
    }
}
