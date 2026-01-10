using UnityEngine;
public enum NeedType
{
    Sleep,
    Hunger,
    Warmth
}
public class Needs : MonoBehaviour
{
    [Range(0, 100)] public float energy = 100;
    [Range(0, 100)] public float hunger = 100;
    [Range(0, 100)] public float warmth = 100;

    public float drainPerSecond = 1f;

    [Header("Thresholds")]
    public float urgent = 60f;
    public float critical = 20f;
    public void AddMeter(NeedType need, float amount)
    {
        switch (need)
        {
            case NeedType.Sleep: energy = Mathf.Clamp(energy + amount, 0, 100); break;
            case NeedType.Hunger: hunger = Mathf.Clamp(hunger + amount, 0, 100); break;
            case NeedType.Warmth: warmth = Mathf.Clamp(warmth + amount, 0, 100); break;
        }
    }

    public void Tick(float dt)
    {
        float d = drainPerSecond * dt;
        energy = Mathf.Clamp(energy - d, 0, 100);
        hunger = Mathf.Clamp(hunger - d, 0, 100);
        warmth = Mathf.Clamp(warmth - d, 0, 100);
    }
}
