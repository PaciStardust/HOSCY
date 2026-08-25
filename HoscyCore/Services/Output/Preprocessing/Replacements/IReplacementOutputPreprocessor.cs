using HoscyCore.Services.Output.Core;
using HoscyCore.Utility;

namespace HoscyCore.Services.Output.Preprocessing.Replacements;

public interface IReplacementOutputPreprocessor : IOutputPreprocessor
{
    public int LastLoadCorrect { get; }
    public int LastLoadBroken { get; }
    public int LastLoadDisabled { get; }
    public Res ReloadReplacements();
}

public interface IFullReplacementOutputPreprocessor : IReplacementOutputPreprocessor { }
public interface IPartialReplacementOutputPreprocessor : IReplacementOutputPreprocessor { }