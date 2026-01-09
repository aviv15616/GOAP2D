using System.Collections.Generic;

public class AgentBeliefs
{
    public Dictionary<string, bool> beliefs = new();

    public bool Has(string key)
    {
        return beliefs.ContainsKey(key) && beliefs[key];
    }

    public void Set(string key, bool value)
    {
        beliefs[key] = value;
    }
}
