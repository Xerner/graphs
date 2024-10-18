using System.Text;
using Graphs.Interfaces;
using Graphs.Models;

namespace Graphs.Exceptions;

internal class MissingInvariantException<TNode>(IEnumerable<GraphNode> nodes) 
    : Exception(Format(nodes)) 
    where TNode : IInvariantNode
{
    public IEnumerable<GraphNode> MissingTypes = nodes;

    public static string Format(IEnumerable<GraphNode> nodes)
    {
        var entryNodeType = typeof(TNode);
        var missingDepsDict = new Dictionary<GraphNode, List<GraphNode>>();
        foreach (var node in nodes)
        {
            foreach (var dependentNode in node.Dependents)
            {
                if (!missingDepsDict.TryGetValue(node, out List<GraphNode>? dependents))
                {
                    dependents = [];
                    missingDepsDict.Add(node, dependents);
                }
                dependents.Add(dependentNode);
            }
        }

        var strBuilder = new StringBuilder();
        foreach (var typeAndList in missingDepsDict)
        {
            var type = typeAndList.Key;
            strBuilder.Append(type);
            strBuilder.AppendLine(" is a dependency in:");
            var dependents = typeAndList.Value;
            foreach (var dependent in dependents)
            {
                strBuilder.Append('\t');
                if (dependent.NodeType is null)
                {
                    strBuilder.AppendLine("null");
                    continue;
                }
                strBuilder.AppendLine(dependent.NodeType.FullName);
            }
            strBuilder.AppendLine();
        }
        return $"The graph for '{entryNodeType.FullName}' is missing the following invariants\n\n{strBuilder}\n";
    }
}
