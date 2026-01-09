using UnityEngine;

public class CollectWood : GoapAction
{
    public Transform woodSource;

    private void Awake()
    {
        Effects.Add("HasWood", true);
    }

    public override bool CheckProceduralPrecondition(GoapAgent agent)
    {
        return woodSource != null;
    }

    public override void Perform(GoapAgent agent)
    {
        IsRunning = true;
        agent.MoveTowards(woodSource.position);

        if (Vector2.Distance(agent.transform.position, woodSource.position) < 0.4f)
        {
            agent.beliefs.Set("HasWood", true);
            IsRunning = false;
        }

    }
}
