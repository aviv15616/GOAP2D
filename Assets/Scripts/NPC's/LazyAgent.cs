using UnityEngine;

public class LazyAgent : GoapAgent
{
    private void Start()
    {
        beliefs.Set("IsTired", true);
    }
}
