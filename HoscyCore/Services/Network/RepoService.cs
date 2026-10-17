using System.Text.RegularExpressions;
using HoscyCore.Services.Dependency;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Network;

[LoadIntoDiContainer(typeof(IRepoService))]
public class RepoService(ILogger logger, IWebClient client) : IRepoService
{
    public string RepoUrl => "https://github.com/PaciStardust/HOSCY";

    private readonly ILogger _logger = logger.ForContext<RepoService>();
    private readonly IWebClient _client = client;

    private static readonly Regex _tagMatch = new(@"^[vV]\.(?'version'[0-9]+(?:\.[0-9]+)*)$");
    public async Task<Res<UpdateInfo>?> CheckForUpdates()
    {
        _logger.Information("Checking for new releases on GitHub");

        var currentVersionRaw = LaunchUtils.GetVersion();
        if (currentVersionRaw is null)
            return ResC.TFailLog<UpdateInfo>("Failed to retrieve current version", _logger);
        
        var currentMatch = _tagMatch.Match(currentVersionRaw);
        if (!currentMatch.Success)
            return ResC.TFailLog<UpdateInfo>($"Current release version \"{currentVersionRaw}\" is not a valid format", _logger);
        var currentVersion = currentMatch.Groups["version"].Value.Split(".").Select(int.Parse).ToList();

        var urlLatest = "https://api.github.com/repos/pacistardust/hoscy/releases/latest";
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, urlLatest);

        var res = await _client.SendAsyncString(requestMessage);
        if (!res.IsOk)
        {
            _logger.Warning("Failed to retrieve version info");
            return ResC.TFail<UpdateInfo>(res.Msg);
        }

        var releaseTag = OtherUtils.ExtractFromJson("tag_name", res.Value, _logger);
        if (!releaseTag.IsOk) 
            return ResC.TFailLog<UpdateInfo>("Failed to grab release tag from release", _logger);
        _logger.Information("Current repo release tag is \"{tag}\"", releaseTag.Value);

        var tagMatch = _tagMatch.Match(releaseTag.Value);
        if (!tagMatch.Success)
            return ResC.TFailLog<UpdateInfo>($"Parsed release tag \"{releaseTag.Value}\" is not a valid format", _logger);
        var releaseVersion = tagMatch.Groups["version"].Value.Split(".").Select(int.Parse).ToList();

        if (currentVersion.Count > releaseVersion.Count)
            releaseVersion.AddRange(new int[currentVersion.Count - releaseVersion.Count].Select(x => 0));
        else if (currentVersion.Count < releaseVersion.Count)
            currentVersion.AddRange(new int[releaseVersion.Count - currentVersion.Count].Select(x => 0));

        var updateNeeded = false;
        for (var i = 0; i < currentVersion.Count; i++)
        {
            if (releaseVersion[i] == currentVersion[i])
            {
                _logger.Verbose("Tag segment {seg} of release and current are equal ({segVal})", i+1, releaseVersion[i]);
                continue;
            }

            if (releaseVersion[i] > currentVersion[i])
            {
                _logger.Information("Tag segment {seg} of release is higher than current ({segValRelease} > {segValCurrent}) => Update Available",
                    i+1, releaseVersion[i], currentVersion[i]);
                updateNeeded = true;
                break;
            }

            _logger.Information("Tag segment {seg} of current is higher than release ({segValRelease} < {segValCurrent}) => No Update",
                i+1, releaseVersion[i], currentVersion[i]);
            return null;
        }

        if (!updateNeeded)
        {
            _logger.Information("Current version equals release version \"{tag}\" => No Update", currentVersionRaw);
            return null;
        }

        var releaseUrl = OtherUtils.ExtractFromJson("url", res.Value, _logger);
        var releaseTitle = OtherUtils.ExtractFromJson("name", res.Value, _logger);
        var releaseText = OtherUtils.ExtractFromJson("body", res.Value, _logger);

        return ResC.TOk(new UpdateInfo(releaseText.Value, releaseText.Value,
            currentVersionRaw, releaseTag.Value, releaseUrl.Value));
    }
}