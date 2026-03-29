namespace HoscyCore.Services.Output.Core;

#region For Input
/// <summary>
/// Determine how a message should be processed
/// </summary>
[Flags]
public enum OutputSettingsFlags
{
    None = 0,
    AllowTextOutput = 1,
    AllowAudioOutput = 2,
    AllowOtherOutput = 4,
    DoTranslate = 8,
    DoPreprocessPartial = 16,
    DoPreprocessFull = 32,
    DoPreprocessAll = DoPreprocessPartial | DoPreprocessFull,
    AllowAllOutputs = AllowTextOutput | AllowAudioOutput | AllowOtherOutput,
    AllowAllTransforms = DoTranslate | DoPreprocessAll
}

/// <summary>
/// Determines priority of notifications
/// </summary>
public enum OutputNotificationPriority
{
    None,
    Minimal,
    Low,
    Medium,
    High,
    Important,
    Critical
}
#endregion

#region For Handlers
/// <summary>
/// Format the output should be in if translation is enabled
/// </summary>
public enum OutputTranslationFormat
{
    Untranslated,
    Translation,
    Both
}

/// <summary>
/// Flags to determine which media output will be in
/// </summary>
[Flags]
public enum OutputsAsMediaFlags
{
    None = 0,
    OutputsAsText = 1,
    OutputsAsAudio = 2,
    OutputsAsOther = 4
}
#endregion

#region For Preprocessors
/// <summary>
/// Determines in which order preprocessors will act
/// </summary>
public enum OutputPreprocessorHandlingStage
{
    /// <summary>
    /// Should be used BEFORE any changes are made
    /// </summary>
    Initial,

    /// <summary>
    /// Alters the output slightly (Step 1 of 3)
    /// </summary>
    AlterEarly,

    /// <summary>
    /// Alters the output slightly (Step 2 of 3)
    /// </summary>
    Alter,

    /// <summary>
    /// Alters the output slightly (Step 3 of 3)
    /// </summary>
    AlterLate,

    /// <summary>
    /// Replaces the output fully (Step 1 of 3)
    /// </summary>
    ReplaceEarly,

    /// <summary>
    /// Replaces the output fully (Step 2 of 3)
    /// </summary>
    Replace,

    /// <summary>
    /// Replaces the output fully (Step 3 of 3)
    /// </summary>
    ReplaceLate,

    /// <summary>
    /// Should be used AFTER all changes are made
    /// </summary>
    Final
}
#endregion