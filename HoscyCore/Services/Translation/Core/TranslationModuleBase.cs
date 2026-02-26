using HoscyCore.Services.Core;
using Serilog;

namespace HoscyCore.Services.Translation.Core;

public abstract class TranslationModuleBase(ILogger logger) : StartStopModuleBase(logger), ITranslationModule
{
    public abstract TranslationResult TryTranslate(string input, out string? output);
}