using System.Collections.Generic;
using UnityEngine;

public class BuildStationAction : GoapAction
{
    public StationType buildType = StationType.Bed;

    [Tooltip("Prefab to spawn (must have Station.cs on it).")]
    public GameObject stationPrefab;

    public int woodCost = 2;
    public float buildTime = 1.5f;

    private Vector2 _buildWorld;
    private float _t;

    private List<Vector2> _path;
    private int _pathIndex;

    public override bool CanPlan(WorldState s)
    {
        bool exists = buildType switch
        {
            StationType.Bed => s.bedExists,
            StationType.Pot => s.potExists,
            StationType.Fire => s.fireExists,
            _ => true
        };

        return !exists && s.woodCarried >= woodCost;
    }

    public override void ApplyPlanEffects(ref WorldState s)
    {
        if (buildType == StationType.Bed) s.bedExists = true;
        if (buildType == StationType.Pot) s.potExists = true;
        if (buildType == StationType.Fire) s.fireExists = true;

        s.woodCarried -= woodCost;
    }

    public override float EstimateCost(GoapAgent agent, WorldState currentState)
    {
        return buildTime + 0.5f; // keep cheap
    }

    public override bool StartAction(GoapAgent agent)
    {
        if (stationPrefab == null) return false;
        if (agent.buildValidator == null) return false;

        Vector2 desired = agent.transform.position;
        if (!agent.buildValidator.TryFindValidPosition(desired, out Vector2 found))
            return false;

        _buildWorld = found;
        _t = 0f;

        _pathIndex = 0;
        _path = agent.nav != null ? agent.nav.FindPath(agent.transform.position, _buildWorld) : null;

        return true;
    }

    public override bool Perform(GoapAgent agent, float dt)
    {
        if (!_started)
        {
            _started = true;
            if (!StartAction(agent)) return true;
        }

        if (_path != null && _path.Count > 0)
        {
            if (!agent.mover.FollowPath(_path, ref _pathIndex, dt)) return false;
        }
        else
        {
            if (!agent.mover.MoveTowards(_buildWorld, dt)) return false;
        }

        _t += dt;
        if (_t >= buildTime)
        {
            if (agent.wood < woodCost) return true;

            agent.wood -= woodCost;

            Vector3 spawn = new Vector3(_buildWorld.x, _buildWorld.y, agent.spawnZ);
            var go = Object.Instantiate(stationPrefab, spawn, Quaternion.identity);
            // after Instantiate(...)
            var tag = go.GetComponent<BuiltByTag>();
            if (tag == null) tag = go.AddComponent<BuiltByTag>();

            tag.builderName = agent.name;
            tag.stationType = buildType;
            tag.sequence = ++BuiltByTag.GlobalSequence;


            var st = go.GetComponent<Station>();
            if (st != null)
            {
                st.type = buildType;
                st.built = true;
            }

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
        _path = null;
        _pathIndex = 0;
    }
}
