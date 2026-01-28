using System.Diagnostics.CodeAnalysis;
using HoscyCore.Services.DependencyCore;

namespace HoscyCore.Services.Translation.Core;

public interface ITranslator : IStartStopSubmodule
{
    public string Metadata { get; } //todo: fix
    public bool TryTranslate(string input, [NotNullWhen(true)] out string? output);
}