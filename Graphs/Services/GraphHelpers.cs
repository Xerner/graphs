using System.Reflection;
using System.Xml.Linq;
using Graphs.Exceptions;
using Graphs.Interfaces;
using Graphs.Models;

namespace Graphs.Services;

public class GraphHelpers
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
        var (_, dependencies) = GetTypesFromFirstConstructor(nodeType);
        if (dependencies is null || dependencies.Count == 0)
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

        cycleTracker.Visit(node.NodeType);
        flatNodes.Add(node);
        if (node.Dependencies is null)
        {
            cycleTracker.Unvisit(node.NodeType);
            return flatNodes;
        }
        foreach (var dependentNode in node.Dependencies)
        {
            FlattenGraphNodes((T)dependentNode, flatNodes, cycleTracker);
        }
        cycleTracker.Unvisit(node.NodeType);
        return flatNodes;
    }

    internal GraphNode GetGraphNodes<TNode, TExpectedType>(GraphCycleTracker<Type>? cycleTracker = null, Dictionary<Type, GraphNode>? createdNodes = null)
    {
        return GetGraphNodes(typeof(TNode), typeof(TExpectedType), cycleTracker, createdNodes);
    }

    /// <summary>
    /// Turns <paramref name="nodeType"/> into a <see cref="GraphNode"/> and then does the same for all of its 
    /// constructors parameters recursively. The constructor parameters are what are considered 
    /// dependencies for <paramref name="nodeType"/>. Constructor parameters that don't inherit from <paramref name="expectedType"/>
    /// are assumed to be invariants that are not created inside the graph
    /// </summary>
    /// <param name="expectedType">Parameters of this type are assumed to be generated inside the graph. Anything that is not this type, or does not inherit from it is considered an invariant</param>
    /// <param name="nodeType">The type to turn into a <see cref="GraphNode"/></param>
    /// <returns>The graphs root <see cref="GraphNode"/></returns>
    /// <exception cref="CycleInGraphException"></exception>
    internal GraphNode GetGraphNodes(Type nodeType, Type expectedType, GraphCycleTracker<Type>? cycleTracker = null, Dictionary<Type, GraphNode>? createdNodes = null)
    {
        cycleTracker ??= new();
        createdNodes ??= [];
        if (createdNodes.TryGetValue(nodeType, out GraphNode? existingNode))
        {
            return existingNode;
        }
        cycleTracker.Visit(nodeType);
        if (!IsTypeOrSubclassOf(nodeType, expectedType))
        {
            cycleTracker.Unvisit(nodeType);
            var invariantNode = CreateInvariant(nodeType);
            createdNodes.Add(nodeType, invariantNode);
            return invariantNode;
        }
        var (_, dependentNodeTypes) = GetTypesFromFirstConstructor(nodeType);
        if (dependentNodeTypes is null)
        {
            cycleTracker.Unvisit(nodeType);
            var nodeWithNoDeps = CreateInvariant(nodeType);
            createdNodes.Add(nodeType, nodeWithNoDeps);
            return nodeWithNoDeps;
        }
        if (!createdNodes.TryGetValue(nodeType, out GraphNode? nodeWithDeps))
        {
            nodeWithDeps = new GraphNode()
            {
                NodeType = nodeType,
            };
            createdNodes.Add(nodeType, nodeWithDeps);
        }
        foreach (var dependentNodeType in dependentNodeTypes)
        {
            var dependentNode = GetGraphNodes(nodeType, expectedType, cycleTracker, createdNodes);
            dependentNode.Dependents.Add(nodeWithDeps);
            nodeWithDeps.Dependencies.Add(dependentNode);
        }
        cycleTracker.Unvisit(nodeType);
        return nodeWithDeps;
    }

    internal GraphNode CreateInvariant(Type node) => new() { NodeType = node };

    public (ConstructorInfo?, List<Type>) GetTypesFromFirstConstructor<T>()
    {
        return GetTypesFromFirstConstructor(typeof(T));
    }

    /// <summary>
    /// TODO: Target certain constructors instead of the first constructor
    /// </summary>
    public (ConstructorInfo?, List<Type>) GetTypesFromFirstConstructor(Type type)
    {
        var graphNodes = new List<Type>();
        var ctorInfos = type.GetConstructors();
        if (ctorInfos is null || ctorInfos.Length == 0)
        {
            return (null, graphNodes);
        }
        var ctorInfo = ctorInfos[0];
        if (ctorInfo is null)
        {
            return (ctorInfo, graphNodes);
        }
        foreach (var parameterInfo in ctorInfo.GetParameters())
        {
            graphNodes.Add(parameterInfo.ParameterType);
        }
        return (ctorInfo, graphNodes);
    }

    public bool IsTypeOrSubclassOf<T1, T2>()
    {
        return IsTypeOrSubclassOf(typeof(T1), typeof(T2));
    }


    public bool IsTypeOrSubclassOf(Type type1, Type type2)
    {
        return type1 == type2 || type2.IsSubclassOf(type2);
    }

    /// <summary>
    /// https://stackoverflow.com/questions/374651/how-to-check-if-an-object-is-nullable
    /// </summary>
    public bool IsNullable<T>(T obj)
    {
        if (obj is null) return true; // obvious
        return IsNullable<T>();
    }

    /// <inheritdoc cref="IsNullable{T}(T)" />
    public bool IsNullable<T>()
    {
        var type = typeof(T);
        if (!type.IsValueType) return true; // ref-type
        if (Nullable.GetUnderlyingType(type) != null) return true; // Nullable<T>
        return false; // value-type
    }
}
