namespace Graphs.Interfaces;

public interface IInfoNode<T>
{
    public T? Value { get; }
    bool HasResolved { get; }
    public T? Calculate();
    public Task<T?> CalculateAsync();
    public T? Resolve();
    public Task<T?> ResolveAsync();
}
