using HoscyCore.Services.Core;

namespace HoscyCore.Services.Audio;

public interface IApplicationSound : IService
{
    public void PlayMuteSound();
}