using System.Text;
using Graphs.Interfaces;
using Graphs.Models;

namespace Graphs.Exceptions;

internal class MissingExternalGraphDependencyException<TNode, TNodeValue> : Exception where TNode : ICalculationNode<TNodeValue>
{
    public IEnumerable<GraphNode> MissingTypes;

    public MissingExternalGraphDependencyException(IEnumerable<GraphNode> missingTypes) : base(Format(missingTypes))
    {
        MissingTypes = missingTypes;
    }

    public static string Format(IEnumerable<GraphNode> missingTypes)
    {
        var entryNodeType = typeof(TNode);
        var missingTypesDict = new Dictionary<Type, List<GraphNode>>();
        foreach (var missingType in missingTypes)
        {
            foreach (var dependent in missingType.Dependents)
            {
                if (!missingTypesDict.TryGetValue(missingType.NodeType, out List<GraphNode>? dependents))
                {
                    dependents = new();
                    missingTypesDict.Add(missingType.NodeType, dependents);
                }
                dependents.Add(dependent);
            }
        }
        var strBuilder = new StringBuilder();
        foreach (var typeAndList in missingTypesDict)
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
