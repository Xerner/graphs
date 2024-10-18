using Graphs.Exceptions;
using Graphs.Interfaces;

namespace Graphs.Models;

/// <inheritdoc cref="CalculationNode"/>
public class CalculationNode<TNodeValue> : CalculationNode, ICalculationNode<TNodeValue>
{
    public new virtual TNodeValue? Value { get; protected set; }
    public new virtual TNodeValue? Calculate() => Calculate();
    public new virtual async Task<TNodeValue?> CalculateAsync() => await CalculateAsync();
}

/// <summary>
/// A graph node containing info intended for use with a <see cref="CalculationTree{T}"/>.
/// Its constructor parameters are assumed to be its dependencies. It is not aware of nodes that depend on it.
/// </summary>
public class CalculationNode : ICalculationNode
{
    /// <summary>
    /// The graph that this node is a part of
    /// </summary>
    public IGraph<ICalculationNode>? Graph { get; set; } = default;

    /// <inheritdoc cref="ICalculationNode.Value"/>
    public virtual object? Value { get; protected set; }

    /// <inheritdoc cref="ICalculationNode.HasCalculated"/>
    public virtual bool HasCalculated { get; protected set; } = false;

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
    /// Helper function to quickly calculate a node of type <typeparamref name="TNode"/>
    /// </summary>
    /// <typeparam name="TNode">The node to calculate</typeparam>
    /// <param name="dependencies">The nodes to calculates dependencies</param>
    /// <returns>The node with a resolved value, or errors</returns>
    public static TNode Calculate<TNode>(params object[] dependencies) where TNode : CalculationNode
    {
        var graph = new CalculationTree<TNode>();
        graph.Calculate(dependencies);
        return graph.Root;
    }
}

