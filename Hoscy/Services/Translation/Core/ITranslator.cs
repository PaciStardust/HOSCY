using System.Diagnostics.CodeAnalysis;
using Hoscy.Services.DependencyCore;

namespace Hoscy.Services.Translation.Core;

public interface ITranslator : IStartStopSubmodule<string>
{
    public bool TryTranslate(string input, [NotNullWhen(true)] out string? output);
}