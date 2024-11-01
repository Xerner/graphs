using Graphs.Extensions;
using Graphs.Interfaces;
using Graphs.Services;

namespace Graphs.Models;

/// <summary>
/// A graph node containing info intended for use with a <see cref="CalculationTree{T}"/>.
/// Its constructor parameters are assumed to be its dependencies. It is not aware of nodes that depend on it.
/// </summary>
public class CalculationNode : ICalculationNode
{
    /// <summary>
    /// The graph that this node is a part of
    /// </summary>
    public required IGraph<ICalculationNode> Graph { get; init; }
    IGraph<INode> INode.Graph => (IGraph<INode>)Graph;

    /// <inheritdoc cref="ICalculationNode.Value"/>
    public virtual object? Value { get; protected set; }

    /// <inheritdoc cref="ICalculationNode.HasCalculated"/>
    public virtual bool HasCalculated { get; protected set; } = false;


    /// <summary>
    /// Returns nodes in the graph that match the types of the constructor parameters of this types first constructor
    /// </summary>
    public IEnumerable<INode> GetDependencies()
    {
        var (_, dependencies) = GetType().GetTypesFromFirstConstructor();
        return Graph.GetAll().Where(node => dependencies.Contains(node.GetType()));
    }

    public static IEnumerable<Type> GetDependenciesTypes(Type calculationNodeType)
    {
        var (_, dependencies) = calculationNodeType.GetTypesFromFirstConstructor();
        return dependencies;
    }

    /// <summary>
    /// Returns nodes in the graph that depend on this node
    /// </summary>
    public IEnumerable<INode> GetDependents()
    {
        return Graph.GetAll().Where(node => node.GetDependencies().Contains(this));
    }

    public int GetInDegree() => GetDependencies() is null ? 0 : GetDependencies().Count();

    public bool IsInvariant() => GetInDegree() == 0;

    /// <inheritdoc cref="ICalculationNode.GetErrors"/>
    protected HashSet<string> Errors = [];

    /// <inheritdoc cref="Errors"/>
    public IReadOnlySet<object> GetErrors() => (IReadOnlySet<object>)Errors;

    /// <summary>
    /// Clears all errors on the node
    /// </summary>
    public void ResetErrors() => Errors.Clear();

    /// <inheritdoc cref="ICalculationNode.Calculate"/>
    public virtual object? Calculate()
    {
        ResetErrors();
        Value = Calculate();
        HasCalculated = true;
        return Value;
    }

    /// <inheritdoc cref = "Calculate" />
    public virtual async Task<object?> CalculateAsync()
    {
        ResetErrors();
        Value = await CalculateAsync();
        HasCalculated = true;
        return Value;
    }

    /// <summary>
    /// Helper function to quickly calculate a node of type <typeparamref name="TRootNode"/>
    /// </summary>
    /// <typeparam name="TRootNode">The node to calculate</typeparam>
    /// <param name="dependencies">The nodes to calculates dependencies</param>
    /// <returns>The node with a resolved value, or errors</returns>
    public static TRootNode Calculate<TRootNode>(params object[] dependencies) where TRootNode : CalculationNode
    {
        var calcService = new CalculationTreeService();
        var rootNode = calcService.Calculate<TRootNode>(dependencies);
        return rootNode;
    }
}

/// <inheritdoc cref="CalculationNode"/>
public class CalculationNode<TNodeValue> : CalculationNode, ICalculationNode<TNodeValue>
{
    public new virtual TNodeValue? Value { get; protected set; }
    public new virtual TNodeValue? Calculate() => Calculate();
    public new virtual async Task<TNodeValue?> CalculateAsync() => await CalculateAsync();
}
