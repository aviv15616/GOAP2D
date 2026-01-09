using UnityEngine;

public class SurvivorAgent : GoapAgent
{
    private void Start()
    {
        beliefs.Set("IsHungry", false);
        beliefs.Set("IsThirsty", false);
    }
}
