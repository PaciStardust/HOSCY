namespace HoscyCore.Services.Translation.Core;

public enum TranslationResult
{
    /// <summary>
    /// Translation was successfull, output is available
    /// </summary>
    Succeeded,

    /// <summary>
    /// Translation failed, output unavailable
    /// </summary>
    Failed,

    /// <summary>
    /// Trannslation was skipped, output unavailable but original should be used
    /// </summary>
    UseOriginal
}