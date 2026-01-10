using System.Collections.Generic;

public enum BeliefKey
{
    // world
    WoodExists, BedExists, PotExists, FireExists,

    // needs (derived)
    IsHungry, IsStarving,
    IsTired, IsExhausted,
    IsCold, IsFreezing,
}

public class Beliefs
{
    private readonly Dictionary<BeliefKey, bool> _b = new();

    public bool Get(BeliefKey k) => _b.TryGetValue(k, out var v) && v;
    public void Set(BeliefKey k, bool v) => _b[k] = v;

    public void Clear() => _b.Clear();
}
