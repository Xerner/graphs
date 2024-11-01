namespace Graphs.Interfaces;

public interface INode
{
    /// <summary>
    /// The graph that the node is a part of
    /// </summary>
    IGraph<INode> Graph { get; }
    IEnumerable<INode> GetDependencies();
    IEnumerable<INode> GetDependents();
}
