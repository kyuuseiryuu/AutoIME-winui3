using System;
using System.Threading;
using Microsoft.UI.Xaml;

namespace AutoIME
{
    public partial class App : Application
    {
        private static Mutex? _singleInstanceMutex;
        public static MainWindow? MainWindowInstance { get; private set; }

        // Ensure single instance
        public static void ReleaseSingleInstanceMutex()
        {
            try
            {
                if (_singleInstanceMutex != null)
                {
                    _singleInstanceMutex.ReleaseMutex();
                    _singleInstanceMutex.Dispose();
                    _singleInstanceMutex = null;
                }
            }
            catch { }
        }

        public App()
        {
            this.InitializeComponent();

            this.UnhandledException += (s, e) =>
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "crash.log"),
                    $"{e.Message}\r\n{e.Exception}");
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "crash_domain.log"),
                    $"{e.ExceptionObject}");
            };

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
            System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "startup.log"),
                $"[{DateTime.Now:HH:mm:ss.fff}] OnLaunched start\r\n");

            try
            {
                MainWindowInstance = new MainWindow();
                System.IO.File.AppendAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "startup.log"),
                    $"[{DateTime.Now:HH:mm:ss.fff}] MainWindow created, activating...\r\n");

                MainWindowInstance.Activate();
                System.IO.File.AppendAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "startup.log"),
                    $"[{DateTime.Now:HH:mm:ss.fff}] MainWindow activated!\r\n");
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "startup.log"),
                    $"[{DateTime.Now:HH:mm:ss.fff}] Exception: {ex}\r\n");
                throw;
            }
        }
    }
}
