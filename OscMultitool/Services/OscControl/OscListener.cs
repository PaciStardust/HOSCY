using CoreOSC;
using System;
using System.Collections.Generic;
using System.Timers;
using VRC.OSCQuery;
using System.Threading.Tasks;

namespace Hoscy.Services.OscControl
{
    internal class OscListener
    {
        private readonly IReadOnlyList<OscRoutingFilter> _filters;
        private UDPListener? _listener;

        private readonly Dictionary<string, HostInfo> _serviceProfiles = new();
        private OSCQueryService? _queryService;
        internal IReadOnlyDictionary<string, HostInfo> ServiceProfiles => _serviceProfiles;

        private Timer? _refreshTimer;

        private readonly int _port;
        internal int Port => _port;

        internal OscListener(int port, List<OscRoutingFilter> filters)
        {
            _filters = filters;
            _port = port;
        }

        #region Starting, Ending
        /// <summary>
        /// Starts the OSC Listener
        /// </summary>
        /// <returns>Running status</returns>
        internal bool Start()
        {
            if (_listener != null)
                return true;

            try
            {
                var logger = new LoggerProxy<OSCQueryService>();

                _queryService = new OSCQueryServiceBuilder()
                                    .WithServiceName("HOSCY")
                                    .WithTcpPort(Extensions.GetAvailableTcpPort())
                                    .WithUdpPort(_port)
                                    .WithLogger(logger)
                                    .WithDiscovery(new MeaModDiscovery(logger))
                                    .Build();

                SetQueryServiceEndpoints();

                //Loading in serviceProfiles
                foreach (var service in _queryService.GetOSCQueryServices())
                    AddServiceProfile(service).RunWithoutAwait();
                _queryService.OnOscQueryServiceAdded += (OSCQueryServiceProfile service) => AddServiceProfile(service).RunWithoutAwait();
                SetQueryServiceRefresh(5000);

                _listener = new UDPListener(_port, Callback);
                Logger.PInfo($"OSC-Listener is now listening on port {_port}");
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to start OSC listener.");
                return false;
            }
        }

        /// <summary>
        /// Callback for parsing incoming osc packets
        /// </summary>
        /// <param name="packet">Incomping packet</param>
        private void Callback(CoreOSC.OscPacket packet)
        {
            var message = (OscMessage)packet;

            if (message == null)
            {
                Logger.Debug("Received an empty packet, skipping");
                return;
            }

            var argsInfo = new List<string>();
            foreach (var arg in message.Arguments)
                argsInfo.Add($"{arg.GetType().Name}({arg})" ?? "???");

            Logger.Debug($"Packet has been received on port {_port} ({message.Address}) => {string.Join(", ", argsInfo)}");
            HandleData(message.Address, message.Arguments.ToArray());
        }

        /// <summary>
        /// Stops the listener, it should always be running but can be stopped in case it needs to be restarted
        /// </summary>
        internal void Stop()
        {
            _refreshTimer?.Dispose();
            _refreshTimer = null;

            _serviceProfiles.Clear();
            _queryService?.Dispose();
            _queryService = null;

            _listener?.Close();
            //_listener?.Dispose(); This somehow crashes the program
            _listener = null;
            Logger.PInfo($"Stopped listener on port " + _port);
        }
        #endregion

        #region Data Handling
        /// <summary>
        /// Handles the data by sending it to all matching filters and checks for internal uses
        /// </summary>
        /// <param name="address">Target Address of packet</param>
        /// <param name="arguments">Arguments of packet</param>
        private void HandleData(string address, object[] arguments)
        {
            OscDataHandler.Handle(address, arguments);

            foreach (var filter in _filters)
                if (filter.Matches(address))
                    filter.Send(address, arguments);
        }
        #endregion

        #region OscQuery
        /// <summary>
        /// Sets the endpoints of the service to the ones used by HOSCY
        /// </summary>
        private void SetQueryServiceEndpoints()
        {
            if (_queryService == null)
                return;

            var endpoints = new string[]
            {
                Config.Osc.AddressManualMute,           "T",    "True -> Toggle Mute",
                Config.Osc.AddressManualSkipSpeech,     "T",    "True -> Skip Speech",
                Config.Osc.AddressManualSkipBox,        "T",    "True -> Skip Textbox",
                Config.Osc.AddressEnableReplacements,   "T",    "True -> Toggle Replacements",
                Config.Osc.AddressEnableTextbox,        "T",    "True -> Toggle Textbox",
                Config.Osc.AddressEnableTts,            "T",    "True -> Toggle TTS",
                Config.Osc.AddressEnableAutoMute,       "T",    "True -> Toggle Automute",
                Config.Osc.AddressAddTextbox,           "s",    "Text -> Send as Message",
                Config.Osc.AddressAddTts,               "s",    "Text -> Send as Voice",
                Config.Osc.AddressAddNotification,      "s",    "Text -> Send as Notification",
                "/any",                                 "[,]",  "Any -> HOSCY sends and receives anything for routing and custom commands"
            };
            for (int i = 0; i < endpoints.Length; i += 3)
            {
                _queryService.AddEndpoint(endpoints[i], endpoints[i + 1], Attributes.AccessValues.WriteOnly, null, endpoints[i + 2]);
            }
        }

        /// <summary>
        /// Refreshes Services every n ms
        /// </summary>
        private void SetQueryServiceRefresh(long timeout)
        {
            _queryService?.RefreshServices();

            _refreshTimer = new(timeout);
            _refreshTimer.Elapsed += (s, e) => _queryService?.RefreshServices();
            _refreshTimer.Start();
        }

        /// <summary>
        /// Adds ServiceProfile to list
        /// Likely not the cleanest implementation, but does the job
        /// </summary>
        private async Task AddServiceProfile(OSCQueryServiceProfile profile)
        {
            var connectionInfo = await Extensions.GetHostInfo(profile.address, profile.port);

            if (connectionInfo == null)
            {
                Logger.Error($"Failed to grab ConnectionInfo for ServiceProfile \"{profile.name}\"");
                return;
            }

            _serviceProfiles[connectionInfo.name.ToLower()] = connectionInfo;
            Logger.Info($"Adding service profile \"{profile.name}\"");
        }
        #endregion
    }
}
