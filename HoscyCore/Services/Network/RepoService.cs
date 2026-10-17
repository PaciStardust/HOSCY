using HoscyCore.Services.Dependency;

namespace HoscyCore.Services.Network;

[LoadIntoDiContainer(typeof(IRepoService))]
public class RepoService : IRepoService
{
    public string RepoUrl => "https://github.com/PaciStardust/HOSCY";
}