using System;
using System.Collections.Generic;
using System.Linq;

namespace Hoscy.Services.Output.Core;

/// <summary>
/// Represents all current 
/// </summary>
/// <param name="exceptions"></param>
public class OutputProcessorListException(List<Exception> exceptions) : Exception
{
    public IReadOnlyList<Exception> ProcessorExceptions { get; init; } = exceptions;

    public override string Message => string.Join(" /// ", ProcessorExceptions.Select(x => $"{x.GetType().FullName} => {x.Message}"));
    public override string? StackTrace => string.Join("\n\n///\n\n", ProcessorExceptions.Select(x => $"{x.GetType().FullName} => {x.StackTrace}")); 
}