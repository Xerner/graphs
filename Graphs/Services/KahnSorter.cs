using Graphs.Interfaces;
using Graphs.Models;

namespace Graphs.Services;

public class KahnSorter
{
    readonly GraphHelpers graphHelpers = new();

    /// <summary>
    /// Returns a sorted list of the graphs nodes
    /// <br/><br/>
    /// <see href="https://www.geeksforgeeks.org/topological-sorting-indegree-based-solution"/> 
    /// </summary>
    /// <inheritdoc cref="KahnSort(IEnumerable{Type})"/>
    internal IEnumerable<GraphNode> KahnSort<TNode>(CalculationTree<TNode> graph) where TNode : ICalculationNode
        => KahnSort(graph.GraphNodes.Values.ToList());

    /// <summary>
    /// Sorts the nodes in-place in order of their in-degree
    /// <br/><br/>
    /// <see href="https://www.geeksforgeeks.org/topological-sorting-indegree-based-solution"/> 
    /// </summary>
    /// <param name="nodes">The node types to consider</param>
    /// <returns>The nodes, sorted by their in-degree. This prevents dependency conflicts when resolving nodes</returns>
    /// <exception cref="CycleInGraphException"></exception>
    public IEnumerable<Type> KahnSort(IEnumerable<Type> originGraphNodes, Type expectedInfoNodeType)
    {
        graphHelpers.DetectCycleInDirectedGraph(originGraphNodes);

        // Get nodes in a nicer data structure to sort
        var sortNodes = new List<GraphNode>();
        foreach (var graphNodeType in originGraphNodes)
        {
            sortNodes.Add(graphHelpers.GetGraphNodes(expectedInfoNodeType, graphNodeType));
        }
        var sortedNodes = KahnSort(sortNodes);
        return sortedNodes.Select(sortNode => sortNode.NodeType);
    }

