using Graphs.Models;

namespace Graphs.Interfaces;

public interface IGraph
{
    T Get<T>() where T : InfoNode;
    IEnumerable<string> GetErrors();
    IEnumerable<Type> GetNodes();
    T GetExternalDependency<T>();
    IEnumerable<Type> GetUnnecessaryExternalDependencies();
}
