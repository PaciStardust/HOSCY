using System.Runtime.InteropServices;
using System.Speech.Recognition;
using HoscyCore.Services.Dependency;
using Whisper;

namespace HoscyCore.Services.Recognition.Extra;

[PrototypeLoadIntoDiContainer(typeof(IRecognitionModelProviderService))]
public class RecognitionModelProviderService : IRecognitionModelProviderService
{
    IReadOnlyList<(string Name, string Desc, string Id)> IRecognitionModelProviderService.GetWindowsRecognizers()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return [];

#pragma warning disable CA1416 // Validate platform compatibility
        return SpeechRecognitionEngine.InstalledRecognizers()
                .Select(x => (x.Name, x.Description, x.Id)).ToList();
#pragma warning restore CA1416 // Validate platform compatibility
    }

    public IReadOnlyList<string> GetGraphicsAdapters()
    {
        return Library.listGraphicAdapters();
    }
}