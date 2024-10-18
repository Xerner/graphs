using Graphs.Models;

namespace Graphs.Interfaces;

/// <summary>
/// A <see cref="ICalculationNode"/> that holds a constant value. Does not perform any calculations.
/// </summary>
public interface IInvariantNode : ICalculationNode
{
    /// <summary>
    /// The constant value of the node
    /// </summary>
    new object? Value { get; }

    /// <summary>
    /// Always returns true
    /// </summary>
    new bool HasCalculated { get; }

    /// <summary>
    /// Sets the value of the node
    /// </summary>
    void SetValue(object value);

    /// <summary>
    /// Simply returns <see cref="Value"/>
    /// </summary>
    new object? Calculate();

    /// <summary>
    /// Simply returns <see cref="Task{}"/> with the result of the Task being <see cref="Value"/>
    /// </summary>
    new Task<object?> CalculateAsync();
}

/// <inheritdoc cref="InvariantNode"/>
/// <typeparam name="TNodeValue">The type of value that it is expected to hold</typeparam>
public interface IInvariantNode<TNodeValue> : IInvariantNode, ICalculationNode<TNodeValue>
{
    /// <inheritdoc cref="IInvariantNode.Value"/>
    new TNodeValue? Value { get; }
    /// <inheritdoc cref="IInvariantNode.Calculate"/>
    new TNodeValue? Calculate() => Calculate();
    /// <inheritdoc cref="IInvariantNode.CalculateAsync"/>
    new Task<TNodeValue?> CalculateAsync();
}
