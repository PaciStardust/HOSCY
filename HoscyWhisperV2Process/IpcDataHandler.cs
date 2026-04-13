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
                var resAlive = _converter.Deserialize<WhisperIpcKeepalive>(data);
                if (resAlive.IsOk)
                {
                    OnKeepAlive(resAlive.Value);
                }
                return;

            case WhisperIpcMute.IDENTIFIER:
                var resMute = _converter.Deserialize<WhisperIpcMute>(data);
                if (resMute.IsOk)
                {
                    _logger.Debug("Received mute signal with state \"{data}\"", resMute.Value.State);
                    OnMute(resMute.Value);
                }
                return;

            case WhisperIpcStatus.IDENTIFIER:
                var resStatus = _converter.Deserialize<WhisperIpcStatus>(data);
                if (resStatus.IsOk)
                {
                    _logger.Debug("Received status signal with state \"{data}\"", resStatus.Value.State);
                    OnStatus(resStatus.Value);
                }
                return;

            default:
                _logger.Warning("Received unknown data with identifier {id}: \"{data}\"", id, data);
                return;
        }
    }

    public void ClearActions()
    {
        OnKeepAlive = delegate { };
        OnMute = delegate { };
        OnStatus = delegate { };
    }
}