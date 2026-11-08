using System;
using System.Runtime.InteropServices;
using AutoIME.Helpers;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace AutoIME.Views
{
    public sealed partial class ToolTipFloatingWindow : Window
    {
        private readonly DispatcherTimer _hideTimer;
        private readonly AppWindow _appWindow;
        private readonly IntPtr _hwnd;

        public ToolTipFloatingWindow()
        {
            this.InitializeComponent();

            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            // Hide from Alt+Tab and taskbar
            _appWindow.IsShownInSwitchers = false;

            if (_appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
                presenter.IsMinimizable = false;
                presenter.SetBorderAndTitleBar(false, false);
                presenter.IsAlwaysOnTop = true;
            }

            _hideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1800)
            };
            _hideTimer.Tick += (s, e) =>
            {
                _hideTimer.Stop();
                Win32Native.ShowWindow(_hwnd, Win32Native.SW_HIDE);
            };
        }

        public void ShowTip(string title, string subtitle, bool matched)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                TxtTitle.Text = title;
                TxtSubtitle.Text = subtitle;

                if (matched)
                {
                    StateIcon.Glyph = "\uE73E"; // Checkmark
                    StateIcon.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
                }
                else
                {
                    StateIcon.Glyph = "\uE946"; // Info
                    StateIcon.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorAttentionBrush"];
                }

                // Position in bottom-right corner of display
                var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
                int width = 340;
                int height = 76;
                int x = displayArea.WorkArea.X + displayArea.WorkArea.Width - width - 24;
                int y = displayArea.WorkArea.Y + displayArea.WorkArea.Height - height - 24;

                _appWindow.Resize(new SizeInt32(width, height));
                _appWindow.Move(new PointInt32(x, y));

                Win32Native.ShowWindow(_hwnd, Win32Native.SW_SHOW);

                _hideTimer.Stop();
                _hideTimer.Start();
            });
        }
    }
}

