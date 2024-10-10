namespace Graphs.Exceptions;

public class CycleInGraphException : Exception
{
    public CycleInGraphException(IEnumerable<Type> visitedNodesStack) : base(Format(visitedNodesStack)) { }

    public static string Format(IEnumerable<Type> visitedNodesStack)
    {
        var stackStr = string.Join("\n↓\n", visitedNodesStack);
        return $"The graph contains a cycle and cannot be topologically sorted: \n\n{stackStr}\n";
    }
}
