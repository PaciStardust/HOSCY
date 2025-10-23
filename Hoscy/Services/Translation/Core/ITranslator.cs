using System.Diagnostics.CodeAnalysis;
using Hoscy.Services.DependencyCore;

namespace Hoscy.Services.Translation.Core;

public interface ITranslator : IStartStopService
{
    public string GetName();
    public bool TryTranslate(string input, [NotNullWhen(true)] out string? output);
}