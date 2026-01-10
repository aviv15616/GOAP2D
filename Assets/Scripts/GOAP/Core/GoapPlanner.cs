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

    // Safety caps
    private const int MAX_EXPANDED = 3000;
    private const int MAX_OPEN = 8000;

    public Stack<GoapAction> Plan(WorldState start, List<GoapAction> actions, IGoal goal, GoapAgent agent)
    {
        if (actions == null || actions.Count == 0) return null;

        int woodCap = agent != null ? Mathf.Max(0, agent.maxWoodForPlanning) : 12;

        var open = new List<Node>(256);
        var bestG = new Dictionary<WorldState, float>(512);

        var startNode = new Node { state = ClampState(start, woodCap), parent = null, action = null, g = 0f };
        open.Add(startNode);
        bestG[startNode.state] = 0f;

        int expanded = 0;

        while (open.Count > 0)
        {
            expanded++;
            if (expanded > MAX_EXPANDED) return null;
            if (open.Count > MAX_OPEN) return null;

            // Pick lowest g WITHOUT sorting the whole list
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

            foreach (var a in actions)
            {
                if (a == null) continue;
                if (!a.CanPlan(current.state)) continue;

                var next = current.state;
                a.ApplyPlanEffects(ref next);
                next = ClampState(next, woodCap);

                float stepCost = a.EstimateCost(agent, current.state);
                float newG = current.g + stepCost;

                // Dedupe: only keep better paths to the same state
                if (bestG.TryGetValue(next, out float oldG) && newG >= oldG)
                    continue;

                bestG[next] = newG;

                open.Add(new Node
                {
                    state = next,
                    parent = current,
                    action = a,
                    g = newG
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
