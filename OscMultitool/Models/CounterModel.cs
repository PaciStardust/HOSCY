using System;
using System.Globalization;

namespace Hoscy.Models
{
    internal class CounterModel
    {
        private static readonly NumberFormatInfo _nfi = new()
        {
            NumberDecimalDigits = 0,
            NumberGroupSeparator = "."
        };

        public string Name
        {
            get => _name;
            set => _name = string.IsNullOrWhiteSpace(value) ? "Unnamed Counter" : value;
        }
        private string _name = "Unnamed Counter";
        public uint Count { get; set; } = 0;
        public DateTime LastUsed { get; set; } = DateTime.MinValue;
        public bool Enabled { get; set; } = true;
        public float Cooldown
        {
            get => _cooldown;
            set => _cooldown = Utils.MinMax(value, 0, 3600);
        }
        private float _cooldown = 0;

        public string Parameter
        {
            get => _parameter;
            set
            {
                _parameter = value;
                _fullParameter = value.StartsWith("/") ? value : "/avatar/parameters/" + value;
            }
        }
        private string _parameter = "Parameter";
        private string _fullParameter = "/avatar/parameters/Parameter";

        internal void Increase()
        {
            Count++;
            LastUsed = DateTime.Now;
        }

        internal string FullParameter() => _fullParameter;

        public override string ToString()
            => $"{(Enabled ? "" : "[x] ")}{Name}: {Count.ToString("N", _nfi)}";
    }
}
