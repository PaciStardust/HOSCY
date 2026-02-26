using HoscyCore.Services.Dependency;
using HoscyCore.Services.Translation.Core;
using Serilog;

namespace HoscyCore.Services.Translation.Modules;

[PrototypeLoadIntoDiContainer(typeof(TestTranslationModuleStartInfo), Lifetime.Singleton)]
public class TestTranslationModuleStartInfo : ITranslationModuleStartInfo
{
    public string Name => "Test Translator";
    public string Description => "Translator for test purposes, only outputs random garbage";
    public Type ModuleType => typeof(TestTranslationModule);

    public TranslationModuleConfigFlags ConfigFlags 
        => TranslationModuleConfigFlags.None;
}

[PrototypeLoadIntoDiContainer(typeof(TestTranslationModule), Lifetime.Transient)]
public class TestTranslationModule(ILogger logger)
    : TranslationModuleBase(logger.ForContext<TestTranslationModule>())
{
    private const string CHARACTER_LIST = "abcdefghijklmnopqrstuvwxyz      ";

    private readonly Random _random = new();
    private bool _running = false;

    public override TranslationResult TryTranslate(string input, out string? output)
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

    protected override void StartForService()
    {
        _running = true;
    }
    protected override bool UseAlreadyStartedProtection => false;

    protected override void StopForModule()
    {
        _running = false;
    }
}