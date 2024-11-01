using Graphs.Interfaces;

namespace Graphs.Models;

/// <inheritdoc cref="IInvariantNode"/>
public class InvariantNode : IInvariantNode
{
    public required IGraph<ICalculationNode> Graph { get; init; }
    IGraph<INode> INode.Graph => (IGraph<INode>)Graph;

    public object? Value { get; init; }

    public bool HasCalculated { get; protected set; } = true;


    public object? Calculate()
    {
        return Value;
    }

    public Task<object?> CalculateAsync()
    {
        return Task.FromResult(Value);
    }

    public IEnumerable<INode> GetDependencies()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<INode> GetDependents()
    {
        throw new NotImplementedException();
    }

    public IReadOnlySet<object> GetErrors() => new HashSet<object>();

    public void ResetErrors() { }
}

/// <inheritdoc cref="InvariantNode"/>
/// <typeparam name="TNodeValue">The type of value that it is expected to hold</typeparam>
public class InvariantNode<TNodeValue> : InvariantNode, IInvariantNode<TNodeValue>
{
    public new TNodeValue? Value { get => (TNodeValue?)base.Value; init => base.Value = value; }

    /// <inheritdoc cref="IInvariantNode.Calculate"/>
    public new virtual TNodeValue? Calculate() => Calculate();

    /// <inheritdoc cref="IInvariantNode.CalculateAsync"/>
    public new virtual Task<TNodeValue?> CalculateAsync() => CalculateAsync();
}
