using System;
using AutoIME.Helpers;
using AutoIME.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace AutoIME.Views
{
    public sealed partial class CaptureOverlayWindow : Window
    {
        private readonly AppWindow _appWindow;
        private readonly IntPtr _hwnd;
        private int _lastX = -1;
        private int _lastY = -1;

        public CaptureOverlayWindow()
        {
            this.InitializeComponent();

            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            // Hide from Alt+Tab and taskbar using Win32 ToolWindow style
            try
            {
                const int GWL_EXSTYLE = -20;
                const int WS_EX_TOOLWINDOW = 0x00000080;
                long exStyle = Win32Native.GetWindowLongPtr(_hwnd, GWL_EXSTYLE).ToInt64();
                Win32Native.SetWindowLongPtr(_hwnd, GWL_EXSTYLE, (IntPtr)(exStyle | WS_EX_TOOLWINDOW));
            }
            catch { }

            if (_appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
                presenter.SetBorderAndTitleBar(false, false);
                presenter.IsAlwaysOnTop = true;
            }

            _appWindow.Resize(new SizeInt32(370, 210));
        }

        public void ShowOverlay(string targetImeName)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                var loc = LocalizationService.Instance;
                TxtHeader.Text = loc["capture_mode_title"];
                TxtTargetIme.Text = loc["capture_target_ime", targetImeName];
                TxtProc.Text = loc["capture_scanning"];
                TxtTitle.Text = "";
                TxtHintConfirm.Text = loc["capture_hint_confirm"];
                TxtHintCancel.Text = loc["capture_hint_cancel"];

                Win32Native.ShowWindow(_hwnd, Win32Native.SW_SHOW);
            });
        }

        public void HideOverlay()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                Win32Native.ShowWindow(_hwnd, Win32Native.SW_HIDE);
            });
        }

        public void UpdateInfo(string targetIme, string proc, string title, int cursorX, int cursorY)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                var loc = LocalizationService.Instance;
                TxtTargetIme.Text = loc["capture_target_ime", targetIme];
                TxtProc.Text = loc["capture_hover_proc", string.IsNullOrEmpty(proc) ? loc["capture_scanning"] : proc];
                TxtTitle.Text = loc["capture_hover_title", string.IsNullOrEmpty(title) ? loc["capture_no_title"] : title];

                // Avoid positioning off-screen
                int targetX = cursorX + 20;
                int targetY = cursorY + 20;

                var displayArea = DisplayArea.GetFromPoint(new PointInt32(cursorX, cursorY), DisplayAreaFallback.Primary);
                if (displayArea != null)
                {
                    if (targetX + 370 > displayArea.WorkArea.X + displayArea.WorkArea.Width)
                    {
                        targetX = cursorX - 380;
                    }
                    if (targetY + 210 > displayArea.WorkArea.Y + displayArea.WorkArea.Height)
                    {
                        targetY = cursorY - 220;
                    }
                }

                if (targetX != _lastX || targetY != _lastY)
                {
                    _lastX = targetX;
                    _lastY = targetY;
                    _appWindow.Move(new PointInt32(targetX, targetY));
                }
            });
        }
    }
}

