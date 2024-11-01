using Graphs.Interfaces;
using Graphs.Models;

namespace Graphs.Services;

public class KahnSorter
{
    readonly GraphService graphHelpers = new();

    /// <summary>
    /// Returns a sorted list of the graphs nodes
    /// <br/><br/>
    /// <see href="https://www.geeksforgeeks.org/topological-sorting-indegree-based-solution"/> 
    /// </summary>
    /// <inheritdoc cref="KahnSort(IEnumerable{Type})"/>
    internal IEnumerable<INode> KahnSort<TNode>(ICalculationTree<TNode> graph) where TNode : ICalculationNode
    {
        List<INode> nodes = graph.GetAll().Select(node => (INode)node).ToList();
        return KahnSort(nodes);
    }

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
        var sortNodes = new List<INode>();
        foreach (var graphNodeType in originGraphNodes)
        {
            sortNodes.Add(graphHelpers.GetGraphNodes(expectedInfoNodeType, graphNodeType));
        }
        var sortedNodes = KahnSort(sortNodes);
        return sortedNodes.Select(sortNode => sortNode.GetType());
    }

    /// <inheritdoc cref="KahnSort(IEnumerable{Type})"/>
    internal IEnumerable<INode> KahnSort(List<INode> nodes, bool descending = false)
    {
        var nodesWithInDegreeZero = GetAllNodesWithNoDependencies(nodes);
        if (nodesWithInDegreeZero.Count == nodes.Count)
        {
            return nodes;
        }

        // Sort the nodes topologically
        Dictionary<INode, int> indegrees = new();
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
            if (node.GetDependents() is null)
            {
                continue;
            }
            foreach (var dependentNode in node.GetDependents())
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

    void InitializeInDegrees(Dictionary<INode, int> indegrees, List<INode> nodes)
    {
        foreach (var node in nodes)
        {
            indegrees.Add(node, node.GetDependencies().Count());
        }
    }

    /// <summary>
    /// Gets all nodes with in-degree 0
    /// </summary>
    Queue<INode> GetAllNodesWithNoDependencies(IEnumerable<INode> nodes)
    {
        var nodesWithZeroInDegree = new Queue<INode>();
        foreach (var node in nodes)
        {
            if (node.GetDependencies().Any() == false)
            {
                nodesWithZeroInDegree.Enqueue(node);
            }
        }
        return nodesWithZeroInDegree;
    }
}
