namespace Graphs.Exceptions;

public class InvalidTypeAddedToGraphException : Exception
{
    public InvalidTypeAddedToGraphException(Type providedType, Type expectedGraphType)
        : base($"Invalid type '{providedType.FullName}' provided to a graph. All graph types must inherit from the {expectedGraphType.FullName} interface") { }
}
