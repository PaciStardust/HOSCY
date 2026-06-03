using HoscyCore.Utility;
using Serilog;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Components;

namespace HoscyCore.Services.Audio;

public class AudioPlaybackDeviceProxy(AudioPlaybackDevice playback, ILogger logger) : IDisposable
{
    private readonly AudioPlaybackDevice _playback = playback;
    private readonly ILogger _logger = logger;
    
    public MemoryStream Stream { get; private init; } = new(); //todo:[REFACTOR] Optimize this to only use a singular component

    public Res Start()
    {
        return ResC.WrapR(_playback.Start, "Failed to start playback device", _logger);
    }

    public Res Stop()
    {
        return ResC.WrapR(_playback.Stop, "Failed to stop playback device", _logger);
    }

    public bool IsRunning => _playback.IsRunning;

    public void Dispose()
    {
        Stream.Dispose();
        if (!_playback.IsDisposed)
            _playback.Dispose();
    }

    public async Task<Res> PlayAsync(float volume, CancellationToken ct)
    {
        FixedStreamDataProvider? provider = null;
        SoundPlayer? player = null;
        try
        {
            _logger.Verbose("Initializing components to play audio");
            provider = new FixedStreamDataProvider(_playback.Engine, _playback.Format, Stream);
            player = new SoundPlayer(_playback.Engine, _playback.Format, provider)
            {
                Volume = volume,
            };
            _playback.MasterMixer.AddComponent(player);

            _logger.Verbose("Starting playback, awaiting end");
            player.Play();
            while (player.State == SoundFlow.Enums.PlaybackState.Playing && !ct.IsCancellationRequested)
            {
                await Task.Delay(10);
            }
            _logger.Verbose("Playback complete");
            player.Stop();
        }
        catch (Exception ex)
        {
            return ResC.FailLog("Failed to play audio", _logger, ex);
        }
        finally
        {
            _logger.Verbose("Cleaning up components for playback");
            if (player is not null)
            {
                _playback.MasterMixer.RemoveComponent(player);
                player.Dispose();
            }
            provider?.Dispose();
        }

        return ResC.Ok();
    }

    public void ClearStream()
    {
        Stream.Position = 0;
        Stream.SetLength(0);
        Stream.Capacity = 0;
    }
}