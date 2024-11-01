using System.Reflection;
using System.Xml.Linq;
using Graphs.Exceptions;
using Graphs.Extensions;
using Graphs.Interfaces;
using Graphs.Models;

namespace Graphs.Services;

public class GraphService
{
    /// <inheritdoc cref="DetectCycleFromNode(Type, HashSet{Type}?)"/>
    public bool DetectCycleInDirectedGraph(IEnumerable<Type> nodeTypes)
    {
        foreach (var node in nodeTypes)
        {
            DetectCycleFromNode(node);
        }
        return false;
    }

    /// <inheritdoc cref="DetectCycleFromNode(Type, HashSet{Type}?, Stack{Type}?)"/>
    public bool DetectCycleFromNode<T>()
    {
        return DetectCycleFromNode(typeof(T));
    }

    /// <summary>
    /// Depth first search that will throw an error if it finds the same nodeType twice while traversing
    /// </summary>
    /// <exception cref="CycleInGraphException"></exception>
    bool DetectCycleFromNode(Type nodeType, GraphCycleTracker<Type>? cycleTracker = null)
    {
        cycleTracker ??= new();
        cycleTracker.Visit(nodeType);
        var (_, dependencies) = nodeType.GetTypesFromFirstConstructor();
        if (dependencies is null || dependencies.Count() == 0)
        {
            cycleTracker.Unvisit(nodeType);
            return false;
        }
        foreach (var dependentNode in dependencies)
        {
            return DetectCycleFromNode(dependentNode, cycleTracker);
        }
        cycleTracker.Unvisit(nodeType);
        return false;
    }

    /// <inheritdoc cref="FlattenGraphNodes{T}(T, HashSet{T}, GraphCycleTracker{Type})"/>
    internal IEnumerable<T> FlattenNodes<T>(T node) where T : GraphNode
    {
        return FlattenGraphNodes(node, new(), new());
    }

    /// <exception cref="CycleInGraphException"></exception>
    HashSet<T> FlattenGraphNodes<T>(T node, HashSet<T> flatNodes, GraphCycleTracker<Type> cycleTracker) where T : GraphNode
    {

        cycleTracker.Visit(node.Type);
        flatNodes.Add(node);
        if (node.Dependencies is null)
        {
            cycleTracker.Unvisit(node.Type);
            return flatNodes;
        }
        foreach (var dependentNode in node.Dependencies)
        {
            FlattenGraphNodes((T)dependentNode, flatNodes, cycleTracker);
        }
        cycleTracker.Unvisit(node.Type);
        return flatNodes;
    }

    internal INode GetGraphNodes<TNode, TExpectedType>(GraphCycleTracker<Type>? cycleTracker = null, Dictionary<INode>? createdNodes = null)
    {
        return GetGraphNodes(typeof(TNode), typeof(TExpectedType), cycleTracker, createdNodes);
    }
}
