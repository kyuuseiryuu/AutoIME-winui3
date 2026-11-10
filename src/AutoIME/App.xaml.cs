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

        private static void SafeLog(string filename, string content)
        {
            try
            {
                string dir = AppContext.BaseDirectory;
                string targetPath = System.IO.Path.Combine(dir, filename);
                try
                {
                    System.IO.File.AppendAllText(targetPath, content);
                    return;
                }
                catch
                {
                    string appDataDir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "AutoIME");
                    if (!System.IO.Directory.Exists(appDataDir))
                        System.IO.Directory.CreateDirectory(appDataDir);
                    System.IO.File.AppendAllText(System.IO.Path.Combine(appDataDir, filename), content);
                }
            }
            catch { }
        }

        public App()
        {
            this.InitializeComponent();

            this.UnhandledException += (s, e) =>
            {
                SafeLog("crash.log", $"{e.Message}\r\n{e.Exception}\r\n");
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                SafeLog("crash_domain.log", $"{e.ExceptionObject}\r\n");
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
            SafeLog("startup.log", $"[{DateTime.Now:HH:mm:ss.fff}] OnLaunched start\r\n");

            try
            {
                MainWindowInstance = new MainWindow();
                SafeLog("startup.log", $"[{DateTime.Now:HH:mm:ss.fff}] MainWindow created, activating...\r\n");

                MainWindowInstance.Activate();
                SafeLog("startup.log", $"[{DateTime.Now:HH:mm:ss.fff}] MainWindow activated!\r\n");
            }
            catch (Exception ex)
            {
                SafeLog("startup.log", $"[{DateTime.Now:HH:mm:ss.fff}] Exception: {ex}\r\n");
                throw;
            }
        }
    }
}
