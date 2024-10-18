using Graphs.Interfaces;

namespace Graphs.Models;

/// <inheritdoc cref="IInvariantNode"/>
public class InvariantNode : CalculationNode, IInvariantNode
{
    /// <inheritdoc cref="IInvariantNode.Value"/>
    public override object? Value { get; protected set; }

    /// <inheritdoc cref="IInvariantNode.HasCalculated"/>
    public override bool HasCalculated { get; protected set; } = true;

    /// <inheritdoc cref="IInvariantNode.SetValue"/>
    public void SetValue(object value) => Value = value;

    /// <inheritdoc cref="IInvariantNode.Calculate"/>
    public override object? Calculate()
    {
        return Value;
    }

    /// <inheritdoc cref="IInvariantNode.CalculateAsync"/>
    public override Task<object?> CalculateAsync()
    {
        return Task.FromResult(Value);
    }
}

/// <inheritdoc cref="InvariantNode"/>
/// <typeparam name="TNodeValue">The type of value that it is expected to hold</typeparam>
public class InvariantNode<TNodeValue> : InvariantNode, IInvariantNode<TNodeValue>
{
    /// <inheritdoc cref="IInvariantNode.HasCalculated"/>
    public override bool HasCalculated { get; protected set; } = true;

    /// <inheritdoc cref="IInvariantNode.Value"/>
    public new TNodeValue? Value { get => (TNodeValue?)base.Value; set => base.Value = value; }

    /// <inheritdoc cref="IInvariantNode.SetValue"/>
    public void SetValue(TNodeValue value) => Value = value;

    /// <inheritdoc cref="IInvariantNode.Calculate"/>
    public new virtual TNodeValue? Calculate() => Calculate();

    /// <inheritdoc cref="IInvariantNode.CalculateAsync"/>
    public new virtual Task<TNodeValue?> CalculateAsync() => CalculateAsync();
}
