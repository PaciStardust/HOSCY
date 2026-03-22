using HoscyCore.Ipc;
using HoscyCore.Services.Recognition.Extra;
using Serilog;

namespace HoscyWhisperV2Process;

public class IpcDataHandler(ILogger logger)
{
    private readonly ILogger _logger = logger;
    private readonly IpcDataConverter _converter = new(logger);

    public event Action<WhisperIpcKeepalive> OnKeepAlive = delegate { };
    public event Action<WhisperIpcMute> OnMute = delegate { };
    public event Action<WhisperIpcStatus> OnStatus = delegate { };

    public void Handle(string data)
    {
        if (!_converter.IsValid(data))
        {
            _logger.Warning("Unable to handle IPC data {data}, it is invalid", data);
            return;
        }

        var id = _converter.GetIdentifier(data);
        switch (id)
        {
            case WhisperIpcKeepalive.IDENTIFIER:
                if (_converter.TryDeserialize<WhisperIpcKeepalive>(data, out var resAlive))
                {
                    OnKeepAlive(resAlive);
                }
                return;

            case WhisperIpcMute.IDENTIFIER:
                if (_converter.TryDeserialize<WhisperIpcMute>(data, out var resMute))
                {
                    _logger.Debug("Received mute sigmal with state \"{data}\"", id, resMute.State);
                    OnMute(resMute);
                }
                return;

            case WhisperIpcStatus.IDENTIFIER:
                if (_converter.TryDeserialize<WhisperIpcStatus>(data, out var resStatus))
                {
                    _logger.Debug("Received status sigmal with state \"{data}\"", id, resStatus.State);
                    OnStatus(resStatus);
                }
                return;

            default:
                _logger.Warning("Received unknown data with identifier {id}: \"{data}\"", id, data);
                return;
        }
    }
}