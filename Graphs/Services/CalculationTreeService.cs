using Graphs.Exceptions;
using Graphs.Extensions;
using Graphs.Interfaces;
using Graphs.Models;

namespace Graphs.Services;

public class CalculationTreeService
{
    /// <summary>
    /// Resolves the graph by sorting it topologically and resolving nodes one by one. 
    /// Returns the value of the given entry node in the <see cref="ICalculationTree{TEntryNode}"/> definition.
    /// <br/><br/>
    /// For each node in the graph, assigns the value returned form <see cref="ICalculationNode.Calculate"/> to <see cref="ICalculationNode.Value"/>
    /// <br/><br/>
    /// Calls <see cref="ICalculationNode{}.ResetErrors"/> on each node in the graph before calling <see cref="ICalculationNode{}.Calculate"/>
    /// </summary>
    /// <exception cref="NullReferenceException"></exception>
    /// <exception cref="MissingInvariantException{}"></exception>
    /// <exception cref="InvalidTypeAddedToGraphException"></exception>
    public TRootNode Calculate<TRootNode>(params object[] invariants) where TRootNode : ICalculationNode
    {
        var calculationTree = new CalculationTree<TRootNode>();
        var sortedGraphNodes = SetupNodeResolution(invariants);
        foreach (var graphNode in sortedGraphNodes)
        {
            // TODO: pull this logic out into its own function that can also be used in CalculateAsync
            // to consolidate duplicate logic
            var isExternalDependency = calculationTree.Invariants.Contains(graphNode.Type);
            if (isExternalDependency)
            {
                continue;
            }
            var infoNode = Nodes[graphNode.Type];
            infoNode.Graph = this;
            infoNode.Calculate();
        }
        return Root;
    }

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


    /// <inheritdoc cref="Calculate(object[])"/>
    //public async Task<TRootNode> CalculateAsync(params object[] invariants)
    //{
    //    var sortedGraphNodes = SetupNodeResolution(invariants);
    //    foreach (var graphNode in sortedGraphNodes)
    //    {
    //        var isExternalDependency = Invariants.Contains(graphNode.Type);
    //        if (isExternalDependency)
    //        {
    //            continue;
    //        }
    //        var infoNode = Nodes[graphNode.Type];
    //        infoNode.Graph = this;
    //        await infoNode.CalculateAsync();
    //    }
    //    return Root;
    //}

    InvariantNode<T> CreateInvariantNode<T>(T Value, ICalculationTree<ICalculationNode> graph)
    {
        var infoNode = new InvariantNode<T>()
        {
            Graph = graph,
            Value = Value
        };
        return infoNode;
    }

    internal ICalculationNode CreateCalculationNodes(ICalculationTree<ICalculationNode> graph, Type nodeType, GraphCycleTracker<Type>? cycleTracker = null, Dictionary<Type, ICalculationNode>? createdNodes = null)
    {
        cycleTracker ??= new();
        createdNodes ??= [];
        cycleTracker.Visit(nodeType);
        // Already visited this node, continue
        if (createdNodes.TryGetValue(nodeType, out ICalculationNode? value))
        {
            return value;
        }
        var dependencies = CalculationNode.GetDependenciesTypes(nodeType);
        // If the node is not a subclass of the expected type, it is an invariant
        // If there are no constructor parameters, this is a node with no dependencies
        if (!nodeType.IsTypeOrSubclassOf(typeof(ICalculationNode)) || dependencies.Any() == false)
        {
            cycleTracker.Unvisit(nodeType);
            var invariantNode = CreateInvariantNode(nodeType, graph);
            createdNodes.Add(nodeType, invariantNode);
            return invariantNode;
        }
        // Recursively get the nodes for the constructor parameters
        foreach (var dependency in dependencies)
        {
            var dependencyInstance = CreateCalculationNodes(graph, nodeType, cycleTracker, createdNodes);
            graph.AddNode(dependencyInstance);
        }
        // Create the node and add it to the created nodes
        var newNode = CreateCalculationNode(graph, nodeType);
        createdNodes.Add(nodeType, newNode);
        cycleTracker.Unvisit(nodeType);
        return newNode;
    }

    ICalculationNode CreateCalculationNode(ICalculationTree<ICalculationNode> graph, Type nodeType)
    {
        if (!nodeType.IsTypeOrSubclassOf(typeof(ICalculationNode)))
        {
            throw new InvalidTypeAddedToGraphException(nodeType, typeof(ICalculationNode));
        }
        var (ctorInfo, ctorArgTypes, ctorArgInstances) = nodeType.GetConstructorArgInstances(graph.GetAll());
        if (ctorInfo is null || ctorArgTypes is null || ctorArgInstances is null)
        {
            throw new NullReferenceException($"Failed to find Constructor for type '{nodeType.Name}'");
        }
        ICalculationNode? nodeInstance;
        var ctorInstancesWithExternalValues = ctorArgInstances
            .Select(arg => (ICalculationNode?)arg)
            .Select(arg => arg is null ? arg : arg.Value).ToArray();
        nodeInstance = (ICalculationNode?)ctorInfo.Invoke(ctorInstancesWithExternalValues);
        if (nodeInstance is null)
        {
            throw new NullReferenceException($"Unexpected null created from Activator. Failed to create instance of {nodeType} in graph");
        }
        return nodeInstance;
    }

    internal ICalculationNode CreateInvariantNode(object? value, IGraph<ICalculationNode> graph)
    {
        return new InvariantNode()
        {
            Graph = graph,
            Value = value,
        };
    }
}
