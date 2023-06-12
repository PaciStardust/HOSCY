using System.Collections.Generic;

namespace Hoscy.Models
{
    /// <summary>
    /// THIS CLASS IS USED IN CONFIG - IT CAN NOT LOG
    /// </summary>
    internal class OscRoutingFilterModel
    {
        public string Name
        {
            get => _name;
            set => _name = string.IsNullOrWhiteSpace(value) ? "Unnamed Filter" : value;
        }
        private string _name = "Unnamed Filter";
        public int Port
        {
            get => _port;
            set => _port = Utils.MinMax(value, -1, 65535);
        }
        private int _port = -1;
        public string Ip { get; set; } = "127.0.0.1";
        public List<string> Filters { get; set; } = new();
        public bool BlacklistMode { get; set; } = false;

        private bool _isValid = true;
        public override string ToString()
            => $"{(_isValid ? "" : "[x]")}{Name} => {Ip}:{Port}";

        /// <summary>
        /// Sets validity to be displayed in filter window
        /// </summary>
        internal void SetValidity(bool state)
            => _isValid = state;
    }
}
