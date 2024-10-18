using Graphs.Interfaces;
using Graphs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphs.Services;

public class CalculationTreeService
{
    /// <summary>
    /// Resolves the graph by sorting it topologically and resolving nodes one by one. 
    /// Returns the value of the given entry node in the <see cref="ICalculationTree{TEntryNode}"/> definition.
    /// <br/><br/>
    /// For each node in the graph, assigns the value returned form <see cref="ICalculationNode.Calculate"/> to <see cref="ICalculationNode.Value"/>
    /// <br/><br/>
    /// Calls <see cref="ICalculationNode{}.ResetErrors"/> on each node in the graph before calling <see cref="ICalculationNode{}.Calculate"/>
    /// </summary>
    /// <exception cref="NullReferenceException"></exception>
    /// <exception cref="MissingInvariantException{}"></exception>
    /// <exception cref="InvalidTypeAddedToGraphException"></exception>
    public TRootNode Calculate<TRootNode>(params object[] invariants) where TRootNode : ICalculationNode
    {
        var calculationTree = new CalculationTree<TRootNode>();
        var sortedGraphNodes = SetupNodeResolution(invariants);
        foreach (var graphNode in sortedGraphNodes)
        {
            // TODO: pull this logic out into its own function that can also be used in CalculateAsync
            // to consolidate duplicate logic
            var isExternalDependency = Invariants.Contains(graphNode.NodeType);
            if (isExternalDependency)
            {
                continue;
            }
            var infoNode = Nodes[graphNode.NodeType];
            infoNode.Graph = this;
            infoNode.Calculate();
        }
        return Root;
    }

    /// <inheritdoc cref="Calculate(object[])"/>
    public async Task<TRootNode> CalculateAsync(params object[] invariants)
    {
        var sortedGraphNodes = SetupNodeResolution(invariants);
        foreach (var graphNode in sortedGraphNodes)
        {
            var isExternalDependency = Invariants.Contains(graphNode.NodeType);
            if (isExternalDependency)
            {
                continue;
            }
            var infoNode = Nodes[graphNode.NodeType];
            infoNode.Graph = this;
            await infoNode.CalculateAsync();
        }
        return Root;
    }
}
