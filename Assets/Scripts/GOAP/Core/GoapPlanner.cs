// Assets/Scripts/GOAP/Core/GoapPlanner.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GoapPlanner
{
    private class Node
    {
        public Node Parent;
        public GoapAction Action;
        public Dictionary<string, bool> State;
        public float Cost;
    }

    public Queue<GoapAction> Plan(
        GoapAgent agent,
        List<GoapAction> actions,
        AgentBeliefs beliefs,
        AgentGoal goal)
    {
        if (goal == null)
        {
            if (agent.debug) Debug.LogWarning($"[PLAN] {agent.name} goal is NULL");
            return null;
        }

        // אם המטרה כבר מתקיימת עכשיו – אין צורך בתכנית
        if (beliefs.GetState(goal.Key) == goal.DesiredValue)
            return new Queue<GoapAction>();

        // Actions "usable" = procedural ok
        var usable = new List<GoapAction>();
        foreach (var a in actions)
        {
            if (a == null) continue;

            bool ok = a.CheckProceduralPrecondition(agent);
            if (!ok)
            {
                if (agent.debug)
                    Debug.Log($"[PROC_FAIL] {agent.name}:{a.GetType().Name} CheckProceduralPrecondition=false");
                continue;
            }

            usable.Add(a);
        }

        var start = new Node
        {
            Parent = null,
            Action = null,
            State = (Dictionary<string, bool>)beliefs.Snapshot(),
            Cost = 0f
        };

        var open = new List<Node> { start };
        var closed = new HashSet<string>();

        Node bestGoalNode = null;

        while (open.Count > 0)
        {
            // cheapest-first
            open.Sort((a, b) => a.Cost.CompareTo(b.Cost));
            var current = open[0];
            open.RemoveAt(0);

            var hash = HashState(current.State);
            if (!closed.Add(hash))
                continue;

            // goal reached?
            if (current.State.TryGetValue(goal.Key, out var v) && v == goal.DesiredValue)
            {
                bestGoalNode = current;
                break;
            }

            foreach (var action in usable)
            {
                if (action == null) continue;

                if (!PreconditionsMet(action, current.State))
                    continue;

                var nextState = ApplyEffects(action, current.State);
                var nextNode = new Node
                {
                    Parent = current,
                    Action = action,
                    State = nextState,
                    Cost = current.Cost + Mathf.Max(0.001f, action.Cost)
                };

                open.Add(nextNode);
            }
        }

        if (bestGoalNode == null)
        {
            if (agent.debug)
                Debug.Log($"[PLAN_FAIL] {agent.name} no plan reaches goal {goal.Key}={goal.DesiredValue}");
            return null;
        }

        // build queue from node chain
        var stack = new Stack<GoapAction>();
        var n = bestGoalNode;
        while (n != null && n.Action != null)
        {
            stack.Push(n.Action);
            n = n.Parent;
        }

        var q = new Queue<GoapAction>();
        while (stack.Count > 0) q.Enqueue(stack.Pop());
        return q;
    }

    private static bool PreconditionsMet(GoapAction a, Dictionary<string, bool> state)
    {
        foreach (var kv in a.Preconditions)
        {
            if (!state.TryGetValue(kv.Key, out var v) || v != kv.Value)
                return false;
        }
        return true;
    }

    private static Dictionary<string, bool> ApplyEffects(GoapAction a, Dictionary<string, bool> state)
    {
        var next = new Dictionary<string, bool>(state);
        foreach (var kv in a.Effects)
            next[kv.Key] = kv.Value;
        return next;
    }

    private static string HashState(Dictionary<string, bool> state)
    {
        // stable hash: key-sorted
        var keys = state.Keys.ToList();
        keys.Sort();
        return string.Join("|", keys.Select(k => $"{k}:{(state[k] ? 1 : 0)}"));
    }
}
