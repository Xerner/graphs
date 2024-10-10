using Graphs.Interfaces;

namespace Graphs.Models;

/// <inheritdoc cref="IInfoNode{T}"/>
internal class ExternalInfoNode<T> : IInfoNode<T>
{
    public T? Value { get; set; }
    public bool HasResolved { get; private set; } = true;

    public T? Calculate()
    {
        return Value;
    }

    public Task<T?> CalculateAsync()
    {
        return Task.FromResult(Value);
    }

    public T? Resolve()
    {
        return Calculate();
    }

    public Task<T?> ResolveAsync()
    {
        return CalculateAsync();
    }
}

