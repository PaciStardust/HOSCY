using System.Diagnostics.CodeAnalysis;
using Hoscy.Services.DependencyCore;

namespace Hoscy.Services.Translation.Core;

public abstract class TranslatorBase : StartStopSubmoduleBase<string>, ITranslator
{
    public abstract bool TryTranslate(string input, [NotNullWhen(true)] out string? output);
}