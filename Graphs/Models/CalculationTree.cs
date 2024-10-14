using System.Reflection;
using Graphs.Exceptions;
using Graphs.Extensions;
using Graphs.Interfaces;
using Graphs.Services;

namespace Graphs.Models;

// TODO: Add code generation to generate the expected external unnecessaryTypes that need to be provided to <see cref="Resolve"/> as a class or object
/// <summary>A graph of nodes that exist to resolve the value of a graphNode with type T</summary>
/// <typeparam name="T">The graphNode that the entire graph exists to resolve the value for</typeparam>
public class CalculationTree<TRootNode, TRootNodeValue> : ICalculationTree<TRootNode, TRootNodeValue> where TRootNode : IInfoNode<TRootNodeValue>
{
    static readonly Type AllowedNodeType = typeof(IInfoNode<object>);
    readonly GraphHelpers graphHelpers = new();
    /// <summary>The graphNode structure necessary for sorting the graph and constructing the instances of <see cref="IInfoNode"/></summary>
    internal Dictionary<Type, GraphNode> GraphNodes { get; private set; } = [];
    /// <summary>Maps type to InfoNode possibly containing instance data</summary>
    readonly Dictionary<Type, IInfoNode<object>> Nodes = [];
    readonly HashSet<Type> ExternalDependencies = [];
    readonly GraphNode RootNode;

    #region Public Methods

    public TRootNode Root => (TRootNode)Nodes[RootNode.NodeType];
    public TRootNodeValue? Value => (TRootNodeValue?)Nodes[RootNode.NodeType].Value;

    /// <summary>
    /// Returns a graph with all nodes necessary to calculate the nodeType of type T
    /// </summary>
    public CalculationTree()
    {
        RootNode = Add<TRootNode>();
    }

    /// <summary>
    /// Resolves the graph by sorting it topologically and resolving nodes one by one. 
    /// Returns the value of the given entry node in the <see cref="CalculationTree{TEntryNode, TEntryNodeValue}"/> definition.
    /// <br/>
    /// <br/>
    /// Assigns the value returned form <see cref="IInfoNode.ResolveObject"/> to <see cref="IInfoNode.ObjectValue"/>, and
    /// <see cref="IInfoNode{T}.Resolve"/> to <see cref="IInfoNode{T}.Value"/>
    /// <br/>
    /// <br/>
    /// Calls <see cref="IInfoNode.ResetErrors"/> on each node in the graph before calling <see cref="IInfoNode.ResolveObject"/>
    /// </summary>
    /// <exception cref="NullReferenceException"></exception>
    /// <exception cref="MissingExternalGraphDependencyException"></exception>
    /// <exception cref="InvalidTypeAddedToGraphException"></exception>
    public TRootNodeValue? Resolve(params object[] externalGraphDependencies)
    {
        var sortedGraphNodes = SetupNodeResolution(externalGraphDependencies);
        foreach (var graphNode in sortedGraphNodes)
        {
            // TODO: pull this logic out into its own function that can also be used in CalculateAsync
            // to consolidate duplicate logic
            var isExternalDependency = ExternalDependencies.Contains(graphNode.NodeType);
            if (isExternalDependency)
            {
                continue;
            }
            var infoNode = Nodes[graphNode.NodeType];
            infoNode.Graph = this;
            infoNode.Resolve();
        }
        return Root.Value;
    }

    /// <inheritdoc cref="Resolve(object[])"/>
    public async Task<TRootNodeValue?> ResolveAsync(params object[] externalGraphDependencies)
    {
        var sortedGraphNodes = SetupNodeResolution(externalGraphDependencies);
        foreach (var graphNode in sortedGraphNodes)
        {
            var isExternalDependency = ExternalDependencies.Contains(graphNode.NodeType);
            if (isExternalDependency)
            {
                continue;
            }
            var infoNode = Nodes[graphNode.NodeType];
            infoNode.Graph = this;
            await infoNode.ResolveAsync();
        }
        return Root.Value;
    }

