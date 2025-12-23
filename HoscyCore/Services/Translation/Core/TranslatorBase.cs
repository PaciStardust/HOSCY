using System.Diagnostics.CodeAnalysis;
using HoscyCore.Services.DependencyCore;

namespace HoscyCore.Services.Translation.Core;

public abstract class TranslatorBase : StartStopSubmoduleBase<string>, ITranslator
{
    public abstract bool TryTranslate(string input, [NotNullWhen(true)] out string? output);
}