using System.Runtime.InteropServices;
using System.Speech.Recognition;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Recognition.Extra;

[PrototypeLoadIntoDiContainer(typeof(IRecognitionModelProviderService))]
public class RecognitionModelProviderService(ILogger logger) : IRecognitionModelProviderService
{
    private readonly ILogger _logger = logger.ForContext<RecognitionModelProviderService>();

    Res<List<WindowsRecognizerInfo>> IRecognitionModelProviderService.GetWindowsRecognizers()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var message = "Windows recognition engines are only available on Windows";
            return ResC.TFailLog<List<WindowsRecognizerInfo>>(message, _logger);
        }
        try
        {
            #pragma warning disable CA1416 // Validate platform compatibility
            var res = SpeechRecognitionEngine.InstalledRecognizers()
                    .Select(x => new WindowsRecognizerInfo(x.Name, x.Description, x.Id)).ToList();
            #pragma warning restore CA1416 // Validate platform compatibility
            return ResC.TOk(res);
        }
        catch (Exception ex)
        {
            return ResC.TFailLog<List<WindowsRecognizerInfo>>("Failed to retrieve installed recognizers", _logger, ex);
        }
    }
}