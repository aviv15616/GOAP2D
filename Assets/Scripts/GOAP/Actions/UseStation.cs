using UnityEngine;

public class UseStation : GoapAction
{
    [Header("Station")]
    [Tooltip("Must be exactly: Water / Food / Fire")]
    public string stationType;

    public float useRange = 0.6f;
    public float useSeconds = 1.5f;

    private GameObject targetStation;
    private float t;

    private void Awake()
    {
        if (!string.IsNullOrEmpty(stationType))
        {
            Preconditions[$"HasStation_{stationType}"] = true;
            Preconditions[$"Near{stationType}"] = true;
            Effects[$"Used_{stationType}"] = true;
        }
    }

    public override bool CheckProceduralPrecondition(GoapAgent agent)
    {
        if (agent == null) return false;
        if (string.IsNullOrEmpty(stationType)) return false;
        if (StationManager.Instance == null) return false;

        var nearest = StationManager.Instance.GetNearestStation(stationType, agent.transform.position);
        if (nearest == null) return false;

        targetStation = nearest.gameObject;
        return true;
    }

    public override void Perform(GoapAgent agent)
    {
        if (agent == null || targetStation == null || StationManager.Instance == null)
        {
            IsRunning = false;
            return;
        }

        float d = Vector2.Distance(agent.transform.position, targetStation.transform.position);
        if (d > useRange)
        {
            IsRunning = true;
            agent.MoveTowards(targetStation.transform.position);
            return;
        }

        if (!StationManager.Instance.RequestUse(targetStation))
        {
            IsRunning = false; // busy -> replan
            return;
        }

        IsRunning = true;

        t += Time.deltaTime;
        if (t >= useSeconds)
        {
            StationManager.Instance.ReleaseUse(targetStation);
            IsRunning = false;
            t = 0f;
        }
    }

    public override void DoReset()
    {
        base.DoReset();
        if (StationManager.Instance != null && targetStation != null)
            StationManager.Instance.ReleaseUse(targetStation);

        targetStation = null;
        t = 0f;
    }
}
