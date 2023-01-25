using CoreOSC;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading.Tasks;
using VRC.OSCQuery;
using System.Linq;

namespace Hoscy.Services.OscControl
{
    /// <summary>
    /// Static class for osc-related things
    /// </summary>
    internal static class Osc
    {
        private static OscListener? _listener;
        internal static int? ListenerPort => _listener?.Port;
        internal static bool HasInvalidFilters { get; private set; } = false;

        #region Sending
        /// <summary>
        /// Send OSC Data
        /// </summary>
        /// <param name="packet">Packet</param>
        /// <returns>Success?</returns>
        internal static bool Send(OscPacket p)
        {
            if(!p.IsValid)
            {
                Logger.Warning($"Attempted to send a package to {p.Ip}:{p.Port} ({p.Address}), but the contents were empty or port was invalid");
                return false;
            }

            try
            {
                var message = new OscMessage(p.Address, p.Variables);
                var sender = new UDPSender(p.Ip, p.Port);

                sender.Send(message);
                Logger.Debug($"Successfully sent data to {p}");
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to send OSC data.");
                return false;
            }
        }

        //Utility for splitting address
        internal static string[] SplitAddress(string address)
            => address[1..].Split('/');
        #endregion

        #region Listener
        /// <summary>
        /// Recreates all osc-listeners
        /// </summary>
        internal static void RecreateListener()
        {
            try
            {
                Logger.PInfo("Recreating listener");
                _listener?.Stop();
                _listener = new(Config.Osc.PortListen, CreateInputFilters());
                _listener.Start();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Failed to create OSC listener");
            }
        }

        /// <summary>
        /// Loads in and tests all InputFilters for the listener
        /// </summary>
        /// <returns>A list of tested filters</returns>
        private static List<OscRoutingFilter> CreateInputFilters()
        {
            Logger.PInfo("Creating input filters");
            HasInvalidFilters = false;

            List<OscRoutingFilter> filters = new();
            if (Config.Osc.RoutingFilters.Count == 0)
            {
                Logger.Info("No filtered listeners to create");
                return filters;
            }

            foreach (var filter in Config.Osc.RoutingFilters)
            {
                var newFilter = new OscRoutingFilter(filter.Name, filter.Ip, filter.Port, filter.Filters);

                if (!newFilter.TestValidity())
                {
                    Logger.Warning($"Skipping creation of listener \"{filter.Name}\" as its values are invalid (Name / Port / Ip / Filters)");
                    filter.SetValidity(false);
                    HasInvalidFilters = true;
                    continue;
                }

                filter.SetValidity(true);
                filters.Add(newFilter);
                Logger.Info($"Added new routing filter to listener: {newFilter}");
            }

            Logger.PInfo("Created new input filters");
            return filters;
        }

        internal static HostInfo? GetServiceProfile(string name)
            => _listener?.ServiceProfiles.TryGetValue(name, out var profile) ?? false ? profile : null;

        internal static List<string> GetServiceProfileNames()
            => _listener?.ServiceProfiles.Values.Select(x => x.name).ToList() ?? new List<string>();
        #endregion

        #region OSC Command Parsing
        //These are amazingly readable regexes, I know
        private static readonly Regex _oscCommandIdentifier = new(@"\[ *(?<address>(?:\/[^\ #\*,\/?\[\]\{\}]+)+)(?<values>(?: +\[(?:[fF]\]-?[0-9]+(?:\.[0-9]+)?|[iI]\]\-?[0-9]+|[sS]\]""[^""]*""|[bB]\](?:[tT]rue|[fF]alse)))+)(?: +(?:(?<ip>(?:(?:25[0-5]|(?:2[0-4]|1\d|[1-9]|)\d)\.?\b){4}):(?<port>[0-9]{1,5})|""(?<target>[^""]*)""))?(?: +[wW](?<wait>[0-9]+))? *\]", RegexOptions.CultureInvariant);
        private static readonly Regex _oscParameterExtractor = new(@" +\[(?<type>[iIfFbBsS])\](?:""(?<value>[^""]*)""|(?<value>[a-zA-Z]+|[0-9\.\-]*))", RegexOptions.CultureInvariant);
        /// <summary>
        /// Checks for message to be an osc command
        /// </summary>
        internal static bool ParseOscCommands(string message)
        {
            //Obtaining parsed command
            Logger.Info("Detected osc command, attempting to parse: " + message);
            var commandMatches = _oscCommandIdentifier.Matches(message);
            if (commandMatches == null || commandMatches.Count == 0)
            {
                Logger.Warning("Failed parsing osc command, it did not match the filter");
                return false;
            }

            var commandPackets = new List<(OscPacket, int)>();
            foreach (Match commandMatch in commandMatches)
            {
                var output = ParseOscCommandString(commandMatch);
                if (output == null)
                    return false;

                commandPackets.Add(output.Value);
            }

            if (commandPackets.Count == 0)
            {
                Logger.Warning("Failed to find any command packets to execute");
                return false;
            }

            var threadId = "ST-" + Guid.NewGuid().ToString().Split('-')[0];
            Task.Run(() => ExecuteOscCommands(threadId, commandPackets));
            return true;
        }

