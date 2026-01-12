using UnityEngine;

public class GoapDebugLogger
{
    private readonly string _name;
    private readonly float _minInterval;
    private float _lastTime;
    private string _lastSig = "";

    public GoapDebugLogger(string npcName, float minIntervalSeconds = 0.25f)
    {
        _name = npcName;
        _minInterval = Mathf.Max(0.05f, minIntervalSeconds);
        _lastTime = -999f;
    }

    // Logs only if enough time passed OR signature changed.
    public void Log(string tag, string msg, string signature = null)
    {
        float now = Time.time;
        string sig = signature ?? (tag + "|" + msg);

        bool timeOk = (now - _lastTime) >= _minInterval;
        bool changed = sig != _lastSig;

        if (!timeOk && !changed)
            return;

        _lastTime = now;
        _lastSig = sig;

        Debug.Log($"[GOAP][{_name}][{tag}] {msg}");
    }
}
