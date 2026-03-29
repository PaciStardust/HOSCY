/// <summary>
/// Represents multiple exceptions
/// </summary>
/// <param name="exceptions"></param>
public class CombinedException(List<Exception> exceptions) : Exception
{
    public IReadOnlyList<Exception> Exceptions { get; init; } = exceptions;

    public override string Message => string.Join(" /// ", Exceptions.Select(x => $"{x.GetType().FullName} => {x.Message}"));
    public override string? StackTrace => string.Join("\n\n///\n\n", Exceptions.Select(x => $"{x.GetType().FullName} => {x.StackTrace}")); 
}