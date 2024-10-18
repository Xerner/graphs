using Graphs.Exceptions;
using Graphs.Models;

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
    /// <exception cref="MissingInvariantException{T,K}"></exception>
    /// <exception cref="InvalidTypeAddedToGraphException"></exception>
    TRootNode Calculate(params object[] invariants);

    /// <inheritdoc cref="Resolve(object[])"/>
    Task<TRootNode> CalculateAsync(params object[] invariants);

    /// <summary>
    /// Fetches an invariant in the calculation tree of type <typeparamref name="T"/>
    /// </summary>
    /// <exception cref="NodeOutsideOfGraphException{T, ICalculationTree{TRootNode}, ICalculationNode}"></exception>
    IInvariantNode? GetInvariant<T>();

    /// <summary>
    /// Fetches all invariant nodes
    /// </summary>
    /// <returns>A list of all invariants in the <see cref="ICalculationTree{TRootNode}"/></returns>
    /// <exception cref="NodeOutsideOfGraphException{T, ICalculationTree{TRootNode}, ICalculationNode}"></exception>
    IEnumerable<IInvariantNode> GetInvariants();

    /// <summary>
    /// Fetches all invariant nodes that do not have another node that depends on them. Mainly for debugging purposes
    /// </summary>
    IEnumerable<IInvariantNode> GetUnnecessaryInvariants();

    /// <summary>
    /// Retrieves all the errors for all nodes in the graph recursively
    /// </summary>
    IReadOnlySet<object> GetErrors();
}
