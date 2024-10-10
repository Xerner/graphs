namespace Graphs.Models;

/// <inheritdoc cref="InfoNode{T}"/>
internal class ExternalInfoNode : InfoNode
{
    public override object? ObjectValue
    {
        get => objectValue;
        set => objectValue = value;
    }
    public override object? CalculateObject()
    {
        return ObjectValue;
    }
}

/// <inheritdoc cref="InfoNode{T}"/>
internal class ExternalInfoNode<T> : InfoNode<T>
{
    public override T? Value
    {
        get => value;
        set => this.value = value;
    }
    public override T? Calculate()
    {
        return Value;
    }
}
