namespace Graphs.Models;

/// <inheritdoc cref="CalculationNode{T}"/>
public abstract class Formula<T> : CalculationNode<T>
{
}

/// <inheritdoc cref="CalculationNode{T}"/>
public abstract class FormulaAsync<T> : InfoNodeAsync<T>
{
}
