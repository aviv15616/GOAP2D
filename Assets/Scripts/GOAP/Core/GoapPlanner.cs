using System.Collections.Generic;
using UnityEngine;

public class GoapPlanner
{
    private class Node
    {
        public WorldState state;
        public Node parent;
        public GoapAction action;
        public float g;
        public Vector2 agentPos; // simulated agent position for planning
    }

    private const int MAX_EXPANDED = 3000;
    private const int MAX_OPEN = 8000;

    public Stack<GoapAction> Plan(
        WorldState start,
        List<GoapAction> actions,
        IGoal goal,
        GoapAgent agent)
    {
        if (actions == null || actions.Count == 0 || agent == null)
            return null;

        int woodCap = Mathf.Max(0, agent.maxWoodForPlanning);

        var open = new List<Node>(256);
        var bestG = new Dictionary<WorldState, float>(512);

        var startNode = new Node
        {
            state = ClampState(start, woodCap),
            parent = null,
            action = null,
            g = 0f,
            agentPos = agent.transform.position
        };

        open.Add(startNode);
        bestG[startNode.state] = 0f;

        int expanded = 0;

        while (open.Count > 0)
        {
            expanded++;
            if (expanded > MAX_EXPANDED) return null;
            if (open.Count > MAX_OPEN) return null;

            // Pick node with lowest g
            int bestIdx = 0;
            float bestCost = open[0].g;
            for (int i = 1; i < open.Count; i++)
            {
                if (open[i].g < bestCost)
                {
                    bestCost = open[i].g;
                    bestIdx = i;
                }
            }

            var current = open[bestIdx];
            open.RemoveAt(bestIdx);

            if (goal.IsSatisfied(current.state))
                return BuildStack(current);

            foreach (var action in actions)
            {
                if (action == null) continue;
                if (!action.CanPlan(current.state)) continue;

                // simulate worldstate
                var nextState = current.state;
                action.ApplyPlanEffects(ref nextState);
                nextState = ClampState(nextState, woodCap);

                // ---------- COST CALCULATION ----------
                float cost = 0f;

                // 1) action duration (1s by design)
                cost += action.duration;

                // 2) travel time (if action has a target)
                float travelTime = action.EstimateCost(agent, current.state);
                cost += travelTime;

                float newG = current.g + cost;
                // -------------------------------------

                if (bestG.TryGetValue(nextState, out float oldG) && newG >= oldG)
                    continue;

                bestG[nextState] = newG;

                open.Add(new Node
                {
                    state = nextState,
                    parent = current,
                    action = action,
                    g = newG,
                    agentPos = current.agentPos // updated later per action
                });
            }
        }

        return null;
    }

    private static WorldState ClampState(WorldState s, int woodCap)
    {
        s.woodCarried = Mathf.Clamp(s.woodCarried, 0, woodCap);
        return s;
    }

    private Stack<GoapAction> BuildStack(Node goalNode)
    {
        var stack = new Stack<GoapAction>();
        var n = goalNode;
        while (n != null && n.action != null)
        {
            stack.Push(n.action);
            n = n.parent;
        }
        return stack;
    }
}
