using System;
using System.Collections.Generic;

namespace Hoscy.Services.Output.Core;

/// <summary>
/// Represents all current 
/// </summary>
/// <param name="exceptions"></param>
public class OutputProcessorListException(List<Exception> exceptions) : Exception
{
    public IReadOnlyList<Exception> ProcessorExceptions { get; init; } = exceptions;
}