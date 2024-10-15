namespace Graphs.Models;

/// <inheritdoc cref="CalculationNode{T}"/>
public abstract class Rule : Formula<bool>
{
    new public static T Resolve<T>(params object[] dependencies) where T : Rule
    {
        return CalculationNode<bool>.Calculate<T>(dependencies);
    }
}

/// <inheritdoc cref="CalculationNode{T}"/>
public abstract class RuleAsync : FormulaAsync<bool>
{
    new public static async Task<T> Resolve<T>(params object[] dependencies) where T : RuleAsync
    {
        return await InfoNodeAsync<bool>.Resolve<T>(dependencies);
    }
}
