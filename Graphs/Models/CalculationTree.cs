using System.Reflection;
using Graphs.Exceptions;
using Graphs.Extensions;
using Graphs.Interfaces;
using Graphs.Services;

namespace Graphs.Models;

// TODO: Add code generation to generate the expected invariants that need to be provided to <see cref="Resolve"/> as a class or object
/// <summary>
/// A directed tree graph that exists to calculate the value for the provided <typeparamref name="TRootNode"/>.
/// It is assumed that each nodes dependencies are their constructor parameters
/// </summary>
public class CalculationTree<TRootNode> : ICalculationTree<TRootNode>, IGraph<CalculationNode> where TRootNode : CalculationNode
{
    readonly GraphHelpers graphHelpers = new();
    /// <summary>The graphNode structure necessary for sorting the graph and constructing the instances of <see cref="IInfoNode"/></summary>
    internal Dictionary<Type, GraphNode> GraphNodes { get; private set; } = [];
    /// <summary>Maps type to InfoNode possibly containing instance data</summary>
    readonly Dictionary<Type, CalculationNode> Nodes = [];
    readonly HashSet<Type> ExternalDependencies = [];
    readonly GraphNode RootNode;

    #region Public Methods

    public TRootNode Root => (TRootNode)Nodes[RootNode.NodeType];

    /// <summary>
    /// Returns a graph with all nodes necessary to calculate the nodeType of type T
    /// </summary>
    public CalculationTree()
    {
        RootNode = Add<TRootNode>();
    }

