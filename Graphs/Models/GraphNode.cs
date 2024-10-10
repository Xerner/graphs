namespace Graphs.Models;

internal class GraphNode
{
    // TODO: when upgraded to .Net 8, put `required` into NodeTypes definition
    public Type? NodeType { get; set; } = null;
    public HashSet<GraphNode> Dependencies { get; set; } = [];
    public HashSet<GraphNode> Dependents { get; set; } = [];
    public HashSet<Type> ExternalDependencies { get; set; } = [];
    public int InDegree => GetInDegree();
    public int GetInDegree() => Dependencies is null ? 0 : Dependencies.Count;
}
