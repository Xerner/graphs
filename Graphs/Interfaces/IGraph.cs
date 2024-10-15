using Graphs.Exceptions;

namespace Graphs.Interfaces;

public interface IGraph<TNode>
{
    /// <summary>
    /// Get a node of type <typeparamref name="T"/> from the graph
    /// </summary>
    /// <exception cref="NodeOutsideOfGraphException{IGraph{TNode}, T}"></exception>
    T Get<T>() where T : TNode;

    /// <summary>
    /// Gets all the nodes present in the graph
    /// </summary>
    IEnumerable<TNode> GetAll();
}
