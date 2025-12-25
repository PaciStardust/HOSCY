using System.Diagnostics;
using HoscyCore;
using HoscyCore.Utility;

namespace HoscyCli;

class Program
{
    static void Main(string[] args)
    {
        Process? debugProcess = null;
        HoscyCoreApp? coreApp = null;
        try
        {
            #if DEBUG
            var log = LogUtils.CreateTemporaryLogger<Program>(disableConsoleLogging: true);
            log.Information("a");
            Console.WriteLine("Type 'y' to follow logs");
            if (Console.ReadLine() == "y")
            {
                var process = new Process();
                process.StartInfo.FileName = "foot";
                process.StartInfo.Arguments = $"-e tail -f {LogUtils.LogFileName}";
                process.StartInfo.UseShellExecute = true;
                process.Start();
                debugProcess = process;
            }
            #endif

            coreApp = new HoscyCoreApp(log);
            var coreAppParams = new HoscyCoreAppStartParameters()
            {
                OnProgress = new((s) => Console.WriteLine($"Loading: {s.Replace(Environment.NewLine, " ")}")),
                ShouldOpenConsoleIfRequested = false,
                DisableConsoleLog = true
            };
            coreApp.Start(coreAppParams);
            while (true)
            {
                //todo: logic
                Console.WriteLine("Type 'exit' to exit");
                if (Console.ReadLine() == "exit") break;
            }
        } catch (Exception e)
        {
            Console.WriteLine($"{e.GetType().FullName}: {e.Message}");
        }

        try
        {
            coreApp?.Stop();
            debugProcess?.Kill();
            debugProcess?.Dispose();
        } catch (Exception e)
        {
            Console.WriteLine($"{e.GetType().FullName}: {e.Message}");
        }
    }
}
