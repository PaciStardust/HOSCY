using HoscyCore.Services.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Translation.Core;

public abstract class TranslationModuleBase(ILogger logger) : StartStopModuleBase(logger), ITranslationModule
{
    public abstract Res<string> Translate(string input);
}