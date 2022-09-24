using Hoscy;
using System.Collections.Generic;

namespace Hoscy.OscControl
{
    public readonly struct OscRoutingFilter
    {
        public readonly string Name { get; init; } = "OSC-Filter";
        public readonly int Port { get; init; } = -1;
        public readonly string Ip { get; init; } = string.Empty;
        public readonly IReadOnlyList<string> Filters { get; init; } = new List<string>();

        public OscRoutingFilter(string name, string ip, int port, List<string> filters)
        {
            Name = name;
            Port = port;
            Ip = ip;
            Filters = filters.AsReadOnly();
        }

        #region Validation
        /// <summary>
        /// Checks if an address matches the filters filters
        /// </summary>
        /// <param name="input">Address to check</param>
        /// <returns>Matches?</returns>
        public bool Matches(string input)
        {
            if (Filters.Count == 0)
                return true;

            foreach (var filter in Filters)
                if (input.StartsWith(filter))
                    return true;

            return false;
        }

        /// <summary>
        /// Checks validity of filter by attempting to send a packet to that location
        /// </summary>
        /// <returns>Success?</returns>
        public bool TestValidity()
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
        public bool Send(string address, params object[] args)
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