        /// <summary>
        /// Turns a command string into a packet and wait
        /// </summary>
        private static (OscPacket, int)? ParseOscCommandString(Match commandMatch)
        {
            Logger.Log("Attempting to parse osc subcommand: " + commandMatch.Value);

            string addressText = commandMatch.Groups["address"].Value;
            string valuesText = commandMatch.Groups["values"].Value;
            string targetText = commandMatch.Groups["target"].Value;
            string ipText = commandMatch.Groups["ip"].Value.Length == 0 ? Config.Osc.Ip.ToString() : commandMatch.Groups["ip"].Value;
            string portText = commandMatch.Groups["port"].Value.Length == 0 ? Config.Osc.Port.ToString() : commandMatch.Groups["port"].Value;
            string waitText = commandMatch.Groups["wait"].Value.Length == 0 ? "0" : commandMatch.Groups["wait"].Value;

            if (!int.TryParse(waitText, out var parsedWait))
            {
                Logger.Warning("Failed parsing osc subcommand, unable to parse wait");
                return null;
            }

            var parsedVariables = ParseOscVariables(valuesText).ToArray();
            OscPacket? packet;

            if (string.IsNullOrWhiteSpace(targetText))
            {
                if (!int.TryParse(portText, out var parsedPort))
                {
                    Logger.Warning("Failed parsing osc subcommand, unable to parse port");
                    return null;
                }

                packet = new(addressText, ipText, parsedPort, parsedVariables);
            }
            else
                packet = OscPacket.FromServiceProfile(targetText, addressText, parsedVariables);

            if (!packet.HasValue || !packet.Value.IsValid)
            {
                Logger.Warning("Failed parsing osc subcommand, packet is invalid");
                return null;
            }

            return (packet.Value, parsedWait);
        }

        /// <summary>
        /// Parses osc variables from string
        /// </summary>
        private static List<object> ParseOscVariables(string variableString)
        {
            var variableMatches = _oscParameterExtractor.Matches(variableString);
            var parsedVariables = new List<object>();
            foreach (Match variableMatch in variableMatches)
            {
                var type = variableMatch.Groups["type"].Value;
                var value = variableMatch.Groups["value"].Value;

                if (string.IsNullOrWhiteSpace(type))
                    continue;

                switch (type.ToLower())
                {
                    case "s":
                        parsedVariables.Add(value);
                        continue;

                    case "b":
                        parsedVariables.Add(value.Equals("true", StringComparison.OrdinalIgnoreCase));
                        continue;

                    case "f":
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedFloat))
                            parsedVariables.Add(parsedFloat);
                        continue;

                    case "i":
                        if (int.TryParse(value, out var parsedInt))
                            parsedVariables.Add(parsedInt);
                        continue;

                    default:
                        continue;
                }
            }
            return parsedVariables;
        }

        /// <summary>
        /// Runs all osc commands asyncronously
        /// </summary>
        /// <param name="threadId">identifier for thread</param>
        /// <param name="commandPackets">packets to execute with wait</param>
        private static Task ExecuteOscCommands(string taskId, List<(OscPacket, int)> commandPackets)
        {
            Logger.Info("Started osc subcommand execution task for: " + taskId);
            int cmdCount = commandPackets.Count;

            for (int i = 0; i < cmdCount; i++)
            {
                if (!App.Running)
                    return Task.CompletedTask;

                var packet = commandPackets[i];
                var identifier = $"{taskId} ({packet.Item1}";
                Logger.Info($"Running osc subcommand {i+1}/{cmdCount} for: " + identifier);

                if (!Send(packet.Item1))
                {
                    Logger.Warning($"Failed running osc subcommand {i+1}/{cmdCount} for: " + identifier);
                    return Task.CompletedTask;
                }
                if (i != commandPackets.Count - 1)
                {
                    Logger.Log($"Setting osc subcommand timeout of {packet.Item2}ms for " + taskId);
                    Task.Delay(packet.Item2).Wait();
                }

            }
            Logger.Info("Finished processing osc subcommands for: " + taskId);
            return Task.CompletedTask;
        }
        #endregion
    }
}
