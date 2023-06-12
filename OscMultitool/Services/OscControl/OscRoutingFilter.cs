using System.Collections.Generic;

namespace Hoscy.Services.OscControl
{
    internal readonly struct OscRoutingFilter
    {
        internal readonly string Name { get; init; } = "OSC-Filter";
        internal readonly int Port { get; init; } = -1;
        internal readonly string Ip { get; init; } = string.Empty;
        internal readonly IReadOnlyList<string> Filters { get; init; } = new List<string>();
        internal readonly bool BlacklistMode { get; init; } = false;

        internal OscRoutingFilter(string name, string ip, int port, List<string> filters, bool blacklist)
        {
            Name = name;
            Port = port;
            Ip = ip;
            Filters = filters.AsReadOnly();
            BlacklistMode = blacklist;
        }

        #region Validation
        /// <summary>
        /// Checks if an address matches the filters filters
        /// </summary>
        /// <param name="input">Address to check</param>
        /// <returns>Matches?</returns>
        internal bool Matches(string input)
        {
            if (Filters.Count == 0)
                return true;

            foreach (var filter in Filters)
                if (input.StartsWith(filter))
                    return !BlacklistMode;

            return BlacklistMode;
        }

        /// <summary>
        /// Checks validity of filter by attempting to send a packet to that location
        /// </summary>
        /// <returns>Success?</returns>
        internal bool TestValidity()
        {
            if (string.IsNullOrWhiteSpace(Ip) || string.IsNullOrWhiteSpace(Name))
                return false;

            Logger.Log($"Performing test send of filter {ToString()}");
            if (!Send("/osctest123/", false))
            {
                Logger.Warning($"Failed test send of filter {ToString()}");
                return false;
            }

            return true;
        }
        #endregion

        /// <summary>
        /// Utility for checking and validating packets sent from filter
        /// </summary>
        /// <param name="address">Target address</param>
        /// <param name="args">Arguments for the packets</param>
        /// <returns>Success?</returns>
        internal bool Send(string address, params object[] args)
        {
            var packet = new OscPacket(address, Ip, Port, args);
            if (!packet.IsValid)
                return false;

            return Osc.Send(packet);
        }

        public override string ToString()
            => $"{Name} => {Ip}:{Port}";
    }
}
