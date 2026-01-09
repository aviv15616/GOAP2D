using GOAP.Actions;
using UnityEngine;

public class Sleep : GoapAction
{
    public float SleepTime = 2f;

    private readonly GoapTimer timer = new GoapTimer();
    private bool started;

    private void Awake()
    {
        Preconditions.Add("isTired", true);
        Effects.Add("rested", true);
        Cost = 4f;
    }

    public override void Perform(GoapAgent agent)
    {
        if (!started)
        {
            timer.Start(SleepTime);
            IsRunning = true;
            started = true;
        }

        if (timer.Done)
        {
            agent.beliefs.Set("isTired", false);
            IsRunning = false;
            started = false;
            timer.Reset();
        }
    }

    public override void DoReset()
    {
        base.DoReset();
        started = false;
        timer.Reset();
    }
}
