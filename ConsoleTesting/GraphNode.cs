using Graphs.Interfaces;

namespace Graphs.Models;

internal class GraphNode : INode
{
    public required Type Type { get; init; }
    public List<INode> Dependencies { get; set; } = [];
    public List<INode> Dependents { get; set; } = [];
    public IEnumerable<INode> GetDependencies() => Dependencies;
    public IEnumerable<INode> GetDependents() => Dependents;
    
}
