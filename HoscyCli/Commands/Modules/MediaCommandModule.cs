using HoscyCli.Commands.Core;
using HoscyCore.Configuration.Modern;
using HoscyCore.Services.Dependency;
using HoscyCore.Services.Media.Core;
using HoscyCore.Utility;

namespace HoscyCli.Commands.Modules;

[PrototypeLoadIntoDiContainer(typeof(MediaCommmandModule))]
public class MediaCommmandModule(ReflectPropEditCommandModule reflectCm, IMediaControlService media)
    : AttributeCommandModule, ICoreCommandModule
{
    #region Vars
    private readonly ReflectPropEditCommandModule _reflectCm = reflectCm;
    private readonly IMediaControlService _media = media;

    public string ModuleName => "Media";
    public string ModuleDescription => "Configure and control media";
    public string[] ModuleCommands => ["media"];
    #endregion
    
    #region CTL
    [SubCommandModule(["ctl-play"], "Control Play")] 
    public Res CmdCtlPlay()
    {
        Console.WriteLine("Sending media command \"Play\"");
        _media.PlayAsync().RunWithoutAwait();
        return ResC.Ok();
    }

    [SubCommandModule(["ctl-pause"], "Control Pause")] 
    public Res CmdCtlPause()
    {
        Console.WriteLine("Sending media command \"Pause\"");
        _media.PauseAsync().RunWithoutAwait();
        return ResC.Ok();
    }

    [SubCommandModule(["ctl-toggle"], "Control Toggle")] 
    public Res CmdCtlToggle()
    {
        Console.WriteLine("Sending media command \"Toggle\"");
        _media.PlayPauseAsync().RunWithoutAwait();
        return ResC.Ok();
    }

    [SubCommandModule(["ctl-next"], "Control Next")] 
    public Res CmdCtlNext()
    {
        Console.WriteLine("Sending media command \"Next\"");
        _media.NextAsync().RunWithoutAwait();
        return ResC.Ok();
    }

    [SubCommandModule(["ctl-previous"], "Control Previous")] 
    public Res CmdCtlPrevious()
    {
        Console.WriteLine("Sending media command \"Previous\"");
        _media.PreviousAsync().RunWithoutAwait();
        return ResC.Ok();
    }
    #endregion

    #region Backends
    [SubCommandModule(["backends"], "Lists media backends")] 
    public Res CmdBackends()
    {
        var backends = _media.GetModuleInfos();
        var backendText = backends.Count > 0
            ? string.Join("\n", backends.Select(x => $" - {x.Name} > {x.Description}"))
            : "[NONE]";
        Console.WriteLine($"All available media backends:\n{backendText}");
        return ResC.Ok();
    }

    [SubCommandModule(["selected-backend"], "Media backend to use")]
    public Res CmdSelectedBackend()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Media_Backend));
    }
    #endregion

    #region Display
    [SubCommandModule(["dsp-enabled"], "Should media changes be displayed")]
    public Res CmdDspEnabled()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Media_ShowStatus));
    }

    [SubCommandModule(["dsp-text-pause"], "Text to display on pause")]
    public Res CmdDspTextPause()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Media_PauseText));
    }

    [SubCommandModule(["dsp-add-album"], "Add album to text")]
    public Res CmdDspAlbum()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Media_AddAlbumToText));
    }

    [SubCommandModule(["dsp-filter-album"], "Filter out album name if title is similar")]
    public Res CmdDspFilterAlbum()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Media_FilterSameNameAlbum));
    }

    [SubCommandModule(["dsp-swap-order"], "Swap order of artist and title")]
    public Res CmdDspSwapOrder()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Media_SwapArtistAndSongInText));
    }

    [SubCommandModule(["dsp-text-playing"], "Text to display before artist and title")]
    public Res CmdDspTextPlaying()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Media_PlayingVerb));
    }

    [SubCommandModule(["dsp-text-intermediate"], "Text to display between artist and title")]
    public Res CmdDspTextIntermediate()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Media_IntermediateWord));
    }

    [SubCommandModule(["dsp-text-album"], "Text to display before album")]
    public Res CmdDspTextAlbum()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Media_AlbumWord));
    }

    [SubCommandModule(["dsp-text-extra"], "Text to display at end")]
    public Res CmdDspTextExtra()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Media_ExtraText));
    }
    #endregion

    #region Filtering
    [SubCommandModule(["dsp-filters"], "Text to filter out")]
    public Res CmdDspFilters()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Media_Filters));
    }
    #endregion

    #region Endpoints
    [SubCommandModule(["endpoints"], "Display endpoints")]
    public Res CmdEndpoints()
    {
        if (_media.CanGetEndpoints)
        {
            var endpoints = _media.GetEndpointNamesAsync().AsSync();
            if (!endpoints.IsOk)
            {
                return ResC.Fail(endpoints.Msg);
            }

            if (endpoints.Value.Length == 0)
            {
                Console.WriteLine("Current media backend can not locate any endpoints");
            }
            else
            {
                var endpointsText = string.Join("\n", 
                    endpoints.Value.Select(x => $" - {x}")
                );
                Console.WriteLine($"The following endpoints are available:\n{endpointsText}");
            }
        }
        else
        {
            Console.WriteLine("Current media backend does not provide endpoints");
        }
        return ResC.Ok();
    }
    #endregion

    #region Linux-Mpris
    [SubCommandModule(["mpris-pref-endpoints"], "Linux Mpris - Endpoints to prefer")]
    public Res CmdMprisPrefEndpoints()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Media_Mpris_PreferredEndpoints));
    }

    [SubCommandModule(["mpris-ign-endpoints"], "Linux Mpris - Endpoints to ignore")]
    public Res CmdMprisIgnEndpoints()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Media_Mpris_IgnoredEndpoints));
    }

    [SubCommandModule(["mpris-update-interval"], "Linux Mpris - Endpoints update interval (ms)")]
    public Res CmdMprisUpdateInterval()
    {
        return _reflectCm.SetProperty(nameof(ConfigModel.Media_Mpris_EndpointUpdateIntervalMs));
    }
    #endregion
}