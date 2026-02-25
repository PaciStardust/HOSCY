using HoscyCore.Services.Core;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Translation.Core;

namespace HoscyCore.Services.Translation.Modules;

[PrototypeLoadIntoDiContainer(typeof(TestTranslationModuleStartInfo), Lifetime.Singleton)]
public class TestTranslationModuleStartInfo : ITranslationModuleStartInfo
{
    public string Name => "Test Translator";
    public string Description => "Translator for test purposes, only outputs random garbage";
    public Type ModuleType => typeof(TestTranslationModule);
}

[PrototypeLoadIntoDiContainer(typeof(TestTranslationModule), Lifetime.Transient)]
public class TestTranslationModule : StartStopModuleBase, ITranslationModule
{
    private const string CHARACTER_LIST = "abcdefghijklmnopqrstuvwxyz      ";

    private readonly Random _random = new();
    private bool _running = false;

    public override void Restart()
    {
        return;
    }

    public TranslationResult TryTranslate(string input, out string? output)
    {
        var characters = _random.Next(100);
        var words = new char[characters];

        for(var i = 0; i < characters; i++)
        {
            words[i] = CHARACTER_LIST[_random.Next(CHARACTER_LIST.Length)];
        }

        output = new string(words);
        return TranslationResult.Succeeded;
    }

    protected override bool IsProcessing()
    {
        return IsStarted();
    }

    protected override bool IsStarted()
    {
        return _running;
    }

    protected override void StartInternal()
    {
        _running = true;
    }

    protected override void StopInternal()
    {
        _running = false;
    }
}