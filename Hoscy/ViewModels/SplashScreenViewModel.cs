namespace Hoscy.ViewModels;

public class SplashScreenViewModel : ViewModelBase //todo: open log on error?
{
    private string _versionText = "v.?.?.?";
    public string VersionText
    {
        get => _versionText;
        set => SetProperty(ref _versionText, value);
    }

    private string _progress = "Unknown Progress";
    public string Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }
}