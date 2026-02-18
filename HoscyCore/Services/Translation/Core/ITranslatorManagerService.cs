namespace HoscyCore.Services.Translation.Core;

public interface ITranslationManagerService
{
    public TranslationResult TryTranslate(string input, out string? output);
}