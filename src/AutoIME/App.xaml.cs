using System;
using System.Threading;
using Microsoft.UI.Xaml;

namespace AutoIME
{
    public partial class App : Application
    {
        private static Mutex? _singleInstanceMutex;
        public static MainWindow? MainWindowInstance { get; private set; }

        public App()
        {
            this.InitializeComponent();

            // Ensure single instance
            _singleInstanceMutex = new Mutex(true, "AutoIME_WinUI3_SingleInstance_Mutex", out bool createdNew);
            if (!createdNew)
            {
                // Another instance is already running
                Environment.Exit(0);
                return;
            }
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            MainWindowInstance = new MainWindow();
            MainWindowInstance.Activate();
        }
    }
}
