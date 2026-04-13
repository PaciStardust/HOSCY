using HoscyCore.Utility;

namespace HoscyCore.Services.Recognition.Extra;

public interface IRecognitionModelProviderService
{
    public Res<List<WindowsRecognizerInfo>> GetWindowsRecognizers();
}

public record WindowsRecognizerInfo(string Name, string Desc, string Id);