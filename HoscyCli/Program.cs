using HoscyCore.Utility;

namespace HoscyCli;

class Program
{
    static void Main(string[] args)
    {
        var wrapper = new CliCoreWrapper();

        var startRes = ResC.TWrap(wrapper.Start, "Failed to start wrapper", null, ResMsgLvl.Fatal);
        if (!startRes.IsOk)
        {
            SendConfirmationMessage(startRes.Msg.WithContext("Error during startup").ToString());
        }
        else if (startRes.Value.Length > 0)
        {
            var messages = string.Join("\n", startRes.Value.Select(x => $" - {x}"));
            SendConfirmationMessage($"Following warnings were sent during startup:\n{messages}");
        }
        
        ResC.Wrap(wrapper.RunLoop, "Failed to run wrapper loop", null, ResMsgLvl.Fatal)
            .IfFail((x) => SendConfirmationMessage(x.WithContext("Error during loop").ToString()));

        ResC.Wrap(wrapper.Stop, "Failed to stop wrapper", null, ResMsgLvl.Fatal)
            .IfFail((x) => SendConfirmationMessage(x.WithContext("Error during shutdown").ToString()));
    }

    private static void SendConfirmationMessage(string message)
    {
        Console.WriteLine($"{message}\n\n(Press ENTER to continue)");
        Console.ReadLine();
    }
}
