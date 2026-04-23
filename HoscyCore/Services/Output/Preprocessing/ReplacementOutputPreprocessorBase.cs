using System.Text;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Output.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Output.Preprocessing;

public abstract class ReplacementOutputPreprocessorBase<T> : IOutputPreprocessor
{
    protected readonly ConfigModel _config;
    protected readonly ILogger _logger;
    protected readonly List<T> _handlers = [];

    public ReplacementOutputPreprocessorBase(ConfigModel config, ILogger logger)
    {
        _config = config;
        _logger = logger;
        ReloadReplacements();
    }

    #region Load Info
    public int LastLoadCorrect { get; private set; } = 0;
    public int LastLoadBroken { get; private set; } = 0;
    public int LastLoadDisabled { get; private set; } = 0;
    #endregion

    #region Abstract
    public abstract T ConvertToHandler(ReplacementDataModel model);
    public abstract List<ReplacementDataModel> GetReplacementModels();
    public abstract OutputPreprocessorHandlingStage GetHandlingStage();
    public abstract bool IsEnabled();
    public abstract bool IsFullReplace();
    public abstract OutputPreprocessorResult Process(ref string contents);
    #endregion

    #region Updating
    public Res ReloadReplacements()
    {
        _handlers.Clear();

        LastLoadCorrect = 0;
        LastLoadBroken = 0;
        LastLoadDisabled = 0;

        var models = GetReplacementModels();
        _logger.Debug("Reloading {replacementModelCount} handlers", models.Count);

        var converted = new List<T>();
        var countDisabled = 0;
        var countBroken = 0;

        foreach (var replacement in models)
        {
            if (!replacement.Enabled)
            {
                countDisabled++;
                continue;
            }

            try
            {
                var handler = ConvertToHandler(replacement);
                converted.Add(handler);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Could not instantiate handler \"{replacementText}\", RegEx might be broken => Skipping and disabling", replacement.Text);
                replacement.Enabled = false;
                countBroken++;
            }
        }

        LastLoadCorrect = converted.Count;
        LastLoadBroken = countBroken;
        LastLoadDisabled = countDisabled;


        _logger.Debug("Reloaded {replacementModelCountLoaded}/{replacementModelCount} handlers, {disabledCount} disabled, {brokenCount} broken",
            converted.Count, models.Count, countDisabled, countBroken);
        _handlers.AddRange(converted);

        return countBroken == 0
            ? ResC.Ok()
            : ResC.FailLog($"{countBroken} models could not be loaded and have been disabled", _logger, lvl: ResMsgLvl.Warning);
    }
    #endregion
}