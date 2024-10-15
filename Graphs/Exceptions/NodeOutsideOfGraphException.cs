﻿using Graphs.Interfaces;

namespace Graphs.Exceptions;

public class NodeOutsideOfGraphException<TGraph, TNode>(TGraph graph) : Exception(Format(graph)) where TGraph : IGraph<TNode>
{
    static string Format(TGraph graph)
    {
        var notFoundNodeType = typeof(TNode);
        var expectedNodesStr = graph.GetAll().Select(nodeType => nodeType is null ? "null" : nodeType.GetType().Name + "\n");
        return $"Node of type '{notFoundNodeType.Name}' does not exist in the graph. Expected Nodes\n\n{expectedNodesStr}";
    }
}
