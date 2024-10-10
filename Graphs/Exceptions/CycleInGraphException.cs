namespace Graphs.Exceptions;

public class CycleInGraphException(IEnumerable<Type> visitedNodesStack) : Exception(Format(visitedNodesStack))
{
    public static string Format(IEnumerable<Type> visitedNodesStack)
    {
        var stackStr = string.Join("\n↓\n", visitedNodesStack);
        return $"The graph contains a cycle and cannot be topologically sorted: \n\n{stackStr}\n";
    }
}
