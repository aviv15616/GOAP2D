public class AgentGoal
{
    public string Key;
    public bool DesiredValue;
    public int Priority;

    public AgentGoal(string key, bool value, int priority)
    {
        Key = key;
        DesiredValue = value;
        Priority = priority;
    }
}

public abstract class AgentGoalProvider : UnityEngine.MonoBehaviour
{
    public abstract AgentGoal GetGoal();
}
