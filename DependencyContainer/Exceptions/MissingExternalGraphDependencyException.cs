using Graphs.Models;
using System.Text;

namespace Graphs.Exceptions;

internal class MissingExternalGraphDependencyException<TEntryNode> : Exception where TEntryNode : InfoNode
{
    public IEnumerable<GraphNode> MissingTypes;

    public MissingExternalGraphDependencyException(IEnumerable<GraphNode> missingTypes) : base(Format(missingTypes))
    {
        MissingTypes = missingTypes;
    }

    public static string Format(IEnumerable<GraphNode> missingTypes)
    {
        var entryNodeType = typeof(TEntryNode);
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
        return $"The graph for '{entryNodeType.FullName}' is missing the following external dependencies\n\n{strBuilder}\n";
    }
}
