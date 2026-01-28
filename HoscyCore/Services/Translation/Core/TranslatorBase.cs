using System.Diagnostics.CodeAnalysis;
using HoscyCore.Services.DependencyCore;

namespace HoscyCore.Services.Translation.Core;

public abstract class TranslatorBase : StartStopSubmoduleBase, ITranslator
{
    public abstract string Metadata { get; }
    public abstract bool TryTranslate(string input, [NotNullWhen(true)] out string? output);
}