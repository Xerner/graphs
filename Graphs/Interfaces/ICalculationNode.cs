namespace Graphs.Interfaces;

/// <summary>
/// A node intended for using in calculations in graphs. It is assumed that its constructor parameters are its dependencies.
/// </summary>
public interface ICalculationNode : INode
{
    /// <summary>
    /// The calculated value of the node, or null if not yet calculated
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// Calculates the nodes value and sets it to <see cref="Value"/>
    /// </summary>
    /// <returns>The nodes value</returns>
    object? Calculate();

    /// <inheritdoc cref="Calculate"/>
    Task<object?> CalculateAsync();

    /// <summary>
    /// Exists to differentiate whether or not <see cref="Value"/> is truly null or has not been calculated
    /// </summary>
    bool HasCalculated { get; }

    /// <summary>
    /// Retrieves errors encountered when trying to calculate the node. This can also return errors found in the nodes dependencies
    /// </summary>
    IReadOnlySet<object> GetErrors();

    /// <summary>
    /// Resets the errors that have been encountered when trying to calculate the node
    /// </summary>
    void ResetErrors();
}

/// <inheritdoc cref="ICalculationNode"/>
/// <typeparam name="TValue">The type of value the node is expected to calculate and hold</typeparam>
public interface ICalculationNode<TValue> : ICalculationNode
{
    /// <inheritdoc cref = "ICalculationNode.Value" />
    new TValue? Value { get; }
    object? ICalculationNode.Value { get => Value; }

    /// <summary>
    /// Calculates the nodes value and sets it to <see cref="ICalculationNode{}.Value"/>
    /// </summary>
    /// <returns>The nodes value</returns>
    new TValue? Calculate();
    object? ICalculationNode.Calculate() => Calculate();

    /// <inheritdoc cref="Calculate"/>
    new Task<TValue?> CalculateAsync();
    Task<object?> ICalculationNode.CalculateAsync() => Task.FromResult((object?)CalculateAsync());
}
