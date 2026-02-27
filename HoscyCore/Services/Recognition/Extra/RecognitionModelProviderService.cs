using System.Runtime.InteropServices;
using System.Speech.Recognition;
using HoscyCore.Services.Dependency;

namespace HoscyCore.Services.Recognition.Extra;

[PrototypeLoadIntoDiContainer(typeof(IRecognitionModelProviderService))]
public class RecognitionModelProviderService : IRecognitionModelProviderService
{
    public IReadOnlyList<RecognizerInfo> GetWindowsRecognizers()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return [];

        return SpeechRecognitionEngine.InstalledRecognizers();
    }
}