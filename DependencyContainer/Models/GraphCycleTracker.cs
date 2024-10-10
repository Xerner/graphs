using Graphs.Exceptions;

namespace Graphs.Models;

public class GraphCycleTracker<T>
{
    public HashSet<T> Visited = new();
    public Stack<T> Stack = new();

    public void Visit(T node)
    {
        if (Visited.Contains(node))
        {
            throw new CycleInGraphException(Stack.Select(item => item.GetType()));
        }
        Visited.Add(node);
        Stack.Push(node);
    }

    public void Unvisit(T node)
    {
        Visited.Remove(node);
        Stack.Pop();
    }

    public bool Contains(T node)
    {
        return Visited.Contains(node);
    }
}