    /// <inheritdoc cref="KahnSort(IEnumerable{Type})"/>
    internal List<GraphNode> KahnSort(List<GraphNode> nodes, bool descending = false)
    {
        var nodesWithInDegreeZero = GetAllNodesWithNoDependencies(nodes);
        if (nodesWithInDegreeZero.Count == nodes.Count)
        {
            return nodes;
        }

        // Sort the nodes topologically
        Dictionary<GraphNode, int> indegrees = new();
        InitializeInDegrees(indegrees, nodes);
        while (nodesWithInDegreeZero.Count > 0)
        {
            var node = nodesWithInDegreeZero.Dequeue();
            /* Does this really sort in-place?
             *            ⠀⡠⠞⠉⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠲⡑⢄⠀⠀⠀⠀⠀
             *⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⡼⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠙⢮⣣⡀⠀⠀⠀
             *⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⠞⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠁⠱⡀⠀⠀
             *⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⡞⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢣⠀⠀
             *⠀⠀⠀⠀⠀⠀⠀⠀⠀⡞⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⡆⠀
             *⠀⠀⠀⠀⠀⠀⠀⠀⡼⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⣠⡤⠤⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢸⣹⠀
             *⠀⠀⠀⠀⢀⣀⣀⣸⢧⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⠞⣩⡤⠶⠶⠶⠦⠤⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠘⡇⡇
             *⠀⠀⠀⣰⣫⡏⠳⣏⣿⠇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠚⠁⠀⠀⠀⠀⠀⠀⠙⢿⣿⣶⣄⡀⠀⠀⢀⡀⠀⠀⠀⠀⠀⡀⡅⡇
             *⠀⠀⢰⡇⣾⡇⠀⠙⣟⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⣠⣴⣶⠿⠛⠻⢿⣶⣤⣍⡙⢿⣿⣷⣤⣾⡇⣼⣆⣴⣷⣿⣿⡇⡇
             *⠀⠀⢸⡀⡿⠁⠀⡇⠈⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣸⣿⣿⣯⠴⢲⣶⣶⣶⣾⣿⣿⣿⣷⠹⣿⣿⠟⢰⣿⣿⣿⠿⣿⣿⣿⠁
             *⠀⠀⠈⡇⢷⣾⣿⡿⢱⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠉⠉⠹⣌⠳⣼⣿⣿⣿⣻⣿⣿⣿⣿⡇⠈⠁⢰⣿⣿⣿⣿⣶⣾⣿⣿⠀
             *⠀⠀⠀⣷⠘⠿⣿⡥⠏⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠈⠃⠌⠉⣿⣿⣿⣿⣿⣿⠟⠃⠀⢀⡿⣿⣿⣿⣿⣿⣿⣿⡞⠀
             *⠀⠀⠀⢸⡇⠀⠹⠗⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣸⣿⡿⠟⠉⠉⠀⠀⠀⠈⢃⣿⣿⣿⣿⣿⣿⡻⠀⠀
             *⠀⠀⠀⠈⢧⠀⠀⠏⣇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠙⠋⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⣿⣿⣿⣿⣿⣿⠁⠀⠀
             *⠀⠀⠀⠀⠈⢳⠶⠞⠃⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⣴⠆⠀⠀⠊⠁⠀⠀⠀⠀⠸⣿⣿⣿⣿⣿⣿⠀⠀⠀
             *⠀⠀⠀⠀⠀⢸⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠂⠀⣼⣿⣀⡰⠀⠀⣤⣄⠀⠀⠀⠀⢹⣿⣿⣿⣿⢻⠀⠀⠀
             *⠀⠀⠀⠀⠀⡟⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠉⠹⣿⠀⠀⠀⠀⠉⠀⠀⠀⠀⠀⠙⣿⣿⣿⡏⠀⠀⠀
             *⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢻⣄⢠⣤⣶⣤⣀⠀⢀⣶⣶⣶⣿⣿⠟⠀⠀⠀⠀
             *⠀⠀⠀⠀⠀⡇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⠖⠁⠀⠀⠀⠀⠀⠻⣿⣿⣥⣤⣯⣥⣾⣿⣿⣿⣿⠋⠀⠀⠀⠀⠀
             *⠀⠀⠀⠀⣰⠇⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⢀⡼⠁⠀⠀⠀⠠⠀⠀⠀⠀⠈⣿⣿⣼⣿⣿⣿⣿⣿⣿⠇⠀⠀⠀⠀⠀⠀
             *⠀⠀⠀⡰⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠊⠀⠀⠀⣠⣰⣄⡀⠀⢀⣀⣀⣛⣟⣿⣿⣿⣿⣿⣿⡿⠀⠀⠀⠀⠀⠀⠀
             *⠀⣠⠜⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣠⣼⠾⠛⠛⠻⠿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠃⠀⠀⠀⠀⠀⠀⠀
             *⠾⠁⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣼⡟⠀⠀⠀⠀⠠⣄⣉⣉⣻⣿⣿⣿⣿⣿⣿⡟⠧⢄⡀⠀⠀⠀⠀⠀⠀
             *⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠻⠅⠀⠀⠀⠀⠘⠉⠹⣿⣿⣿⣿⣿⣿⣿⣿⣧⡀⠀⠉⠓⠢⣄⠀⠀⠀
             *⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣉⣿⣿⣿⣿⣿⣿⣿⣿⣷⣻⡄⠀⠀⢀⡑⠢⠄
             */
            nodes.Remove(node);
            nodes.Add(node);
            if (node.Dependents is null)
            {
                continue;
            }
            foreach (var dependentNode in node.Dependents)
            {
                indegrees[dependentNode]--;
                if (indegrees[dependentNode] == 0)
                {
                    nodesWithInDegreeZero.Enqueue(dependentNode);
                }
            }
        }
        if (descending)
        {
            nodes.Reverse();
        }
        return nodes;
    }

    void InitializeInDegrees(Dictionary<GraphNode, int> indegrees, List<GraphNode> nodes)
    {
        foreach (var node in nodes)
        {
            indegrees.Add(node, node.GetInDegree());
        }
    }

    /// <summary>
    /// Gets all nodes with in-degree 0
    /// </summary>
    Queue<GraphNode> GetAllNodesWithNoDependencies(IEnumerable<GraphNode> nodes)
    {
        var nodesWithZeroInDegree = new Queue<GraphNode>();
        foreach (var node in nodes)
        {
            if (node.GetInDegree() == 0)
            {
                nodesWithZeroInDegree.Enqueue(node);
            }
        }
        return nodesWithZeroInDegree;
    }
}
