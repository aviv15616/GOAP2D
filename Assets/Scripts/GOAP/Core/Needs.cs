using UnityEngine;

public enum NeedType
{
    Sleep,
    Hunger,
    Warmth,
    None,
}

public class Needs : MonoBehaviour
{
    [Range(0, 100)]
    public float energy = 100;

    [Range(0, 100)]
    public float hunger = 100;

    [Range(0, 100)]
    public float warmth = 100;

    public float drainPerSecond = 1f;

    [Header("Thresholds")]
    [Tooltip("Above this value: not urgent (urgency=0). Between urgent..critical ramps up to 1.")]
    public float urgent = 60f;

    [Tooltip("At/below this value: max urgent (urgency=1).")]
    public float critical = 20f;

    // -------------------------
    // Meters
    // -------------------------

    public float GetMeter(NeedType need)
    {
        return need switch
        {
            NeedType.Sleep => energy,
            NeedType.Hunger => hunger,
            NeedType.Warmth => warmth,
            _ => 100f,
        };
    }

    public void AddMeter(NeedType need, float amount)
    {
        switch (need)
        {
            case NeedType.Sleep:
                energy = Mathf.Clamp(energy + amount, 0, 100);
                break;
            case NeedType.Hunger:
                hunger = Mathf.Clamp(hunger + amount, 0, 100);
                break;
            case NeedType.Warmth:
                warmth = Mathf.Clamp(warmth + amount, 0, 100);
                break;
        }
    }

    public void Tick(float dt)
    {
        float d = drainPerSecond * dt;
        energy = Mathf.Clamp(energy - d, 0, 100);
        hunger = Mathf.Clamp(hunger - d, 0, 100);
        warmth = Mathf.Clamp(warmth - d, 0, 100);
    }

    // -------------------------
    // Urgency (0..1)
    // -------------------------

    /// <summary>
    /// Returns urgency in [0..1].
    /// 0 when meter >= urgent.
    /// 1 when meter <= critical.
    /// Linear ramp between urgent..critical.
    /// </summary>
    public float GetUrgency(NeedType need)
    {
        if (need == NeedType.None)
            return 0f;

        float m = GetMeter(need);

        // safety (avoid div-by-zero / inverted thresholds)
        float u = urgent;
        float c = critical;
        if (u <= c)
            u = c + 0.0001f;

        if (m >= u)
            return 0f;
        if (m <= c)
            return 1f;

        // m is between (critical..urgent)
        float t = (u - m) / (u - c); // 0 at urgent, 1 at critical
        return Mathf.Clamp01(t);
    }

    /// <summary>
    /// Convenience: returns the most urgent need (highest urgency),
    /// but still returns None if all are 0.
    /// </summary>
    public NeedType GetMostUrgentNeed()
    {
        float ue = GetUrgency(NeedType.Sleep);
        float uh = GetUrgency(NeedType.Hunger);
        float uw = GetUrgency(NeedType.Warmth);

        float best = 0f;
        NeedType bestNeed = NeedType.None;

        if (ue > best)
        {
            best = ue;
            bestNeed = NeedType.Sleep;
        }
        if (uh > best)
        {
            best = uh;
            bestNeed = NeedType.Hunger;
        }
        if (uw > best)
        {
            best = uw;
            bestNeed = NeedType.Warmth;
        }

        return bestNeed;
    }
}
