namespace HoscyCore.Services.Translation.Core;

public interface ITranslatorManagerService
{
    public TranslationResult TryTranslate(string input, out string? output);
}