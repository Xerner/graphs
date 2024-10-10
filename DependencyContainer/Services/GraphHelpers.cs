﻿using System.Reflection;
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
        if (cycleTracker is null)
        {
            cycleTracker = new();
        }
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

    /// <inheritdoc cref="GetGraphNodes(Type, Type, GraphCycleTracker{Type}, Dictionary{Type, GraphNode}?)"/>
    internal GraphNode GetGraphNodes<TExpectedInfoNodeType, TOriginNodeType>() where TExpectedInfoNodeType : InfoNode where TOriginNodeType : TExpectedInfoNodeType
    {
        return GetGraphNodes(typeof(TExpectedInfoNodeType), typeof(TOriginNodeType), new());
    }

    /// <inheritdoc cref="GetGraphNodes(Type, Type, GraphCycleTracker{Type}, Dictionary{Type, GraphNode}?)"/>
    internal GraphNode GetGraphNodes<TOriginNodeType>(Type expectedInfoNodeType) where TOriginNodeType : InfoNode
    {
        return GetGraphNodes(expectedInfoNodeType, typeof(TOriginNodeType), new());
    }

    /// <inheritdoc cref="GetGraphNodes(Type, Type, GraphCycleTracker{Type}, Dictionary{Type, GraphNode}?)"/>
    internal GraphNode GetGraphNodes(Type expectedInfoNodeType, Type graphType)
    {
        return GetGraphNodes(expectedInfoNodeType, graphType, new());
    }

    /// <summary>
    /// Turns nodeType into a <see cref="GraphNode"/> and then does the same for all of its 
    /// constructors parameters recursively. The constructor parameters are what are considered 
    /// dependencies for nodeType. Constructor parameters that don't inherit from expectedGraphNodeType
    /// are assumed to be "external dependencies" that are not created inside the graph
    /// </summary>
    /// <param name="expectedGraphNodeType">Parameters of this type are assumed to be generated inside the graph. Anything that is not this type, or does not inherit from it is considered an external dependency</param>
    /// <param name="nodeType">The type to turn into a <see cref="GraphNode"/></param>
    /// <returns>The graphs origin nodeWithNoDeps</returns>
    /// <exception cref="CycleInGraphException"></exception>
    private GraphNode GetGraphNodes(Type expectedGraphNodeType, Type nodeType, GraphCycleTracker<Type> cycleTracker, Dictionary<Type, GraphNode>? createdNodes = null)
    {
        if (createdNodes is null)
        {
            createdNodes = new();
        }
        if (createdNodes.TryGetValue(nodeType, out GraphNode? existingNode))
        {
            return existingNode;
        }
        cycleTracker.Visit(nodeType);
        if (!IsTypeOrSubclassOf(nodeType, expectedGraphNodeType))
        {
            cycleTracker.Unvisit(nodeType);
            var externalNode = NodeWithNoDependencies(nodeType);
            createdNodes.Add(nodeType, externalNode);
            return externalNode;
        }
        var (_, dependentNodeTypes) = GetTypesFromFirstConstructor(nodeType);
        if (dependentNodeTypes is null)
        {
            cycleTracker.Unvisit(nodeType);
            var nodeWithNoDeps = NodeWithNoDependencies(nodeType);
            createdNodes.Add(nodeType, nodeWithNoDeps);
            return nodeWithNoDeps;
        }
        if (!createdNodes.TryGetValue(nodeType, out GraphNode? nodeWithDeps))
        {
            nodeWithDeps = new GraphNode()
            {
                NodeType = nodeType,
                Dependencies = new(),
                Dependents = new(),
            };
            createdNodes.Add(nodeType, nodeWithDeps);
        }
        foreach (var dependentNodeType in dependentNodeTypes)
        {
            var dependentNode = GetGraphNodes(expectedGraphNodeType, dependentNodeType, cycleTracker, createdNodes);
            dependentNode.Dependents.Add(nodeWithDeps);
            nodeWithDeps.Dependencies.Add(dependentNode);
        }
        cycleTracker.Unvisit(nodeType);
        return nodeWithDeps;
    }

    internal GraphNode NodeWithNoDependencies(Type node) => new() { NodeType = node };

    /// <inheritdoc cref="GetTypesFromFirstConstructor(Type)"/>
    public (ConstructorInfo?, List<Type>)? GetTypesFromFirstConstructor<T>()
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

    public bool IsTypeOrSubclassOf(Type type, Type comparisonType)
    {
        return type == comparisonType || type.IsSubclassOf(comparisonType);
    }

    /// <summary>
    /// https://stackoverflow.com/questions/374651/how-to-check-if-an-object-is-nullable
    /// </summary>
    public bool IsNullable<T>(T obj)
    {
        if (obj == null) return true; // obvious
        return IsNullable<T>();
    }

    /// <inheritdoc cref="IsNullable{T}(T)" />
    public bool IsNullable<T>() => IsNullable(typeof(T));

    /// <inheritdoc cref="IsNullable{T}(T)" />
    public bool IsNullable(Type type)
    {
        if (!type.IsValueType) return true; // ref-type
        if (Nullable.GetUnderlyingType(type) != null) return true; // Nullable<T>
        return false; // value-type
    }
}
