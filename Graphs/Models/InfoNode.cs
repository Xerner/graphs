using Graphs.Exceptions;
using Graphs.Interfaces;

namespace Graphs.Models;

/// <summary>
/// A graph node containing info intended for use with a <see cref="CalculationTree{T, K}"/>.
/// It's constructor parameters are assumed to be its dependencies. It is not aware of nodes that depend on it.
/// </summary>
public abstract class InfoNode<TNodeValue> : IInfoNode<TNodeValue>
{
    public IGraph? Graph { get; internal set; } = default;

    protected TNodeValue? value = default;

    public bool HasResolved { get; set; } = false;

    public virtual TNodeValue? Value
    {
        get => HasResolved ? value : throw new NodeHasNotResolvedException<TNodeValue>(this);
        set => this.@value = value;
    }

    protected HashSet<string> Errors = [];

    public IReadOnlySet<string> GetErrors() => Errors;

    public void ResetErrors() => Errors.Clear();

    public TNodeValue? Resolve()
    {
        ResetErrors();
        Value = Calculate();
        HasResolved = true;
        return Value;
    }

    public async Task<TNodeValue?> ResolveAsync()
    {
        ResetErrors();
        Value = await CalculateAsync();
        HasResolved = true;
        return Value;
    }

    public abstract TNodeValue? Calculate();

    public virtual Task<TNodeValue?> CalculateAsync() => Task.FromResult(Calculate());

    // TODO: re-type this class so that 'value' can return null to signify it has not been resolved yet
    public static K Resolve<K>(params object[] dependencies) where K : InfoNode<TNodeValue>
    {
        var graph = new CalculationTree<K, TNodeValue>();
        graph.Resolve(dependencies);
        return graph.Root;
    }
}