    /// <summary>
    /// Fetches an <see cref="IInfoNode{K}"/> of type <typeparamref name="TNode"/> from the graph. Will throw if <see cref="Resolve(object[])"/> has not been called
    /// </summary>
    /// <typeparam name="TNode">The type of node to fetch in the graph</typeparam>
    /// <returns>The node instance found in the graph</returns>
    /// <exception cref="NodeOutsideOfGraphException{TGraph, TNode}"></exception>
    public TNode Get<TNode, TNodeValue>() where TNode : IInfoNode<TNodeValue>
    {
        if (Nodes.ContainsKey(typeof(TNode)))
        {
            return (TNode)Nodes[typeof(TNode)];
        }
        throw new NodeOutsideOfGraphException<CalculationTree<TRootNode, TRootNodeValue>, TNode>(this);
    }

    /// <summary>
    /// Fetches an external dependency instance that was provided to <see cref="Resolve(object[])"/>. 
    /// This will always fail if called before the graph is resolved
    /// </summary>
    /// <returns>An instance of type <typeparamref name="T"/></returns>
    /// <exception cref="NodeOutsideOfGraphException{CalculationTree{TRootNode, TRootNodeValue}, T}"></exception>
    public T GetExternalDependency<T>()
    {
        if (Nodes.ContainsKey(typeof(T)))
        {
            return (T)Nodes[typeof(T)].ObjectValue;
        }
        throw new NodeOutsideOfGraphException<CalculationTree<TRootNode, TRootNodeValue>, T>(this);
    }

    /// <summary>
    /// Fetches all the <see cref="Type"/>s of nodes in the graph
    /// </summary>
    public IEnumerable<Type> GetNodes()
    {
        return GraphNodes.Keys;
    }

    public IEnumerable<string> GetErrors()
    {
        var errors = new List<string>();
        foreach (var node in Nodes.Values)
        {
            var nodeErrors = node.GetErrors();
            if (nodeErrors.Any())
            {
                errors.AddRange(nodeErrors);
            }
        }
        return errors;
    }

    /// <summary>
    /// Mainly for debugging purposes. <see cref="Resolve(object[])"/> must be called before this can output anything
    /// </summary>
    public IEnumerable<Type> GetUnnecessaryExternalDependencies()
    {
        if (Nodes.Count == 0)
        {
            return new List<Type>();
        }
        var unnecessaryTypes = new List<Type>();
        var expectedExternalTypes = GetExternalDependencyGraphNodes().Select(graphNode => graphNode.NodeType);
        foreach (var providedType in Nodes.Keys)
        {
            if (!expectedExternalTypes.Contains(providedType) && !providedType.IsSubclassOf(typeof(InfoNode)))
            {
                unnecessaryTypes.Add(providedType);
            }
        }
        return unnecessaryTypes;
    }

    #endregion

    IEnumerable<GraphNode> SetupNodeResolution(params object[] externalGraphDependencies)
    {
        AssignExternalDependencies(externalGraphDependencies);
        var sortedNodes = Sort();
        var sortedNodeTypes = sortedNodes.Select(node => node.NodeType);
        foreach (var nodeType in sortedNodeTypes)
        {
            var isExternalDependency = ExternalDependencies.Contains(nodeType);
            if (isExternalDependency)
            {
                continue;
            }
            var infoNode = CreateInfoNode(nodeType);
            Nodes[nodeType] = infoNode;
        }
        return sortedNodes;
    }

    /// <summary>
    /// Adds a nodeType to the graph and all its dependencies. Also performs a sort on the nodes by their in-degree
    /// </summary>
    GraphNode Add<TNode, TNodeValue>() where TNode : IInfoNode<TNodeValue>
    {
        var node = graphHelpers.GetGraphNodes<TNode>(AllowedNodeType);
        var flattenedNodes = graphHelpers.FlattenNodes(node);
        foreach (var node_ in flattenedNodes)
        {
            GraphNodes.Add(node_.NodeType, node_);
        }
        return node;
    }

    IEnumerable<GraphNode> Sort() => new KahnSorter().KahnSort(this);

