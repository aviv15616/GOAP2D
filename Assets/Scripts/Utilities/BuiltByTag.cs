using UnityEngine;

public class BuiltByTag : MonoBehaviour
{
    public static int GlobalSequence = 0;

    public string builderName;
    public StationType stationType;
    public int sequence;
    public float builtAtTime;

    public void Mark(string builder, StationType type)
    {
        builderName = builder;
        stationType = type;
        sequence = ++GlobalSequence;
        builtAtTime = Time.time;
    }
}
