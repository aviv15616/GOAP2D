using System.Collections.Generic;
using UnityEngine;

public class GoapAgent : MonoBehaviour
{
    public List<GoapAction> actions = new();
    public Queue<GoapAction> currentPlan = new();
    public AgentBeliefs beliefs = new();

    public AgentGoal currentGoal;
    public GoapAction currentAction;

    public float moveSpeed = 2f;
    public Rigidbody2D rb;

    private GoapPlanner planner = new GoapPlanner();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        foreach (var action in GetComponents<GoapAction>())
            actions.Add(action);
    }

    private void Update()
    {
        if (currentAction != null && currentAction.IsRunning)
            return;

        if (currentPlan.Count == 0)
            BuildPlan();

        if (currentPlan.Count > 0)
        {
            currentAction = currentPlan.Dequeue();
            currentAction.Perform(this);
        }
    }

    private void BuildPlan()
    {
        currentGoal = null;
        Queue<GoapAction> bestPlan = null;

        var providers = GetComponents<AgentGoalProvider>();

        foreach (var provider in providers)
        {
            var goal = provider.GetGoal();
            var plan = planner.Plan(this, actions, beliefs, goal);

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
