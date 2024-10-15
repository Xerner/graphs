using Graphs.Exceptions;

namespace Graphs.Interfaces;

/// <summary>
/// A directed tree graph that exists to calculate the value for the provided <typeparamref name="TRootNode"/>
/// </summary>
public interface ICalculationTree<TRootNode> : IGraph<ICalculationNode> where TRootNode : ICalculationNode
{
    /// <summary>
    /// The root node of the calculation tree. Will hold the value that the calculation tree exists to calculate
    /// </summary>
    TRootNode Root { get; }

    /// <summary>
    /// Attempts to resolve all of the calculation trees nodes. Returns the root node.
    /// </summary>
    /// <exception cref="NullReferenceException"></exception>
    /// <exception cref="MissingExternalGraphDependencyException{T,K}"></exception>
    /// <exception cref="InvalidTypeAddedToGraphException"></exception>
    TRootNode Calculate(params object[] invariants);

    /// <inheritdoc cref="Resolve(object[])"/>
    Task<TRootNode> CalculateAsync(params object[] invariants);

    /// <summary>
    /// Fetches an invariant in the calculation tree of type <typeparamref name="T"/>
    /// </summary>
    /// <exception cref="NodeOutsideOfGraphException{ICalculationTree{TRootNode}, T}"></exception>
    T GetInvariant<T>();

    /// <summary>
    /// Fetches all invariants/>
    /// </summary>
    /// <returns>An instance of type <typeparamref name="T"/></returns>
    /// <exception cref="NodeOutsideOfGraphException{ICalculationTree{TRootNode}, T}"></exception>
    IEnumerable<object> GetInvariants();

    /// <summary>
    /// Mainly for debugging purposes
    /// </summary>
    IEnumerable<object> GetUnnecessaryInvariants();

    /// <summary>
    /// Retrieves all the errors for all nodes in the graph recursively
    /// </summary>
    ISet<object> GetErrors();
}
