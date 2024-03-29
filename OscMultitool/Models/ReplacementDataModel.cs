﻿namespace Hoscy.Models
{
    /// <summary>
    /// THIS CLASS IS USED IN CONFIG - IT CAN NOT LOG
    /// </summary>
    internal class ReplacementDataModel
    {
        public string Text
        {
            get => _text;
            set => _text = string.IsNullOrWhiteSpace(value) ? "New Text" : value;
        }
        private string _text = "New Text";

        public string Replacement { get; set; } = "Example";

        public bool Enabled { get; set; } = true;
        public bool UseRegex { get; set; } = false;
        public bool IgnoreCase { get; set; } = true;

        public ReplacementDataModel(string text, string replacement, bool ignoreCase = true)
        {
            Text = text;
            Replacement = replacement;
            IgnoreCase = ignoreCase;
        }
        public ReplacementDataModel() { }

        public override string ToString()
            => $"{(Enabled ? string.Empty : "[x] ")}{Text} ={(UseRegex ? "R" : string.Empty)}{(IgnoreCase ? string.Empty : "C")}> {Replacement}";
    }
}
