namespace Graphs.Models;

/// <inheritdoc cref="InfoNode{T}"/>
public abstract class Rule : Formula<bool>
{
    new public static T Resolve<T>(params object[] dependencies) where T : Rule
    {
        return InfoNode<bool>.Resolve<T>(dependencies);
    }
}

/// <inheritdoc cref="InfoNode{T}"/>
public abstract class RuleAsync : FormulaAsync<bool>
{
    new public static async Task<T> Resolve<T>(params object[] dependencies) where T : RuleAsync
    {
        return await InfoNodeAsync<bool>.Resolve<T>(dependencies);
    }
}
