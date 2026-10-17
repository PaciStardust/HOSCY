using HoscyCore.Services.Core;

namespace HoscyCore.Services.Network;

public interface IRepoService : IService
{
    public string RepoUrl { get; }
}