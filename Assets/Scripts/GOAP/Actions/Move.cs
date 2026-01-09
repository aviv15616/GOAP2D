using UnityEngine;

public class Move : GoapAction
{
    public Transform target;

    public float wanderRadius = 2.5f;
    public float wanderRefreshSeconds = 2.0f;

    public float stopDistance = 0.3f;

    private Vector2 wanderTarget;
    private float nextWanderPick;

    public override bool CheckProceduralPrecondition(GoapAgent agent)
    {
        return agent != null; // לא נכשל רק כי Target ריק
    }

    public override void Perform(GoapAgent agent)
    {
        if (agent == null)
        {
            IsRunning = false;
            return;
        }

        Vector2 dest;

        if (target != null)
        {
            dest = target.position;
        }
        else
        {
            if (Time.time >= nextWanderPick)
            {
                wanderTarget = (Vector2)agent.transform.position + Random.insideUnitCircle * wanderRadius;
                nextWanderPick = Time.time + wanderRefreshSeconds;
            }
            dest = wanderTarget;
        }

        float d = Vector2.Distance(agent.transform.position, dest);
        if (d <= stopDistance)
        {
            IsRunning = false;
            return;
        }

        IsRunning = true;
        agent.MoveTowards(dest);
    }

    public override void DoReset()
    {
        base.DoReset();
        nextWanderPick = 0f;
    }
}
