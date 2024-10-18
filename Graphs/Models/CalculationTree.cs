using System.Reflection;
using System.Xml.Linq;
using Graphs.Exceptions;
using Graphs.Extensions;
using Graphs.Interfaces;
using Graphs.Services;

namespace Graphs.Models;

// TODO: Add code generation to generate the expected invariants that need to be provided to Calculate() as a class or object
/// <summary>
/// A directed tree graph that exists to calculate the value for the provided <typeparamref name="TRootNode"/>.
/// It is assumed that each nodes dependencies are their constructor parameters
/// </summary>
public class CalculationTree<TRootNode> : ICalculationTree<TRootNode>, IGraph<ICalculationNode> where TRootNode : ICalculationNode
{
    readonly GraphService graphHelpers = new();
    /// <summary>The graphNode structure necessary for sorting the graph and constructing the instances of <see cref="IInfoNode"/></summary>
    internal Dictionary<Type, GraphNode> GraphNodes { get; private set; } = [];
    readonly GraphNode RootNode;
    /// <summary>Maps type to InfoNode possibly containing instance data</summary>
    readonly Dictionary<Type, ICalculationNode> Nodes = [];
    public IEnumerable<object> ProvidedInvariants { get; init; } = [];

    #region Public Methods

    public TRootNode Root => (TRootNode)Nodes[RootNode.NodeType];

    /// <summary>
    /// Returns a graph with all nodes necessary to calculate the nodeType of type T
    /// </summary>
    public CalculationTree(params object[] invariants)
    {
        ProvidedInvariants = invariants;
        RootNode = Add<TRootNode>();
    }

    /// <summary>
    /// Fetches an <see cref="ICalculationNode{K}"/> of type <typeparamref name="TNode"/> from the graph. Will throw if <see cref="Resolve(object[])"/> has not been called
    /// </summary>
    /// <typeparam name="TNode">The type of node to fetch in the graph</typeparam>
    /// <returns>The node instance found in the graph</returns>
    /// <exception cref="NodeOutsideOfGraphException{TGraph, TNode}"></exception>
    public TNode Get<TNode>() where TNode : ICalculationNode
    {
        if (Nodes.ContainsKey(typeof(TNode)))
        {
            return (TNode)Nodes[typeof(TNode)];
        }
        throw new NodeOutsideOfGraphException<TNode, CalculationTree<TRootNode>, ICalculationNode>(this);
    }

    /// <summary>
    /// Fetches all of the nodes in the graph
    /// </summary>
    public IEnumerable<ICalculationNode> GetAll()
    {
        return Nodes.Values;
    }

    /// <summary>
    /// Fetches all the <see cref="Type"/>s of nodes in the graph
    /// </summary>
    public IEnumerable<Type> GetAllNodeTypes()
    {
        return Nodes.Keys;
    }

    public IInvariantNode? GetInvariant<T>()
    {
        if (Nodes.ContainsKey(typeof(T)))
        {
            return (IInvariantNode?)Nodes[typeof(T)].Value;
        }
        throw new NodeOutsideOfGraphException<T, CalculationTree<TRootNode>, ICalculationNode>(this);
    }

    public IEnumerable<IInvariantNode> GetInvariants()

    {
        var invariants = new List<IInvariantNode>();
        foreach (var node in Nodes)
        {
            if (node.Value is IInvariantNode node1)
            {
                invariants.Add(node1);
            }
        }
        return invariants;
    }

