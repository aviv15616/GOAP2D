using UnityEngine;

public class WorkerAgent : GoapAgent
{
    private void Start()
    {
        beliefs.Set("HasWood", false);
    }
}
