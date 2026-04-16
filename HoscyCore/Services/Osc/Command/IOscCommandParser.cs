using HoscyCore.Services.Core;
using HoscyCore.Utility;

namespace HoscyCore.Services.Osc.Command;

public interface IOscCommandParser : IService
{
    public bool DetectCommandPrefix(string commandString);
    public Res<OscCommandInfo[]> Parse(string commandString);
}