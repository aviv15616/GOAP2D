using UnityEngine;

public class Move : GoapAction
{
    public Transform target;
    public float stopDistance = 0.3f;

    public override bool CheckProceduralPrecondition(GoapAgent agent)
    {
        return target != null;
    }

    public override void Perform(GoapAgent agent)
    {
        IsRunning = true;

        agent.MoveTowards(target.position);

        float dist = Vector2.Distance(agent.transform.position, target.position);
        if (dist <= stopDistance)
        {
            IsRunning = false;
        }

    }
}
