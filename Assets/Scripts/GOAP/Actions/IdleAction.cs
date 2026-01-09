using UnityEngine;

public class IdleAction : GoapAction
{
    public float idleSeconds = 1.5f;
    private float t;

    private void Awake()
    {
        Effects["Idle"] = true;
    }

    public override bool CheckProceduralPrecondition(GoapAgent agent) => agent != null;

    public override void Perform(GoapAgent agent)
    {
        IsRunning = true;
        t += Time.deltaTime;
        if (t >= idleSeconds)
        {
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
