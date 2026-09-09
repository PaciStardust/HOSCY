using HoscyCore.Services.Core;
using Serilog;

namespace HoscyCore.Services.Output.Core;

public abstract class OutputHandlerBase(ILogger logger) : StartStopModuleBase(logger), IOutputHandler
{
    #region Info
    public abstract string Name { get; }
    public abstract OutputsAsMediaFlags OutputTypeFlags { get; }
    public abstract OutputTranslationFormat GetTranslationOutputMode();
    #endregion

    #region Functionality
    public abstract void Clear();
    public abstract Task HandleMessage(string contents, string source);
    public abstract Task HandleNotification(string contents, string source, OutputNotificationPriority priority);
    public abstract void SetProcessingIndicator(bool isProcessing);
    #endregion
}