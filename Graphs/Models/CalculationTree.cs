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

    public TRootNode RootNode { get; set; }

    public List<ICalculationNode> Nodes = [];

    public IEnumerable<IInvariantNode> ProvidedInvariants { get; private set; } = [];

    #region Getters

    public IEnumerable<object> GetProvidedInvariants() => ProvidedInvariants;

    public IEnumerable<IInvariantNode> GetAllInvariants() =>
        (IEnumerable<IInvariantNode>)
        GraphNodes
        .Where(node => node.IsInvariant())
        .Select(node => node.Type);

    /// <summary>
    /// The invariants actually needed to calculate the value of the root node
    /// </summary>
    public IEnumerable<IInvariantNode> GetNecessaryInvariants() =>
        GraphNodes.Values
        .Where(node => node.IsInvariant())
        .Select(node => node.Type);

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns a graph with all nodes necessary to calculate the nodeType of type T
    /// </summary>
    public CalculationTree(params object[] invariants)
    {
        ProvidedInvariants = invariants.Select(invariant =>
        {
            return new InvariantNode() 
            { 
                Graph = this, 
                Value = invariant 
            };
        });
    }



    /// <summary>
    /// Fetches an <see cref="ICalculationNode{K}"/> of type <typeparamref name="TNode"/> from the graph. Will throw if <see cref="Resolve(object[])"/> has not been called
    /// </summary>
    /// <typeparam name="TNode">The type of node to fetch in the graph</typeparam>
    /// <returns>The node instance found in the graph</returns>
    /// <exception cref="NodeOutsideOfGraphException{T, TGraph, TNode}"></exception>
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

    IEnumerable<ICalculationNode> Sort() => new KahnSorter().KahnSort(this);
    
    #endregion
}
