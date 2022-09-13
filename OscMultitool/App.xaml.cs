using OscMultitool.Services.Speech;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace OscMultitool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //Indicator for threads to stop
        public static bool Running { get; private set; } = true;
        protected override void OnExit(ExitEventArgs e)
        {
            if (Recognition.IsRecognizerRunning)
                Recognition.StopRecognizer();

            Config.SaveConfig();
            Running = false;
        }
    }
}
