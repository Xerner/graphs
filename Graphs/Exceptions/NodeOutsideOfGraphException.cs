using Graphs.Interfaces;

namespace Graphs.Exceptions;

public class NodeOutsideOfGraphException<TGraph, TNode> : Exception where TGraph : IGraph
{
    public NodeOutsideOfGraphException(TGraph graph)
        : base(Format(graph)) { }

    static string Format(TGraph graph)
    {
        var expectedNodesStr = graph.GetNodes().Select(nodeType => nodeType.Name + "\n");
        return $"Node of type '{typeof(TNode).Name}' does not exist in the graph. Expected Nodes\n\n{expectedNodesStr}";
    }
}
