using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCore.Services.Network;

public interface IRepoService : IService
{
    public string RepoUrl { get; }
    public Task<Res<UpdateInfo>?> CheckForUpdates();
}

public record UpdateInfo(string? Title, string? Text, string VersionCurrent, string VersionNew, string? Url);