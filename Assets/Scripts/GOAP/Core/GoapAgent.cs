using System.Collections.Generic;
using UnityEngine;

public class GoapAgent : MonoBehaviour
{
    public bool debug = true;

    public List<GoapAction> actions = new();
    public Queue<GoapAction> currentPlan = new();

    // חשוב: פעם אחת בלבד!
    public AgentBeliefs beliefs = new();

    public AgentGoal currentGoal;
    public GoapAction currentAction;

    public float moveSpeed = 2f;
    public Rigidbody2D rb;

    public float Stamina = 10f;
    public int InventoryWood = 0;
    private float nextPlanTime = 0f;
    public float replanCooldown = 0.5f;

    private GoapPlanner planner = new GoapPlanner();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        actions = new List<GoapAction>(GetComponents<GoapAction>());

        if (debug)
            Debug.Log($"[AGENT] {name} Awake | actions={actions.Count}");
    }

    private void Start()
    {
        // אל תערבב keys לא קשורים. נשאיר מינימום וניתן ל-Sensor לקבוע.
        beliefs.SetState("HasWood", InventoryWood > 0);
        beliefs.SetState("LowStamina", Stamina < 20);

        if (debug)
            Debug.Log($"[AGENT] {name} Start | beliefsTrue={beliefs.DumpTrueStates()}");
    }

    void Update()
    {
        if (currentAction == null)
        {
            // אל תבנה תכנית כל פריים
            if (Time.time >= nextPlanTime && (currentPlan == null || currentPlan.Count == 0))
            {
                BuildPlan();
                nextPlanTime = Time.time + replanCooldown;
            }

            if (currentPlan != null && currentPlan.Count > 0)
                currentAction = currentPlan.Dequeue();
        }

        if (currentAction != null)
        {
            currentAction.Perform(this);

            if (!currentAction.IsRunning)
                currentAction = null;
        }
    }


    private void BuildPlan()
    {
        currentGoal = null;
        Queue<GoapAction> bestPlan = null;

        var providers = GetComponents<AgentGoalProvider>();
        if (providers == null || providers.Length == 0)
        {
            if (debug) Debug.LogWarning($"[PLAN] {name} no GoalProviders found.");
            return;
        }

        foreach (var provider in providers)
        {
            var goal = provider.GetGoal();
            if (goal == null) continue;

            var plan = planner.Plan(this, actions, beliefs, goal);

            if (debug)
            {
                Debug.Log($"[GOAL_TRY] {name} try goal={goal.Key}={goal.DesiredValue} p={goal.Priority} -> plan={(plan == null ? "NULL" : plan.Count.ToString())}");
            }

            if (plan != null)
            {
                if (currentGoal == null || goal.Priority > currentGoal.Priority)
                {
                    currentGoal = goal;
                    bestPlan = plan;
                }
            }
        }

        if (bestPlan != null)
            currentPlan = bestPlan;

        if (debug)
        {
            string goalStr = currentGoal != null ? $"{currentGoal.Key}={currentGoal.DesiredValue} (p={currentGoal.Priority})" : "NULL";
            int planCount = (currentPlan == null) ? -1 : currentPlan.Count;
            Debug.Log($"[PLAN_PICK] {name} goal={goalStr} planCount={planCount} beliefsTrue={beliefs.DumpTrueStates()}");
        }
    }

    public void MoveTowards(Vector2 target)
    {
        if (rb != null)
        {
            Vector2 newPos = Vector2.MoveTowards(rb.position, target, moveSpeed * Time.deltaTime);
            rb.MovePosition(newPos);
        }
        else
        {
            transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        }
    }
}
