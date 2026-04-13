using HoscyCore.Utility;

namespace HoscyCli;

class Program
{
    static void Main(string[] args)
    {
        var wrapper = new CliCoreWrapper();

        ResC.Wrap(wrapper.Start, "Failed to start wrapper", null, ResMsgLvl.Fatal)
            .IfFail((x) => Console.WriteLine(x.WithContext("Startup").ToString()));
        
        ResC.Wrap(wrapper.RunLoop, "Failed to run wrapper loop", null, ResMsgLvl.Fatal)
            .IfFail((x) => Console.WriteLine(x.WithContext("Loop").ToString()));

        ResC.Wrap(wrapper.Stop, "Failed to stop wrapper", null, ResMsgLvl.Fatal)
            .IfFail((x) => Console.WriteLine(x.WithContext("Shutdown").ToString()));
    }
}
