namespace HoscyCore.Services.Output.Core;

/// <summary>
/// Represents all current 
/// </summary>
/// <param name="exceptions"></param>
public class OutputHandlerListException(List<Exception> exceptions) : Exception
{
    public IReadOnlyList<Exception> HandlerExceptions { get; init; } = exceptions;

    public override string Message => string.Join(" /// ", HandlerExceptions.Select(x => $"{x.GetType().FullName} => {x.Message}"));
    public override string? StackTrace => string.Join("\n\n///\n\n", HandlerExceptions.Select(x => $"{x.GetType().FullName} => {x.StackTrace}")); 
}