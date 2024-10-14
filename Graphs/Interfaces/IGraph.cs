namespace Graphs.Interfaces;

public interface IGraph<TNode>
{
    TNode Get();
    IEnumerable<TNode> GetNodes();
    T GetExternalDependency<T>();
    IEnumerable<object> GetUnnecessaryExternalDependencies();
}
