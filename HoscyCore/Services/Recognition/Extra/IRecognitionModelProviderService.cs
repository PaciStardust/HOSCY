using System.Speech.Recognition;

namespace HoscyCore.Services.Recognition.Extra;

public interface IRecognitionModelProviderService
{
    public IReadOnlyList<(string Name, string Desc, string Id)> GetWindowsRecognizers();
}