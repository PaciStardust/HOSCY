using HoscyCore.Services.DependencyCore;

namespace HoscyCore.Services.Output.Core;

public interface IOutputProcessor : IStartStopSubmodule<OutputProcessorInfo>
{
    #region Info
    public TranslationOutputMode GetTranslationOutputMode();
    #endregion

    #region Functionality
    public void ProcessMessage(string contents);
    public void ProcessNotification(string contents, OutputNotificationPriority priority);
    public void Clear();
    public void SetProcessingIndicator(bool isProcessing);
    #endregion
}