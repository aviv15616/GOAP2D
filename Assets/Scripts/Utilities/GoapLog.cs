// Assets/Scripts/Utilities/GoapLog.cs
using System.Collections.Generic;
using UnityEngine;

public static class GoapLog
{
    public enum Level
    {
        Off = 0,
        Error = 1,
        Warn = 2,
        Info = 3,
        Verbose = 4,
    }

    // Set in inspector via a ScriptableObject if you want; for now keep it simple:
    public static Level Current = Level.Info;

    // Throttle repeated messages (key -> nextAllowedTime)
    private static readonly Dictionary<string, float> _nextTime = new();

    public static void Info(string key, string msg, Object ctx = null, float minInterval = 0.5f) =>
        Log(Level.Info, key, msg, ctx, minInterval);

    public static void Verbose(
        string key,
        string msg,
        Object ctx = null,
        float minInterval = 0.2f
    ) => Log(Level.Verbose, key, msg, ctx, minInterval);

    public static void Warn(string key, string msg, Object ctx = null, float minInterval = 0.5f) =>
        Log(Level.Warn, key, msg, ctx, minInterval);

    public static void Error(string key, string msg, Object ctx = null, float minInterval = 0f) =>
        Log(Level.Error, key, msg, ctx, minInterval);

    private static void Log(Level lvl, string key, string msg, Object ctx, float minInterval)
    {
        if (Current < lvl || Current == Level.Off)
            return;

        float now = Time.time;
        if (minInterval > 0f)
        {
            if (_nextTime.TryGetValue(key, out float next) && now < next)
                return;
            _nextTime[key] = now + minInterval;
        }

        switch (lvl)
        {
            case Level.Error:
                Debug.LogError(msg, ctx);
                break;
            case Level.Warn:
                Debug.LogWarning(msg, ctx);
                break;
            default:
                Debug.Log(msg, ctx);
                break;
        }
    }
}
