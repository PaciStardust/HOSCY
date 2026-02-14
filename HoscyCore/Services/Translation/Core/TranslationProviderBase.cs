using System.Diagnostics.CodeAnalysis;
using HoscyCore.Services.DependencyCore;

namespace HoscyCore.Services.Translation.Core;

public abstract class TranslationProviderBase : StartStopSubmoduleBase, ITranslationProvider
{
    public abstract string Metadata { get; }
    public abstract TranslationResult TryTranslate(string input, out string? output);
}