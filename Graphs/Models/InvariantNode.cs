using Graphs.Interfaces;

namespace Graphs.Models;

/// <summary>
/// A <see cref="CalculationNode"/> that holds a constant value. Does not perform any calculations.
/// </summary>
internal class InvariantNode : CalculationNode
{
    /// <summary>
    /// The constant value of the node
    /// </summary>
    public override object? Value { get; protected set; }
    /// <summary>
    /// Always returns true
    /// </summary>
    public override bool HasCalculated { get; protected set; } = true;

    /// <summary>
    /// Sets the value of the node
    /// </summary>
    public void SetValue(object value) => Value = value;

    /// <summary>
    /// Simply returns <see cref="Value"/>
    /// </summary>
    public override object? Calculate()
    {
        return Value;
    }

    /// <summary>
    /// Simply returns <see cref="Task{object?}"/> with the result of the Task being <see cref="Value"/>
    /// </summary>
    public override Task<object?> CalculateAsync()
    {
        return Task.FromResult(Value);
    }
}

/// <inheritdoc cref="InvariantNode"/>
/// <typeparam name="TNodeValue">The type of value that it is expected to hold</typeparam>
internal class InvariantNode<TNodeValue> : InvariantNode, ICalculationNode<TNodeValue>
{
    /// <inheritdoc cref="InvariantNode.HasCalculated"/>
    public override bool HasCalculated { get; protected set; } = true;
    /// <inheritdoc cref="InvariantNode.Value"/>
    public new TNodeValue? Value { get => (TNodeValue?)base.Value; set => base.Value = value; }
    public void SetValue(TNodeValue value) => Value = value;
    /// <inheritdoc cref="InvariantNode.Calculate"/>
    public new virtual TNodeValue? Calculate() => Calculate();
    /// <inheritdoc cref="InvariantNode.CalculateAsync"/>
    public new virtual Task<TNodeValue?> CalculateAsync() => CalculateAsync();
}
