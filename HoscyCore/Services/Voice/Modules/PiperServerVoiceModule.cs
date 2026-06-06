using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Network;
using HoscyCore.Services.Voice.Core;
using HoscyCore.Utility;
using Serilog;

namespace HoscyCore.Services.Voice.Modules;

[LoadIntoDiContainer(typeof(PiperServerVoiceModuleStartInfo))]
public class PiperServerVoiceModuleStartInfo : IVoiceModuleStartInfo
{
    public VoiceModuleConfigFlags ConfigFlags => VoiceModuleConfigFlags.PiperWeb;
    public string Name => "Piper Server";
    public string Description => "TTS using a Piper Webserver";
    public Type ModuleType => typeof(PiperServerVoiceModule);
}

[PrototypeLoadIntoDiContainer(typeof(PiperServerVoiceModule), Lifetime.Transient)]
public class PiperServerVoiceModule
(
    ILogger logger, 
    ConfigModel config,
    IWebClient client
)
    : VoiceModuleBase(logger.ForContext<PiperServerVoiceModule>())
{
    #region Injeced
    private readonly ConfigModel _config = config;
    private readonly IWebClient _client = client;
    #endregion

    #region Vars
    private Process? _process = null;
    #endregion

    #region Start / Stop
    protected override Res StartForService()
    { 
        if (!_config.Voice_Piper_Process_Enabled)
        {
            return ResC.Ok(); 
        }
        
        _logger.Debug("Starting piper process");
        if (string.IsNullOrWhiteSpace(_config.Voice_Piper_Process_Terminal) 
            || string.IsNullOrWhiteSpace(_config.Voice_Piper_Process_Voice)
            || string.IsNullOrWhiteSpace(_config.Voice_Piper_Process_VenvDir))
        {
            var err = ResC.FailLog("Can not start piper process without a voice, terminal, or working directory specified", _logger);
            SetFault(err.Msg!);
            return ResC.Ok();
        }

        var path = _config.Voice_Piper_Process_VenvDir.StartsWith('~') && _config.Voice_Piper_Process_VenvDir.Length > 1
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + _config.Voice_Piper_Process_VenvDir.Substring(1)
            : _config.Voice_Piper_Process_VenvDir;

        var process = new Process()
        {
            StartInfo = new ProcessStartInfo(_config.Voice_Piper_Process_Terminal)
            {
                CreateNoWindow = false,
                ErrorDialog = false,
                Arguments = $"./bin/python -m piper.http_server -m {_config.Voice_Piper_Process_Voice} --port {_config.Voice_Piper_Port}",
                UseShellExecute = false,
                WorkingDirectory = path,
                RedirectStandardInput = true,
            },
            EnableRaisingEvents = true,
        };

        var procRes = ResC.TWrapR(process.Start, "Failed to start process", _logger);
        if (!procRes.IsOk)
        {
            process.Dispose();
            SetFault(procRes.Msg);
            return ResC.Ok();
        }

        _process = process;
        _logger.Debug("Started piper process");

        return ResC.Ok();
    }
    protected override bool UseAlreadyStartedProtection => false;
    
    protected override Res StopForModule()
    {
        if (_process is null) return ResC.Ok();

        _logger.Debug("Stopping piper process");
        try
        {
            _process.StandardInput.Close();
            _process.Kill();
            _process.WaitForExit();
            _logger.Debug("Stopped piper process");
        } 
        catch (Exception ex)
        {
            return ResC.FailLog("Unable to stop debug process", _logger, ex);
        }

        return ResC.Ok();
    }
    protected override void DisposeCleanup()
    {
        _process?.Dispose();
        _process = null;
    }

    protected override bool IsStarted() => true;
    protected override bool IsProcessing() => IsStarted();
    #endregion

    #region Voice
    public override async Task<Res> CreateAudio(string message, Stream stream, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message)) 
            return ResC.Ok();

        var requestMessage = CreateMessage(message);
        if (!requestMessage.IsOk)
            return ResC.Fail(requestMessage.Msg);

        var res = await _client.SendAsyncBytes(requestMessage.Value, ctsExternal: ct);
        if (!res.IsOk)
            return ResC.Fail(res.Msg);

        stream.Write(res.Value);
        return ResC.Ok();
    }

    private Res<HttpRequestMessage> CreateMessage(string text)
    {
        List<string> args = [$"\"text\": \"{Regex.Escape(text)}\""];
        
        if(!string.IsNullOrWhiteSpace(_config.Voice_Piper_Request_Voice))
        {
            args.Add($"\"voice\": \"{Regex.Escape(_config.Voice_Piper_Request_Voice)}\"");
        }

        if (_config.Voice_Piper_Request_NoiseScale >= 0)
        {
            args.Add($"\"noise_scale\": {_config.Voice_Piper_Request_NoiseScale}");
        }

        if (_config.Voice_Piper_Request_NoiseWScale >= 0)
        {
            args.Add($"\"noise_w_scale\": {_config.Voice_Piper_Request_NoiseWScale}");
        }

        var content = new StringContent($"{{{string.Join(", ", args)}}}", new MediaTypeHeaderValue("application/json"));

        var ipRaw = (string.IsNullOrWhiteSpace(_config.Voice_Piper_Ip) ? "127.0.0.1" : _config.Voice_Piper_Ip)
            .Replace("localhost", "172.0.0.1", StringComparison.OrdinalIgnoreCase);
        var ipAndPortRaw = $"http://{ipRaw}:{_config.Voice_Piper_Port}";
        if (!Uri.TryCreate(ipAndPortRaw, UriKind.Absolute, out var uri))
            return ResC.TFailLog<HttpRequestMessage>($"Failed to convert config IP and Port ({ipAndPortRaw}) to valid URI", _logger);

        return ResC.TOk(new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = uri,
            Content = content
        });
    }
    #endregion
}