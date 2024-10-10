using Graphs.Interfaces;

namespace Graphs.Exceptions;

public class NodeOutsideOfGraphException<T, K> : Exception where T : IGraph
{
    public NodeOutsideOfGraphException(T graph)
        : base(Format(graph)) { }

    static string Format(T graph)
    {
        var expectedNodesStr = graph.GetNodes().Select(nodeType => nodeType.Name + "\n");
        return $"Node of type '{typeof(K).Name}' does not exist in the graph. Expected Nodes\n\n{expectedNodesStr}";
    }
}
