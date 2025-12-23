namespace HoscyCore.Services.Output.Core;

/// <summary>
/// Determines when the preprocessor should be handled
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