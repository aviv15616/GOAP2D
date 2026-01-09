using System.Collections.Generic;
using UnityEngine;

public abstract class GoapAction : MonoBehaviour
{
    public string ActionName;
    public float Cost = 1f;

    // Preconditions the world must satisfy before action can run
    public Dictionary<string, bool> Preconditions = new Dictionary<string, bool>();

    // Effects applied to the world after action succeeds
    public Dictionary<string, bool> Effects = new Dictionary<string, bool>();

    public bool IsRunning { get; protected set; }

    public virtual bool CheckProceduralPrecondition(GoapAgent agent) { return true; }

    public virtual void DoReset()
    {
        IsRunning = false;
    }

    public abstract void Perform(GoapAgent agent);
}
