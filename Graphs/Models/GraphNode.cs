namespace Graphs.Models;

internal class GraphNode
{
    public required Type NodeType { get; init; }
    public HashSet<GraphNode> Dependencies { get; set; } = [];
    public HashSet<GraphNode> Dependents { get; set; } = [];
    public HashSet<Type> ExternalDependencies { get; set; } = [];
    public int InDegree => GetInDegree();
    public int GetInDegree() => Dependencies is null ? 0 : Dependencies.Count;
}
