using Graphs.Exceptions;

namespace Graphs.Interfaces;

public interface ICalculationTree<TRootNode, TRootNodeValue> : IGraph<IInfoNode<object>> where TRootNode : IInfoNode<TRootNodeValue>
{
    TRootNode Root { get; }

    TRootNodeValue? Value { get; }

    /// <summary>
    /// Resolves the graph by sorting it topologically and resolving nodes one by one. 
    /// Returns the value of the given entry node in the <see cref="ICalculationTree{TEntryNode, TEntryNodeValue}"/> definition.
    /// <br/>
    /// <br/>
    /// Assigns the value returned form <see cref="IInfoNode.ResolveObject"/> to <see cref="IInfoNode.ObjectValue"/>, and
    /// <see cref="IInfoNode{T}.Resolve"/> to <see cref="IInfoNode{T}.Value"/>
    /// <br/>
    /// <br/>
    /// Calls <see cref="IInfoNode.ResetErrors"/> on each node in the graph before calling <see cref="IInfoNode.ResolveObject"/>
    /// </summary>
    /// <exception cref="NullReferenceException"></exception>
    /// <exception cref="MissingExternalGraphDependencyException{T,K}"></exception>
    /// <exception cref="InvalidTypeAddedToGraphException"></exception>
    TRootNodeValue? Resolve(params object[] externalGraphDependencies);

    /// <inheritdoc cref="Resolve(object[])"/>
    Task<TRootNodeValue?> ResolveAsync(params object[] externalGraphDependencies);

    /// <summary>
    /// Fetches an <see cref="IInfoNode{K}"/> of type <typeparamref name="TNode"/> from the graph. Will throw if <see cref="Resolve(object[])"/> has not been called
    /// </summary>
    /// <typeparam name="TNode">The type of node to fetch in the graph</typeparam>
    /// <returns>The node instance found in the graph</returns>
    /// <exception cref="NodeOutsideOfGraphException{TGraph, TNode}"></exception>
    TNode Get<TNode, TNodeValue>() where TNode : IInfoNode<TNodeValue>;

    /// <summary>
    /// Fetches an external dependency instance that was provided to <see cref="Resolve(object[])"/>. 
    /// This will always fail if called before the graph is resolved
    /// </summary>
    /// <returns>An instance of type <typeparamref name="T"/></returns>
    /// <exception cref="NodeOutsideOfGraphException{ICalculationTree{TRootNode, TRootNodeValue}, T}"></exception>
    T GetExternalDependency<T>();

    /// <summary>
    /// Fetches all the types of nodes in the graph
    /// </summary>
    IEnumerable<TNode> GetNodes<TNode>() where TNode : IInfoNode<object>;

    /// <summary>
    /// Retrieves all the errors for each node in the graph recursively
    /// </summary>
    IEnumerable<string> GetErrors();

    /// <summary>
    /// Mainly for debugging purposes. <see cref="Resolve(object[])"/> must be called before this can output anything
    /// </summary>
    IEnumerable<Type> GetUnnecessaryExternalDependencies();
}
