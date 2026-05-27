using HoscyCore.Utility;
using Serilog;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Components;
using SoundFlow.Providers;

namespace HoscyCore.Services.Audio;

public class AudioPlaybackDeviceProxy : IDisposable
{
    private readonly AudioPlaybackDevice _playback;
    private readonly SoundPlayer _player;
    private readonly StreamDataProvider _provider;

    public MemoryStream Stream { get; private init; }

    public AudioPlaybackDeviceProxy(AudioPlaybackDevice playback)
    {
        _playback = playback;

        Stream = new();
        var header = AudioUtils.BaseWavHeader;
        AudioUtils.WriteRestOfWavHeader(header.AsSpan());
        Stream.Write(header);

        _provider = new(_playback.Engine, _playback.Format, Stream);

        _player = new(_playback.Engine, _playback.Format, _provider);
    }

    public Res Start(ILogger logger)
    {
        return ResC.WrapR(() =>
        {
            _playback.MasterMixer.AddComponent(_player);
            _playback.Start();
            //todo: does player start need to be called here?
        }, "Failed to start playback device", logger);
    }

    public Res Stop(ILogger logger)
    {
        return ResC.WrapR(() =>
        {
            _playback.Stop();
            _playback.MasterMixer.RemoveComponent(_player);
        }, "Failed to stop playback device", logger);
    }

    public void SetVolume(float volume)
    {
        _player.Volume = volume.MinMax(0, 1);
    }

    public bool IsRunning => _playback.IsRunning;

    public void Dispose()
    {
        if (!_player.IsDisposed)
            _player.Dispose();
        if (!_provider.IsDisposed)
            _provider.Dispose();
        Stream.Dispose();
        if (!_playback.IsDisposed)
            _playback.Dispose();
    }
}