    public IEnumerable<IInvariantNode> GetUnnecessaryInvariants()
    {
        if (Nodes.Count == 0)
        {
            return [];
        }
        var unnecessaryTypes = new List<InvariantNode>();
        var expectedExternalTypes = GetInvariantGraphNodes().Select(graphNode => graphNode.NodeType);
        foreach (var providedType in Nodes.Keys)
        {
            if (!expectedExternalTypes.Contains(providedType) && !providedType.IsSubclassOf(typeof(ICalculationNode)))
            {
                //unnecessaryTypes.Add(providedType);
                unnecessaryTypes.Add((InvariantNode)Nodes[providedType]);
            }
        }
        return unnecessaryTypes;
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

    #endregion

    #region Private methods

    IEnumerable<GraphNode> SetupNodeResolution(params object[] invariants)
    {
        AssignInvariants(invariants);
        var sortedNodes = Sort();
        var sortedNodeTypes = sortedNodes.Select(node => node.NodeType);
        foreach (var nodeType in sortedNodeTypes)
        {
            var isInvariant = Invariants.ContainsKey(nodeType);
            if (isInvariant)
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
    GraphNode Add<TNode>() where TNode : ICalculationNode
    {
        var node = graphHelpers.GetGraphNodes<TNode, ICalculationNode>();
        var flattenedNodes = graphHelpers.FlattenNodes(node);
        foreach (var graphNode in flattenedNodes)
        {
            GraphNodes.Add(graphNode.NodeType, graphNode);
        }
        return node;
    }

    public IEnumerable<Type> GetNecessaryInvariants() =>
        GraphNodes.Values
        .Where(node => node.IsInvariant)
        .Select(node => node.NodeType);

    IEnumerable<GraphNode> Sort() => new KahnSorter().KahnSort(this);

    ICalculationNode CreateInfoNode(Type nodeType)
    {
        if (!graphHelpers.IsTypeOrSubclassOf(nodeType, typeof(ICalculationNode)))
        {
            throw new InvalidTypeAddedToGraphException(nodeType, typeof(ICalculationNode));
        }
        var (ctorInfo, ctorArgTypes, ctorArgInstances) = GetConstructorArgInstances(nodeType);
        if (ctorInfo is null || ctorArgTypes is null || ctorArgInstances is null)
        {
            throw new NullReferenceException($"Failed to find Constructor for type '{nodeType.Name}'");
        }
        ICalculationNode? nodeInstance;
        var ctorInstancesWithExternalValues = ctorArgInstances.Select(arg => arg.GetType() == typeof(InvariantNode) ? arg.Value : arg).ToArray();
        nodeInstance = (ICalculationNode?)ctorInfo.Invoke(ctorInstancesWithExternalValues);
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

    (ConstructorInfo?, List<Type>?, IEnumerable<ICalculationNode?>) GetConstructorArgInstances(Type nodeType)
    {
        var instances = new List<ICalculationNode?>();
        var (ctorInfo, constructorArgTypes) = graphHelpers.GetTypesFromFirstConstructor(nodeType);
        if (constructorArgTypes is null || ctorInfo is null)
        {
            return (null, null, instances);
        }
        foreach (var type in constructorArgTypes)
        {
            if (Nodes.TryGetValueWithGenerics(type, out ICalculationNode? value))
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

    void SetInvariants()
    {
        Invariants.Clear();
        foreach (var type in GraphNodes.Keys)
        {
            
        }
    }

    void AssignInvariants(params object[] invariants)
    {
        SetInvariants(invariants);
        // Assign 1:1 unnecessaryTypes to instances
        foreach (var dependency in invariants)
        {
            AssignType(Invariants, dependency);
        }
        // Assign unnecessaryTypes to possible interface implementations
        // Ex: we have an instance of List<int> and expect a type of IEnumerable<int>
        // The List<int> will be assigned to the IEnumerable<int>
        foreach (var graphNode in invariantsGraphNodes)
        {
            foreach (var dependency in invariants)
            {
                if (graphNode.NodeType.IsAssignableFrom(dependency.GetType())) 
                {
                    AssignType(invariantsGraphNodeTypes!, dependency, graphNode.NodeType);
                }
            }
        }
        // Oh noooo
        if (invariantsGraphNodeTypes.Count > 0)
        {
            var missingGraphNodes = invariantsGraphNodes.Where(graphNode => invariantsGraphNodeTypes.Contains(graphNode.NodeType));
            throw new MissingInvariantException<TRootNode>(missingGraphNodes);
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
    
    #endregion
}
