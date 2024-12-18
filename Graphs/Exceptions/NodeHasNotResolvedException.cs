﻿using Graphs.Interfaces;

namespace Graphs.Exceptions;

public class NodeHasNotResolvedException<TNodeValue> : Exception
{
    public NodeHasNotResolvedException(ICalculationNode<TNodeValue> node) 
        : base($"Cannot access value of '{node.GetType().FullName}' until Calculate() is called") { }
}
