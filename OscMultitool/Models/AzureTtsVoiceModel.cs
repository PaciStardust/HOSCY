namespace Hoscy.Models
{
    internal class AzureTtsVoiceModel
    {
        public string Name
        {
            get => _name;
            set => _name = string.IsNullOrWhiteSpace(value) ? "New Voice" : value;
        }
        private string _name = "New Voice";

        public string Voice { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;

        public override string ToString()
            => $"{(string.IsNullOrWhiteSpace(Language) ? string.Empty : $"[{Language}] ")}{Name}";
    }
}
