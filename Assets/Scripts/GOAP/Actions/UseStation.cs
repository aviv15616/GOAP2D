// Assets/Scripts/GOAP/Actions/UseStation.cs
// FIX: RequestUse if בלי גוף + מבטל switch-expression (כדי להיות תואם Unity/C# ישנים) + null safety
using UnityEngine;

public class UseStation : GoapAction
{
    public string stationType;

    private GameObject targetStation;
    private float useTime = 2f;
    private float timer;

    private void Awake()
    {
        Effects.Add("UsedStation", true);
    }

    public override bool CheckProceduralPrecondition(GoapAgent agent)
    {
        if (!StationManager.Instance.HasStation(stationType))
            return false;

        targetStation = FindClosestStation();
        return targetStation != null;
    }

    private GameObject FindClosestStation()
    {
        var stations = StationManager.Instance.GetActiveStations(stationType);
        if (stations == null || stations.Count == 0)
            return null;

        float best = float.MaxValue;
        GameObject bestStation = null;

        foreach (var s in stations)
        {
            if (s == null) continue;

            float d = Vector2.Distance(s.transform.position, transform.position);
            if (d < best)
            {
                best = d;
                bestStation = s;
            }
        }

        return bestStation;
    }

    public override void Perform(GoapAgent agent)
    {
        if (targetStation == null)
        {
            IsRunning = false;
            return;
        }

        // אם תפוס/לא ניתן להשתמש — תצא (לא מתחילים פעולה)
        if (!StationManager.Instance.RequestUse(targetStation))
        {
            IsRunning = false;
            return;
        }

        IsRunning = true;

        // Move to station
        agent.MoveTowards(targetStation.transform.position);

        if (Vector2.Distance(agent.transform.position, targetStation.transform.position) < 0.3f)
        {
            timer += Time.deltaTime;
            if (timer >= useTime)
            {
                StationManager.Instance.ReleaseUse(targetStation);
                IsRunning = false;
                timer = 0f;
            }
        }
    }

    public override void DoReset()
    {
        base.DoReset();
        timer = 0f;
        targetStation = null;
    }
}
