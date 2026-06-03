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

    #region Override
    protected override Res AddExtrasSubcommands(List<(SubCommandModuleAttribute Attribute, Func<string?, Res> Func)> list)
    {
        _reflectCm.GenerateQuickConfigCommands(ModuleName, list, GetQuickCommands());
        return base.AddExtrasSubcommands(list);
    }

    private QuickConfigCommandInfo[] GetQuickCommands()
    {
        return [
            new(["selected-backend"], nameof(ConfigModel.Media_Backend), ConfigModel.DESC_Media_Backend),
            
            new(["dsp-enabled"], nameof(ConfigModel.Media_ShowStatus), ConfigModel.DESC_Media_ShowStatus),
            new(["dsp-text-pause"], nameof(ConfigModel.Media_PauseText), ConfigModel.DESC_Media_PauseText),
            new(["dsp-add-album"], nameof(ConfigModel.Media_AddAlbumToText), ConfigModel.DESC_Media_AddAlbumToText),
            new(["dsp-filter-album"], nameof(ConfigModel.Media_FilterSameNameAlbum), ConfigModel.DESC_Media_FilterSameNameAlbum),
            new(["dsp-swap-order"], nameof(ConfigModel.Media_SwapArtistAndSongInText), ConfigModel.DESC_Media_SwapArtistAndSongInText),
            new(["dsp-text-playing"], nameof(ConfigModel.Media_PlayingVerb), ConfigModel.DESC_Media_PlayingVerb),
            new(["dsp-text-intermediate"], nameof(ConfigModel.Media_IntermediateWord), ConfigModel.DESC_Media_IntermediateWord),
            new(["dsp-text-album"], nameof(ConfigModel.Media_AlbumWord), ConfigModel.DESC_Media_AlbumWord),
            new(["dsp-text-extra"], nameof(ConfigModel.Media_ExtraText), ConfigModel.DESC_Media_ExtraText),
            
            new(["dsp-filters"], nameof(ConfigModel.Media_Filters), ConfigModel.DESC_Media_Filters),
            
            new(["mpris-pref-endpoints"], nameof(ConfigModel.Media_Mpris_PreferredEndpoints), ConfigModel.DESC_Media_Mpris_PreferredEndpoints),
            new(["mpris-ign-endpoints"], nameof(ConfigModel.Media_Mpris_IgnoredEndpoints), ConfigModel.DESC_Media_Mpris_IgnoredEndpoints),
            new(["mpris-update-interval"], nameof(ConfigModel.Media_Mpris_EndpointUpdateIntervalMs), ConfigModel.DESC_Media_Mpris_EndpointUpdateIntervalMs),
        ];
    }
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
}