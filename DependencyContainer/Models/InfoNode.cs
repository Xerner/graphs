using Graphs.Exceptions;
using Graphs.Interfaces;

namespace Graphs.Models;

// TODO: can this just be refactored into InfoNode<T> and all instances of it be replaced with InfoNode<object>?
//       The struggle lies to get stuff like CalculationGraph.Nodes to compile. It cannot convert InfoNode<object> to InfoNode<T>.
/// <summary>
/// A node containing info produced from a <see cref="CalculationGraph{T, V}"/>
/// </summary>
public abstract class InfoNode
{
    internal bool HasResolved = false;
    public IGraph? Graph { get; internal set; } = default;
    protected object? objectValue = null;
    public virtual object? ObjectValue
    {
        get => HasResolved ? objectValue : throw new NodeHasNotResolvedException(this);
        set => objectValue = value;
    }
    protected HashSet<string> Errors = new();
    public IReadOnlySet<string> GetErrors() => Errors;
    public void ResetErrors() => Errors.Clear();
    public object? ResolveObject()
    {
        ResetErrors();
        ObjectValue = CalculateObject();
        HasResolved = true;
        return ObjectValue;
    }
    public async Task<object?> ResolveObjectAsync()
    {
        ResetErrors();
        ObjectValue = await CalculateObjectAsync();
        HasResolved = true;
        return ObjectValue;
    }
    public abstract object? CalculateObject();
    public virtual Task<object?> CalculateObjectAsync() => Task.FromResult(CalculateObject());
}

/// <inheritdoc cref="InfoNode"/>
public abstract class InfoNode<T> : InfoNode
{
    public override object? ObjectValue { get => Value; set => Value = (T?)value; }
    // TODO: re-type this class so that 'value' can return null to signify it has not been resolved yet
    protected T? value = default;
    public virtual T? Value
    {
        get => HasResolved ? value : throw new NodeHasNotResolvedException(this);
        set => this.value = value;
    }
    public override object? CalculateObject() => Calculate();
    public override async Task<object?> CalculateObjectAsync() => await CalculateAsync();
    public InfoNode<T> Resolve()
    {
        ResetErrors();
        Value = Calculate();
        HasResolved = true;
        return this;
    }
    public async Task<InfoNode<T>> ResolveAsync()
    {
        ResetErrors();
        Value = await CalculateAsync();
        HasResolved = true;
        return this;
    }
    public abstract T? Calculate();
    public virtual Task<T?> CalculateAsync() => Task.FromResult(Calculate());
    public static K Resolve<K>(params object[] dependencies) where K : InfoNode<T>
    {
        var graph = new CalculationTree<K, T>();
        graph.Resolve(dependencies);
        return graph.Root;
    }
}

/// <inheritdoc cref="InfoNode"/>
public abstract class InfoNodeAsync : InfoNode
{
    public override object? CalculateObject()
    {
        var task = CalculateObjectAsync();
        task.Wait();
        return task.Result;
    }
    public override abstract Task<object?> CalculateObjectAsync();
}

/// <inheritdoc cref="InfoNode"/>
public abstract class InfoNodeAsync<T> : InfoNode<T>
{
    public override T? Calculate()
    {
        var task = CalculateAsync();
        task.Wait();
        return task.Result;
    }

    public override abstract Task<T?> CalculateAsync();

    new public static async Task<K> Resolve<K>(params object[] dependencies) where K : InfoNodeAsync<T>
    {
        var graph = new CalculationTree<K, T>();
        await graph.ResolveAsync(dependencies);
        return graph.Root;
    }
}
