using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OscMultitool.Services.Speech.Recognizers
{
    public readonly struct RecognizerPerms
    {
        public RecognizerPerms() { }
        public readonly bool UsesVoskModel { get; init; } = false;
        public readonly bool UsesWinRecognizer { get; init; } = false;
        public readonly bool UsesMicrophone { get; init; } = false;
    }
}
