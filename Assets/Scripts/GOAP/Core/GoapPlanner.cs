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
    WorldState start,                 // Start node: initial world-state snapshot
    List<GoapAction> actions,          // Graph edges: possible actions (state transitions)
    IGoal goal,                        // Goal predicate: defines target nodes
    GoapAgent agent                    // Agent context (navigation, limits, costs)
)
    {
        // Safety: cannot plan without actions or agent context
        if (actions == null || actions.Count == 0 || agent == null)
            return null;

        // Clamp planning state-space (prevents infinite states like wood = 0..∞)
        int woodCap = Mathf.Max(0, agent.maxWoodForPlanning);

        // OPEN SET (Dijkstra frontier):
        // Nodes discovered but not yet expanded
        var open = new List<Node>(256);

        // DISTANCE MAP (Dijkstra dist[]):
        // Best known cost g(s) to reach each WorldState
        var bestG = new Dictionary<WorldState, float>(512);

        // Start node (Dijkstra source)
        var startNode = new Node
        {
            state = ClampState(start, woodCap), // Initial state (clamped)
            parent = null,                      // No parent (root)
            action = null,                      // No action led here
            g = 0f,                             // dist[start] = 0
        };

        // Initialize Dijkstra
        open.Add(startNode);
        bestG[startNode.state] = 0f;

        int expanded = 0; // Safety counter (prevents runaway search)

        // -------------------------
        // MAIN DIJKSTRA LOOP
        // -------------------------
        while (open.Count > 0)
        {
            expanded++;

            // Hard limits to avoid freezing Unity
            if (expanded > MAX_EXPANDED)
                return null;
            if (open.Count > MAX_OPEN)
                return null;

            // ---------------------------------------
            // Extract-Min (Dijkstra priority queue)
            // ---------------------------------------
            // Pick node with the lowest accumulated cost g
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

            // Current node = cheapest unexplored state
            var current = open[bestIdx];
            open.RemoveAt(bestIdx);

            // Goal test (Dijkstra early-exit optimization)
            if (goal.IsSatisfied(current.state))
                return BuildStack(current); // Reconstruct plan via parents

            // ---------------------------------------
            // Edge relaxation (GOAP actions)
            // ---------------------------------------
            foreach (var action in actions)
            {
                // Skip invalid actions
                if (action == null)
                    continue;

                // Preconditions check (edge existence)
                if (!action.CanPlan(current.state))
                    continue;

                // Create successor state (graph vertex)
                var nextState = current.state;

                // Apply action effects in PLANNING space:
                // - Choose best target based on simulated position
                // - Update simulated position (nextState.pos)
                // - Update world flags and inventory (wood, stations, etc.)
                action.ApplyPlanEffects(agent, ref nextState);

                // Clamp state-space again (safety)
                nextState = ClampState(nextState, woodCap);

                // Edge weight (cost of action from current state)
                float stepCost = action.EstimateCost(agent, current.state);

                // g' = g(current) + cost(edge)
                float newG = current.g + stepCost;

                // -------------------------
                // RELAXATION (Dijkstra)
                // -------------------------
                // If we already reached this state cheaper → skip
                if (bestG.TryGetValue(nextState, out float oldG) && newG >= oldG)
                    continue;

                // Update best known distance to nextState
                bestG[nextState] = newG;

                // Add relaxed node to OPEN set
                open.Add(
                    new Node
                    {
                        state = nextState,   // Vertex
                        parent = current,    // Parent for path reconstruction
                        action = action,     // Edge used to reach this node
                        g = newG,             // dist[nextState]
                    }
                );
            }
        }

        // No plan found within constraints
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
