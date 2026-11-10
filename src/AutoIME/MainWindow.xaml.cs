using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using AutoIME.Helpers;
using AutoIME.Models;
using AutoIME.Services;
using AutoIME.ViewModels;
using AutoIME.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using WinUIEx;

namespace AutoIME
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; } = new();
        public CommunityToolkit.Mvvm.Input.IRelayCommand OpenWindowCommand => new CommunityToolkit.Mvvm.Input.RelayCommand(() => OnTrayOpenClicked(null, null));
        public CommunityToolkit.Mvvm.Input.IRelayCommand ReloadCommand => new CommunityToolkit.Mvvm.Input.RelayCommand(DoReload);
        public CommunityToolkit.Mvvm.Input.IRelayCommand ToggleDebugCommand => new CommunityToolkit.Mvvm.Input.RelayCommand(DoToggleDebug);
        public CommunityToolkit.Mvvm.Input.IRelayCommand ToggleStartupCommand => new CommunityToolkit.Mvvm.Input.RelayCommand(DoToggleStartup);
        public CommunityToolkit.Mvvm.Input.IRelayCommand RestartCommand => new CommunityToolkit.Mvvm.Input.RelayCommand(DoRestart);
        public CommunityToolkit.Mvvm.Input.IRelayCommand ExitCommand => new CommunityToolkit.Mvvm.Input.RelayCommand(DoExit);

        public Visibility GetEmptyVisibility(int count) => count == 0 ? Visibility.Visible : Visibility.Collapsed;
        public string GetSelectedCountText(int count) => ViewModel.Loc["selected_count", count];
        public bool IsGreaterThanZero(int count) => count > 0;
        public InfoBarSeverity GetSeverity(string sev) => sev switch
        {
            "Success" => InfoBarSeverity.Success,
            "Warning" => InfoBarSeverity.Warning,
            "Error" => InfoBarSeverity.Error,
            _ => InfoBarSeverity.Informational
        };

        private readonly IntPtr _hwnd;
        private readonly AppWindow _appWindow;
        private readonly HotkeyService _hotkeyService = new();
        private ToolTipFloatingWindow? _tooltipWindow;
        private CaptureOverlayWindow? _captureOverlay;
        private bool _isExplicitExit = false;

        private static void Log(string msg)
        {
            try
            {
                string dir = AppContext.BaseDirectory;
                string target = System.IO.Path.Combine(dir, "startup.log");
                try
                {
                    System.IO.File.AppendAllText(target, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\r\n");
                    return;
                }
                catch
                {
                    string appDataDir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "AutoIME");
                    if (!System.IO.Directory.Exists(appDataDir))
                        System.IO.Directory.CreateDirectory(appDataDir);
                    System.IO.File.AppendAllText(System.IO.Path.Combine(appDataDir, "startup.log"),
                        $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\r\n");
                }
            }
            catch { }
        }

        public MainWindow()
        {
            Log("MainWindow ctor start");
            this.InitializeComponent();
            Log("InitializeComponent done");

            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);
            Log($"Window handle: {_hwnd}");

            // Configure Window
            this.SetWindowSize(940, 700);
            this.CenterOnScreen();
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);
            Log("Window size and titlebar configured");

            try
            {
                this.SystemBackdrop = new MicaBackdrop();
                Log("MicaBackdrop set");
            }
            catch (Exception ex) { Log($"Mica error: {ex.Message}"); }

            try
            {
                this.SetIcon("Assets/AppIcon.ico");
                string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    TrayIcon.Icon = new System.Drawing.Icon(iconPath);
                }
                Log("Icon set");
            }
            catch (Exception ex) { Log($"Icon error: {ex.Message}"); }

            // Initialize Floating Windows
            Log("Creating ToolTipFloatingWindow");
            _tooltipWindow = new ToolTipFloatingWindow();
            Log("Creating CaptureOverlayWindow");
            _captureOverlay = new CaptureOverlayWindow();
            Log("Floating windows created");

            // Setup Hotkey
            Log("Initializing HotkeyService");
            _hotkeyService.Initialize(_hwnd);
            _hotkeyService.HotkeyPressed += OnGlobalHotkeyPressed;
            _hotkeyService.Register(ViewModel.Settings.Hotkey);
            Log("HotkeyService initialized");

            // Setup Window Watcher
            Log("Initializing WindowWatcherService");
            WindowWatcherService.Instance.WindowSwitched += OnWindowSwitched;
            WindowWatcherService.Instance.Start();
            Log("WindowWatcherService started");

            // Setup Capture Service events
            WindowCaptureService.Instance.HoverUpdated += OnCaptureHoverUpdated;
            WindowCaptureService.Instance.CaptureCompleted += OnCaptureFinished;

            // Setup ViewModel Conflict Callback
            ViewModel.ConflictConfirmRequested = OnConflictConfirmRequestedAsync;

            // Handle Window Close -> Minimize to tray
            _appWindow.Closing += OnWindowClosing;

            // Initialize UI States
            ApplyTheme(ViewModel.Settings.Theme);
            SyncUIFromSettings();
            ApplyLanguageUI();
            Log("MainWindow ctor complete");
        }

        private void SyncUIFromSettings()
        {
            // Sync theme combobox
            foreach (ComboBoxItem item in ThemeComboBox.Items)
            {
                if (item.Tag?.ToString() == ViewModel.Settings.Theme)
                {
                    ThemeComboBox.SelectedItem = item;
                    break;
                }
            }

            // Sync lang combobox
            foreach (ComboBoxItem item in LangComboBox.Items)
            {
                if (item.Tag?.ToString() == ViewModel.Settings.Language)
                {
                    LangComboBox.SelectedItem = item;
                    break;
                }
            }

            // Sync tray checks
            TrayDebugItem.IsChecked = ViewModel.Settings.DebugMode;
            TrayStartupItem.IsChecked = StartupService.IsAutoStartEnabled();
        }

        private void ApplyLanguageUI()
        {
            var loc = ViewModel.Loc;
            Title = loc["app_title"];
            TxtAppTitle.Text = loc["header_title"];
            TxtBtnCapture.Text = loc["btn_capture"].Replace("🎯 ", "");
            TxtBtnBrowse.Text = loc["btn_browse"].Replace("📁 ", "");
            TxtBtnManual.Text = loc["btn_manual"].Replace("➕ ", "");
            SearchBox.PlaceholderText = loc["search_placeholder"];

            TxtColExe.Text = loc["col_exe"];
            TxtColDesc.Text = loc["col_desc"];
            TxtColAction.Text = loc["col_action"];

            TxtBtnDeleteSelected.Text = loc["btn_delete"].Replace("❌ ", "");
            TxtBtnClearTab.Text = loc["btn_clear"].Replace("🗑️ ", "");

            ChkDebugMode.Content = loc["chk_debug"];
            ChkAutoStart.Content = loc["chk_startup"];
            TxtBtnRefreshIme.Text = loc["btn_refresh_ime"].Replace("🔄 ", "");

            ThemeItemSystem.Content = loc["theme_system"];
            ThemeItemLight.Content = loc["theme_light"];
            ThemeItemDark.Content = loc["theme_dark"];

            TrayOpenItem.Text = loc["tray_open_config", ViewModel.FormattedHotkey];
            TrayReloadItem.Text = loc["tray_reload_config"];
            TrayDebugItem.Text = loc["tray_toggle_debug"];
            TrayStartupItem.Text = loc["tray_toggle_startup"];
            TrayRestartItem.Text = loc["tray_restart"];
            TrayExitItem.Text = loc["tray_exit"];
        }

        private void ApplyTheme(string theme)
        {
            var elementTheme = theme switch
            {
                "light" => ElementTheme.Light,
                "dark" => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            if (Content is FrameworkElement fe)
            {
                fe.RequestedTheme = elementTheme;
            }
        }

        private void OnWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (!_isExplicitExit)
            {
                args.Cancel = true;
                Win32Native.ShowWindow(_hwnd, Win32Native.SW_HIDE);
                TrayIcon.ShowNotification(
                    "AutoIME",
                    ViewModel.Loc["tray_tip_minimized"]);
            }
        }

        private void OnGlobalHotkeyPressed()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (Win32Native.IsWindowVisible(_hwnd))
                {
                    Win32Native.ShowWindow(_hwnd, Win32Native.SW_HIDE);
                }
                else
                {
                    this.Show();
                    Win32Native.SetForegroundWindow(_hwnd);
                }
            });
        }

        private void OnWindowSwitched(object? sender, WindowSwitchEventArgs e)
        {
            if (ViewModel.Settings.DebugMode && _tooltipWindow != null)
            {
                string title = e.Matched
                    ? $"【命中规则】{e.ProcessName}"
                    : $"【未配置】{e.ProcessName}";

                string sub = e.Matched
                    ? $"切换至: {e.TargetImeName}"
                    : (string.IsNullOrEmpty(e.WindowTitle) ? "无窗口标题" : e.WindowTitle);

                _tooltipWindow.ShowTip(title, sub, e.Matched);
            }
        }

        // ==============================================================================
        // Toolbar Actions
        // ==============================================================================

        private void OnImeTabClicked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton tb && tb.Content is ImeInfo ime)
            {
                ViewModel.SelectedIme = ime;

                // Refresh visual check of tabs
                foreach (var child in ImeTabsControl.Items)
                {
                    // Triggers converter re-evaluation
                }
            }
        }

        private void OnItemCheckBoxClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.UpdateSelectionCount();
        }

        private void OnSelectAllClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleSelectAllCommand.Execute(ViewModel.IsAllSelected);
        }

        private void OnCaptureWindowClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedIme == null)
                return;

            Win32Native.ShowWindow(_hwnd, Win32Native.SW_HIDE);
            _captureOverlay?.ShowOverlay(ViewModel.SelectedIme.DisplayName);
            WindowCaptureService.Instance.StartCapture(_hwnd, ViewModel.SelectedIme.DisplayName);
        }

        private void OnCaptureHoverUpdated(string proc, string title, int x, int y)
        {
            _captureOverlay?.UpdateInfo(ViewModel.SelectedIme?.DisplayName ?? "", proc, title, x, y);
        }

        private void OnCaptureFinished(WindowCaptureResult result)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                _captureOverlay?.HideOverlay();
                this.Show();
                Win32Native.SetForegroundWindow(_hwnd);

                if (result.Success)
                {
                    await ViewModel.AddAppToCurrentImeAsync(result.ProcessName, result.WindowTitle);
                }
                else if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    ViewModel.ShowStatus(ViewModel.Loc[result.ErrorMessage], "Warning");
                }
            });
        }

        private async void OnBrowseExeClick(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, _hwnd);
            picker.ViewMode = PickerViewMode.List;
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add(".exe");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                string exeName = file.Name.ToLowerInvariant();
                string remark = string.Empty;

                try
                {
                    var versionInfo = FileVersionInfo.GetVersionInfo(file.Path);
                    remark = versionInfo.FileDescription ?? Path.GetFileNameWithoutExtension(file.Name);
                }
                catch
                {
                    remark = Path.GetFileNameWithoutExtension(file.Name);
                }

                await ViewModel.AddAppToCurrentImeAsync(exeName, remark);
            }
        }

        private async void OnAddManualClick(object sender, RoutedEventArgs e)
        {
            var dlg = new ManualAddDialog
            {
                XamlRoot = this.Content.XamlRoot
            };

            var res = await dlg.ShowAsync();
            if (res == ContentDialogResult.Primary && !string.IsNullOrEmpty(dlg.ExeName))
            {
                await ViewModel.AddAppToCurrentImeAsync(dlg.ExeName, dlg.Remark);
            }
        }

        private void OnDeleteSingleItemClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is AppRule rule)
            {
                ViewModel.DeleteRuleCommand.Execute(rule);
            }
        }

        private void OnDeleteSelectedClick(object sender, RoutedEventArgs e)
        {
            ViewModel.DeleteSelectedCommand.Execute(null);
        }

        private async void OnClearTabClick(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedIme == null || ViewModel.SelectedIme.Apps.Count == 0)
                return;

            var loc = ViewModel.Loc;
            var confirmDlg = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Title = loc["clear_title"],
                Content = loc["clear_confirm", ViewModel.SelectedIme.DisplayName],
                PrimaryButtonText = loc["hk_dialog_confirm"],
                CloseButtonText = loc["hk_dialog_cancel"],
                DefaultButton = ContentDialogButton.Close
            };

            if (await confirmDlg.ShowAsync() == ContentDialogResult.Primary)
            {
                ViewModel.ClearCurrentTabCommand.Execute(null);
            }
        }

        private async void OnHotkeyButtonClick(object sender, RoutedEventArgs e)
        {
            var dlg = new HotkeyDialog(ViewModel.Settings.Hotkey)
            {
                XamlRoot = this.Content.XamlRoot
            };

            if (await dlg.ShowAsync() == ContentDialogResult.Primary)
            {
                string newHk = dlg.RecordedHotkey;
                if (!string.IsNullOrEmpty(newHk))
                {
                    bool ok = _hotkeyService.Register(newHk);
                    if (ok)
                    {
                        ViewModel.UpdateHotkey(newHk);
                        ViewModel.ShowStatus(ViewModel.Loc["hotkey_updated", ViewModel.FormattedHotkey], "Success");
                    }
                    else
                    {
                        ViewModel.ShowStatus(ViewModel.Loc["hotkey_failed", newHk, "RegisterHotKey Failed"], "Error");
                    }
                }
            }
        }

        private void OnDebugModeClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleDebugMode(ChkDebugMode.IsChecked == true);
            TrayDebugItem.IsChecked = ViewModel.Settings.DebugMode;
        }

        private void OnAutoStartClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleAutoStart(ChkAutoStart.IsChecked == true);
            TrayStartupItem.IsChecked = StartupService.IsAutoStartEnabled();
        }

        private void OnRefreshImeClick(object sender, RoutedEventArgs e)
        {
            ViewModel.RefreshImesCommand.Execute(null);
        }

        private async Task<bool> OnConflictConfirmRequestedAsync(string title, string prompt)
        {
            var dlg = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Title = title,
                Content = prompt,
                PrimaryButtonText = ViewModel.Loc["hk_dialog_confirm"],
                CloseButtonText = ViewModel.Loc["hk_dialog_cancel"],
                DefaultButton = ContentDialogButton.Primary
            };

            var res = await dlg.ShowAsync();
            return res == ContentDialogResult.Primary;
        }

        private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string themeTag)
            {
                if (themeTag != ViewModel.Settings.Theme)
                {
                    ViewModel.UpdateTheme(themeTag);
                    ApplyTheme(themeTag);
                }
            }
        }

        private void OnLangSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LangComboBox.SelectedItem is ComboBoxItem item && item.Tag is string langTag)
            {
                if (langTag != ViewModel.Settings.Language)
                {
                    ViewModel.UpdateLanguage(langTag);
                    ApplyLanguageUI();
                }
            }
        }

        // ==============================================================================
        // Tray Menu Handlers
        // ==============================================================================

        private void OnTrayOpenClick(object sender, RoutedEventArgs e) => OnTrayOpenClicked(sender, e);

        public void OnTrayOpenClicked(object? sender, object? e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                this.Show();
                Win32Native.SetForegroundWindow(_hwnd);
            });
        }

        private void OnTrayReloadClicked(object sender, RoutedEventArgs e) => DoReload();
        private void DoReload()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ViewModel.InitializeData();
                ViewModel.ShowStatus(ViewModel.Loc["save_success"], "Success");
            });
        }

        private void OnTrayToggleDebugClicked(object sender, RoutedEventArgs e) => DoToggleDebug();
        private void DoToggleDebug()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ViewModel.ToggleDebugMode(!ViewModel.Settings.DebugMode);
                TrayDebugItem.IsChecked = ViewModel.Settings.DebugMode;
                ChkDebugMode.IsChecked = ViewModel.Settings.DebugMode;
            });
        }

        private void OnTrayToggleStartupClicked(object sender, RoutedEventArgs e) => DoToggleStartup();
        private void DoToggleStartup()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ViewModel.ToggleAutoStart(!ViewModel.Settings.AutoStart);
                TrayStartupItem.IsChecked = ViewModel.Settings.AutoStart;
                ChkAutoStart.IsChecked = ViewModel.Settings.AutoStart;
            });
        }

        private void OnTrayRestartClicked(object sender, RoutedEventArgs e) => DoRestart();
        private void DoRestart()
        {
            _isExplicitExit = true;
            try { WindowWatcherService.Instance.Stop(); } catch { }
            try { _hotkeyService.Cleanup(); } catch { }
            try { TrayIcon?.Dispose(); } catch { }
            App.ReleaseSingleInstanceMutex();

            string? exe = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exe))
            {
                Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
            }
            Environment.Exit(0);
        }

        private void OnTrayExitClicked(object sender, RoutedEventArgs e) => DoExit();
        private void DoExit()
        {
            _isExplicitExit = true;
            try { WindowWatcherService.Instance.Stop(); } catch { }
            try { _hotkeyService.Cleanup(); } catch { }
            try { TrayIcon?.Dispose(); } catch { }
            App.ReleaseSingleInstanceMutex();
            Environment.Exit(0);
        }
    }
}