    InfoNode CreateInfoNode(Type nodeType)
    {
        if (!graphHelpers.IsTypeOrSubclassOf(nodeType, AllowedNodeType))
        {
            throw new InvalidTypeAddedToGraphException(nodeType, AllowedNodeType);
        }
        var (ctorInfo, ctorArgTypes, ctorArgInstances) = GetConstructorArgInstances(nodeType);
        if (ctorInfo is null || ctorArgTypes is null || ctorArgInstances is null)
        {
            throw new NullReferenceException($"Failed to find Constructor for type '{nodeType.Name}'");
        }
        InfoNode? nodeInstance;
        var ctorInstancesWithExternalValues = ctorArgInstances.Select(node => node.GetType() == typeof(ExternalInfoNode) ? node.ObjectValue : node).ToArray();
        nodeInstance = (InfoNode?)ctorInfo.Invoke(ctorInstancesWithExternalValues);
        if (nodeInstance is null)
        {
            throw new NullReferenceException($"Unexpected null created from Activator. Failed to create instance of {nodeType} in graph");
        }
        return nodeInstance;
    }

    ExternalInfoNode<T> CreateExternalInfoNode<T>(T Value)
    {
        var infoNode = new ExternalInfoNode<T>();
        infoNode.Value = Value;
        return infoNode;
    }

    (ConstructorInfo?, List<Type>?, IEnumerable<InfoNode?>) GetConstructorArgInstances(Type nodeType)
    {
        var instances = new List<InfoNode?>();
        var (ctorInfo, constructorArgTypes) = graphHelpers.GetTypesFromFirstConstructor(nodeType);
        if (constructorArgTypes is null || ctorInfo is null)
        {
            return (null, null, instances);
        }
        foreach (var type in constructorArgTypes)
        {
            if (Nodes.TryGetValueWithGenerics(type, out InfoNode? value))
            {
                instances.Add(value);
                continue;
            }
            if (graphHelpers.IsNullable(type))
            {
                instances.Add(null);
                continue;
            }
            throw new ArgumentOutOfRangeException($"Graph does not contain an instance of {type.Name} that is necessary to construct nodeType {nodeType.Name}");
        }
        return (ctorInfo, constructorArgTypes, instances);
    }

    internal HashSet<GraphNode> GetExternalDependencyGraphNodes()
    {
        var externalDependencies = new HashSet<GraphNode>();
        foreach (var nodeType in GraphNodes.Keys)
        {
            if (nodeType != AllowedNodeType && !nodeType.IsSubclassOf(AllowedNodeType))
            {
                ExternalDependencies.Add(nodeType);
                externalDependencies.Add(GraphNodes[nodeType]);
            }
        }
        return externalDependencies;
    }

    void AssignExternalDependencies(params object[] externalGraphDependencies)
    {
        ;
        var externalGraphNodes = GetExternalDependencyGraphNodes();
        var externalGraphNodeTypes = externalGraphNodes.Select(node => node.NodeType).ToHashSet();
        // Assign 1:1 unnecessaryTypes to instances
        foreach (var dependency in externalGraphDependencies)
        {
            AssignType(externalGraphNodeTypes!, dependency);
        }
        // Assign unnecessaryTypes to possible interface implementations
        // Ex: we have an instance of List<int> and expect a type of IEnumerable<int>
        // The List<int> will be assigned to the IEnumerable<int>
        foreach (var graphNode in externalGraphNodes)
        {
            foreach (var dependency in externalGraphDependencies)
            {
                if (graphNode.NodeType.IsAssignableFrom(dependency.GetType())) {
                    AssignType(externalGraphNodeTypes!, dependency, graphNode.NodeType);
                }
            }
        }
        // Oh noooo
        if (externalGraphNodeTypes.Count > 0)
        {
            var missingGraphNodes = externalGraphNodes.Where(graphNode => externalGraphNodeTypes.Contains(graphNode.NodeType));
            throw new MissingExternalGraphDependencyException<TRootNode>(missingGraphNodes);
        }

        void AssignType(HashSet<Type> typeHash, object dependency, Type? type = null)
        {
            var dependencyType = dependency.GetType();
            var infoNode = CreateExternalInfoNode(dependency);
            if (type is null)
            {
                Nodes[dependencyType] = infoNode;
                typeHash.Remove(dependencyType);
            }
            else
            {
                Nodes[type] = infoNode;
                typeHash.Remove(type);
            }
        }
    }
}
