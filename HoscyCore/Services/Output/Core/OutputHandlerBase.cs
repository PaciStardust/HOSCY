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
    public abstract void HandleMessage(string contents);
    public abstract void HandleNotification(string contents, OutputNotificationPriority priority);
    public abstract void SetProcessingIndicator(bool isProcessing);
    #endregion
}