using System.Collections.Generic;

public class GoapPlanner
{
    private class Node
    {
        public WorldState state;
        public Node parent;
        public GoapAction action;
        public float g;
    }

    public Stack<GoapAction> Plan(WorldState start, List<GoapAction> actions, IGoal goal)
    {
        var open = new List<Node> { new Node { state = start, g = 0 } };
        var closed = new HashSet<WorldState>();

        while (open.Count > 0)
        {
            open.Sort((a, b) => a.g.CompareTo(b.g));
            var cur = open[0];
            open.RemoveAt(0);

            if (goal.IsSatisfied(cur.state))
                return BuildStack(cur);

            closed.Add(cur.state);

            foreach (var a in actions)
            {
                if (!a.CanPlan(cur.state)) continue;

                var next = cur.state; // struct copy
                a.ApplyPlanEffects(ref next);

                if (closed.Contains(next)) continue;

                open.Add(new Node
                {
                    state = next,
                    parent = cur,
                    action = a,
                    g = cur.g + a.planCost
                });
            }
        }

        return null; // no plan
    }

    private Stack<GoapAction> BuildStack(Node node)
    {
        var stack = new Stack<GoapAction>();
        var n = node;
        while (n != null && n.action != null)
        {
            stack.Push(n.action);
            n = n.parent;
        }
        return stack;
    }
}
