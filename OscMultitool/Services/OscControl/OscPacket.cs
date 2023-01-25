using System;
using System.Collections.Generic;

namespace Hoscy.Services.OscControl
{
    internal readonly struct OscPacket
    {
        internal readonly string Address { get; init; } = string.Empty;
        internal readonly string Ip { get; init; } = string.Empty;
        internal readonly int Port { get; init; } = -1;
        internal readonly object[] Variables { get; init; } = Array.Empty<object>();
        internal bool IsValid => ValidatePacket();

        internal OscPacket(string address, string ip, int port, params object[] variables)
        {
            Address = address;
            Variables = variables;
            Ip = ip;
            Port = port;
        }

        internal OscPacket(string address, params object[] variables)
        {
            Address = address;
            Variables = variables;
            Ip = Config.Osc.Ip;
            Port = Config.Osc.Port;
        }

        /// <summary>
        /// Basic validity check for packets (Checking basic ip validity, if packet is empty and if port is valid)
        /// </summary>
        /// <returns>Valid?</returns>
        private bool ValidatePacket()
        {
            if (!Address.StartsWith('/') || string.IsNullOrWhiteSpace(Ip) || Variables.Length == 0)
                return false;

            if (Port < 1 || Port > 65535)
                return false;

            return true;
        }

        /// <summary>
        /// Creates a OSCPacket using a ServiceProfile as endpoint
        /// </summary>
        internal static OscPacket? FromServiceProfile(string profile, string address, params object[] variables)
        {
            profile = profile.ToLower();
            if (profile == "self")
                return new(address, "127.0.0.1", Config.Osc.PortListen, variables);

            var serviceProfile = Osc.GetServiceProfile(profile);
            if (serviceProfile == null) return null;

            return new(address, serviceProfile.oscIP, serviceProfile.oscPort, variables);
        }

        /// <summary>
        /// Override to convert to string for logging purposes
        /// </summary>
        /// <returns>String containing IP, Port, Address and Arguments</returns>
        public override string ToString()
        {
            var argsInfo = new List<string>();
            foreach (var arg in Variables)
                argsInfo.Add($"{arg.GetType().Name}({arg})" ?? "???");

            return $"{Ip}:{Port} ({Address}) => {string.Join(", ", argsInfo)}";
        }

    }
}
