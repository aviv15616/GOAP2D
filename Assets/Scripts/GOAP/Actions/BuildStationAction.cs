using UnityEngine;

public class BuildStationAction : GoapAction
{
    public StationType buildType = StationType.Bed;

    [Tooltip("Prefab to spawn (must have Station.cs on it).")]
    public GameObject stationPrefab;

    public int woodCost = 2;
    public float buildTime = 1.5f;

    private Vector3 _buildPos;
    private float _t;

    public override bool CanPlan(WorldState s)
    {
        bool exists = false;
        if (buildType == StationType.Bed) exists = s.bedExists;
        else if (buildType == StationType.Pot) exists = s.potExists;
        else if (buildType == StationType.Fire) exists = s.fireExists;

        return !exists && s.woodCarried >= woodCost;
    }

    public override void ApplyPlanEffects(ref WorldState s)
    {
        if (buildType == StationType.Bed) s.bedExists = true;
        else if (buildType == StationType.Pot) s.potExists = true;
        else if (buildType == StationType.Fire) s.fireExists = true;

        s.woodCarried -= woodCost;
    }

    public override bool StartAction(GoapAgent agent)
    {
        if (stationPrefab == null) return false;
        if (agent.buildValidator == null) return false;

        Vector2 desired = agent.transform.position;
        Vector2 found;
        if (!agent.buildValidator.TryFindValidPosition(desired, out found))
            return false;

        _buildPos = new Vector3(found.x, found.y, agent.spawnZ);
        _t = 0f;
        return true;
    }

    public override bool Perform(GoapAgent agent, float dt)
    {
        if (!_started)
        {
            _started = true;
            if (!StartAction(agent)) return true;
        }

        if (!agent.mover.MoveTowards(_buildPos, dt)) return false;

        _t += dt;
        if (_t >= buildTime)
        {
            if (agent.wood < woodCost) return true;

            agent.wood -= woodCost;

            var go = Object.Instantiate(stationPrefab, _buildPos, Quaternion.identity);


            var st = go.GetComponent<Station>();
            if (st != null) st.built = true;

            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null) rb.bodyType = RigidbodyType2D.Static;

            return true;
        }
        return false;
    }

    public override void ResetRuntime()
    {
        base.ResetRuntime();
        _t = 0f;
    }
}
