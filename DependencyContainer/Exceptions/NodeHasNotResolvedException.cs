using Graphs.Models;

namespace Graphs.Exceptions;

public class NodeHasNotResolvedException : Exception
{
    public NodeHasNotResolvedException(InfoNode node) : base($"Cannot access value of '{node.GetType().FullName}' until Calculate() is called") { }
}