    /// <summary>
    /// Resolves the graph by sorting it topologically and resolving nodes one by one. 
    /// Returns the value of the given entry node in the <see cref="CalculationTree{TEntryNode}"/> definition.
    /// <br/><br/>
    /// For each node in the graph, assigns the value returned form <see cref="ICalculationNode.Calculate"/> to <see cref="ICalculationNode.Value"/>
    /// <br/><br/>
    /// Calls <see cref="CalculationNode{}.ResetErrors"/> on each node in the graph before calling <see cref="CalculationNode{}.Calculate"/>
    /// </summary>
    /// <exception cref="NullReferenceException"></exception>
    /// <exception cref="MissingExternalGraphDependencyException"></exception>
    /// <exception cref="InvalidTypeAddedToGraphException"></exception>
    public TRootNode Calculate(params object[] invariants)
    {
        var sortedGraphNodes = SetupNodeResolution(invariants);
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
            infoNode.Calculate();
        }
        return Root;
    }

    /// <inheritdoc cref="Calculate(object[])"/>
    public async Task<TRootNode> CalculateAsync(params object[] invariants)
    {
        var sortedGraphNodes = SetupNodeResolution(invariants);
        foreach (var graphNode in sortedGraphNodes)
        {
            var isExternalDependency = ExternalDependencies.Contains(graphNode.NodeType);
            if (isExternalDependency)
            {
                continue;
            }
            var infoNode = Nodes[graphNode.NodeType];
            infoNode.Graph = this;
            await infoNode.CalculateAsync();
        }
        return Root;
    }

    /// <summary>
    /// Fetches an <see cref="ICalculationNode{K}"/> of type <typeparamref name="TNode"/> from the graph. Will throw if <see cref="Resolve(object[])"/> has not been called
    /// </summary>
    /// <typeparam name="TNode">The type of node to fetch in the graph</typeparam>
    /// <returns>The node instance found in the graph</returns>
    /// <exception cref="NodeOutsideOfGraphException{TGraph, TNode}"></exception>
    public TNode Get<TNode, TNodeValue>() where TNode : CalculationNode<TNodeValue>
    {
        if (Nodes.ContainsKey(typeof(TNode)))
        {
            return (TNode)Nodes[typeof(TNode)];
        }
        throw new NodeOutsideOfGraphException<CalculationTree<TRootNode>, TNode>(this);
    }

    /// <summary>
    /// Fetches an invariant that was provided to <see cref="Calculate(object[])"/>. 
    /// This will always fail if called before the graph is resolved
    /// </summary>
    /// <returns>An instance of type <typeparamref name="T"/></returns>
    /// <exception cref="NodeOutsideOfGraphException{CalculationTree{TRootNode}, T}"></exception>
    public T? GetExternalDependency<T>() where T : CalculationNode
    {
        if (Nodes.ContainsKey(typeof(T)))
        {
            return (T?)Nodes[typeof(T)].Value;
        }
        throw new NodeOutsideOfGraphException<CalculationTree<TRootNode>, T>(this);
    }

    /// <summary>
    /// Fetches all the <see cref="Type"/>s of nodes in the graph
    /// </summary>
    public IEnumerable<Type> GetNodes()
    {
        return GraphNodes.Keys;
    }

    public IReadOnlySet<object> GetErrors()
    {
        var errors = new HashSet<object>();
        foreach (var node in Nodes.Values)
        {
            var nodeErrors = node.GetErrors();
            if (nodeErrors.Any())
            {
                foreach (var error in nodeErrors)
                {
                    if (errors.Contains(error)) continue;
                    errors.Add(error);
                }
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
        var expectedExternalTypes = GetInvariantGraphNodes().Select(graphNode => graphNode.NodeType);
        foreach (var providedType in Nodes.Keys)
        {
            if (!expectedExternalTypes.Contains(providedType) && !providedType.IsSubclassOf(typeof(CalculationNode)))
            {
                unnecessaryTypes.Add(providedType);
            }
        }
        return unnecessaryTypes;
    }

    #endregion

    IEnumerable<GraphNode> SetupNodeResolution(params object[] invariants)
    {
        AssignInvariants(invariants);
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
    GraphNode Add<TNode>() where TNode : CalculationNode
    {
        var node = graphHelpers.GetGraphNodes<TNode, CalculationNode>();
        var flattenedNodes = graphHelpers.FlattenNodes(node);
        foreach (var node_ in flattenedNodes)
        {
            GraphNodes.Add(node_.NodeType, node_);
        }
        return node;
    }

    IEnumerable<GraphNode> Sort() => new KahnSorter().KahnSort(this);

    CalculationNode CreateInfoNode(Type nodeType)
    {
        if (!graphHelpers.IsTypeOrSubclassOf(nodeType, typeof(CalculationNode)))
        {
            throw new InvalidTypeAddedToGraphException(nodeType, typeof(CalculationNode));
        }
        var (ctorInfo, ctorArgTypes, ctorArgInstances) = GetConstructorArgInstances(nodeType);
        if (ctorInfo is null || ctorArgTypes is null || ctorArgInstances is null)
        {
            throw new NullReferenceException($"Failed to find Constructor for type '{nodeType.Name}'");
        }
        CalculationNode? nodeInstance;
        var ctorInstancesWithExternalValues = ctorArgInstances.Select(node => node.GetType() == typeof(InvariantNode) ? node.Value : node).ToArray();
        nodeInstance = (CalculationNode?)ctorInfo.Invoke(ctorInstancesWithExternalValues);
        if (nodeInstance is null)
        {
            throw new NullReferenceException($"Unexpected null created from Activator. Failed to create instance of {nodeType} in graph");
        }
        return nodeInstance;
    }

    InvariantNode<T> CreateInvariantNode<T>(T Value)
    {
        var infoNode = new InvariantNode<T>();
        infoNode.SetValue(Value);
        return infoNode;
    }

    (ConstructorInfo?, List<Type>?, IEnumerable<CalculationNode?>) GetConstructorArgInstances(Type nodeType)
    {
        var instances = new List<CalculationNode?>();
        var (ctorInfo, constructorArgTypes) = graphHelpers.GetTypesFromFirstConstructor(nodeType);
        if (constructorArgTypes is null || ctorInfo is null)
        {
            return (null, null, instances);
        }
        foreach (var type in constructorArgTypes)
        {
            if (Nodes.TryGetValueWithGenerics(type, out CalculationNode? value))
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

    internal HashSet<GraphNode> GetInvariantGraphNodes()
    {
        var invariants = new HashSet<GraphNode>();
        foreach (var nodeType in GraphNodes.Keys)
        {
            if (nodeType != AllowedNodeType && !nodeType.IsSubclassOf(AllowedNodeType))
            {
                ExternalDependencies.Add(nodeType);
                invariants.Add(GraphNodes[nodeType]);
            }
        }
        return invariants;
    }

    void AssignInvariants(params object[] invariants)
    {
        ;
        var invariantsGraphNodes = GetInvariantGraphNodes();
        var invariantsGraphNodeTypes = invariantsGraphNodes.Select(node => node.NodeType).ToHashSet();
        // Assign 1:1 unnecessaryTypes to instances
        foreach (var dependency in invariants)
        {
            AssignType(invariantsGraphNodeTypes!, dependency);
        }
        // Assign unnecessaryTypes to possible interface implementations
        // Ex: we have an instance of List<int> and expect a type of IEnumerable<int>
        // The List<int> will be assigned to the IEnumerable<int>
        foreach (var graphNode in invariantsGraphNodes)
        {
            foreach (var dependency in invariants)
            {
                if (graphNode.NodeType.IsAssignableFrom(dependency.GetType())) {
                    AssignType(invariantsGraphNodeTypes!, dependency, graphNode.NodeType);
                }
            }
        }
        // Oh noooo
        if (invariantsGraphNodeTypes.Count > 0)
        {
            var missingGraphNodes = invariantsGraphNodes.Where(graphNode => invariantsGraphNodeTypes.Contains(graphNode.NodeType));
            throw new MissingExternalGraphDependencyException<TRootNode>(missingGraphNodes);
        }

        void AssignType(HashSet<Type> typeHash, object dependency, Type? type = null)
        {
            var dependencyType = dependency.GetType();
            var infoNode = CreateInvariantNode(dependency);
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
