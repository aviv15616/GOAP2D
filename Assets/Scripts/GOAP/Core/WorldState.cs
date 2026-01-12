using UnityEngine;

public struct WorldState
{
    public bool woodExists;
    public bool bedExists,
        potExists,
        fireExists;
    public int woodCarried;

    public bool sleepSatisfied;
    public bool hungerSatisfied;
    public bool warmthSatisfied;
    public Vector2 pos;

    public override int GetHashCode()
    {
        int h = 17;
        h = h * 31 + (woodExists ? 1 : 0);
        h = h * 31 + (bedExists ? 1 : 0);
        h = h * 31 + (potExists ? 1 : 0);
        h = h * 31 + (fireExists ? 1 : 0);
        h = h * 31 + woodCarried;
        h = h * 31 + (sleepSatisfied ? 1 : 0);
        h = h * 31 + (hungerSatisfied ? 1 : 0);
        h = h * 31 + (warmthSatisfied ? 1 : 0);
        return h;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is WorldState))
            return false;
        WorldState o = (WorldState)obj;

        return woodExists == o.woodExists
            && bedExists == o.bedExists
            && potExists == o.potExists
            && fireExists == o.fireExists
            && woodCarried == o.woodCarried
            && sleepSatisfied == o.sleepSatisfied
            && hungerSatisfied == o.hungerSatisfied
            && warmthSatisfied == o.warmthSatisfied;
    }
}
