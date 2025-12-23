using System.Diagnostics.CodeAnalysis;
using HoscyCore.Services.DependencyCore;

namespace HoscyCore.Services.Translation.Core;

public interface ITranslator : IStartStopSubmodule<string>
{
    public bool TryTranslate(string input, [NotNullWhen(true)] out string? output);
}