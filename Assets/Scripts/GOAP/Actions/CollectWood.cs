using UnityEngine;

public class CollectWood : GoapAction
{
    public Transform woodSource;

    public float collectRange = 0.6f;
    public int woodGain = 1;

    public float collectSeconds = 1.0f;
    private float t;

    private void Awake()
    {
        Effects["HasWood"] = true;
    }

    public override bool CheckProceduralPrecondition(GoapAgent agent)
    {
        if (agent == null) return false;

        if (woodSource == null)
        {
            var go = GameObject.FindWithTag("Wood");
            woodSource = go != null ? go.transform : null;
        }

        return woodSource != null;
    }

    public override void Perform(GoapAgent agent)
    {
        if (agent == null || woodSource == null)
        {
            IsRunning = false;
            return;
        }

        float d = Vector2.Distance(agent.transform.position, woodSource.position);
        if (d > collectRange)
        {
            IsRunning = true;
            agent.MoveTowards(woodSource.position);
            return;
        }

        IsRunning = true;
        t += Time.deltaTime;

        if (t >= collectSeconds)
        {
            agent.InventoryWood += woodGain;
            agent.beliefs.Set("HasWood", agent.InventoryWood > 0);
            IsRunning = false;
            t = 0f;
        }
    }

    public override void DoReset()
    {
        base.DoReset();
        t = 0f;
    }
}
