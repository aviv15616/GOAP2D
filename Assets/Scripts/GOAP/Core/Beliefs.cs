using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Simple beliefs container (string -> bool) with backward-compatible helper APIs.
/// </summary>
public class AgentBeliefs
{
    private readonly Dictionary<string, bool> _states = new();

    // Core
    public void Set(string key, bool value)
    {
        if (string.IsNullOrEmpty(key)) return;
        _states[key] = value;
    }

    public bool Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        return _states.TryGetValue(key, out var v) && v;
    }

    public bool HasState(string key) => Get(key);

    public IReadOnlyDictionary<string, bool> Snapshot()
        => new Dictionary<string, bool>(_states);

    // Compatibility aliases (so ALL your scripts compile)
    public void SetState(string key, bool value) => Set(key, value);
    public bool GetState(string key) => Get(key);

    public void AddState(string key) => Set(key, true);
    public void RemoveState(string key) => Set(key, false);

    public string DumpTrueStates()
    {
        var trues = _states.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
        return trues.Count == 0 ? "(none)" : string.Join(", ", trues);
    }
}
