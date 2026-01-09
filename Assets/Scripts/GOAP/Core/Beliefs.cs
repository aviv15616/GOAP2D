// Assets/Scripts/GOAP/Core/Beliefs.cs
// FIX: מוסיף תאימות ל-AddState/RemoveState/HasState + מונע new() שעלול לשבור ב-Unity ישן
using System.Collections.Generic;

public class AgentBeliefs
{
    private Dictionary<string, bool> beliefs = new Dictionary<string, bool>();

    // API קיים אצלך
    public bool Has(string key)
    {
        return beliefs.ContainsKey(key) && beliefs[key];
    }

    public void Set(string key, bool value)
    {
        beliefs[key] = value;
    }

    // -----------------------------
    // Compatibility API (כדי לתקן את השגיאות אצלך בקוד)
    // -----------------------------
    public void AddState(string key) => Set(key, true);
    public void RemoveState(string key) => Set(key, false);
    public bool HasState(string key) => Has(key);

    // אופציונלי: alias למי שמשתמש בשם הזה
    public void SetState(string key, bool value) => Set(key, value);
}
