// GoapPlanner.cs (PATCHED: uses agent-aware ApplyPlanEffects so pos is simulated correctly)
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
    }

    private const int MAX_EXPANDED = 3000;
    private const int MAX_OPEN = 8000;

    public Stack<GoapAction> Plan(
        WorldState start,
        List<GoapAction> actions,
        IGoal goal,
        GoapAgent agent
    )
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
        };

        open.Add(startNode);
        bestG[startNode.state] = 0f;

        int expanded = 0;

        while (open.Count > 0)
        {
            expanded++;
            if (expanded > MAX_EXPANDED)
                return null;
            if (open.Count > MAX_OPEN)
                return null;

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
                if (action == null)
                    continue;
                if (!action.CanPlan(current.state))
                    continue;

                var nextState = current.state;

                // ✅ Apply effects using agent, so action can:
                // - choose best target by travel time from nextState.pos
                // - update nextState.pos to that target
                // - update flags/wood
                action.ApplyPlanEffects(agent, ref nextState);

                nextState = ClampState(nextState, woodCap);

                // ✅ Cost comes from action itself (travel + duration + planCost)
                float stepCost = action.EstimateCost(agent, current.state);
                float newG = current.g + stepCost;

                if (bestG.TryGetValue(nextState, out float oldG) && newG >= oldG)
                    continue;

                bestG[nextState] = newG;

                open.Add(
                    new Node
                    {
                        state = nextState,
                        parent = current,
                        action = action,
                        g = newG,
                    }
                );
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
