using System.Speech.Recognition;

namespace HoscyCore.Services.Recognition.Extra;

public interface IRecognitionModelProviderService
{
    public IReadOnlyList<RecognizerInfo> GetWindowsRecognizers();
}