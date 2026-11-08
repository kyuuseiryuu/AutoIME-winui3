using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AutoIME.Helpers;

namespace AutoIME.Services
{
    public class WindowSwitchEventArgs : EventArgs
    {
        public IntPtr Hwnd { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string WindowTitle { get; set; } = string.Empty;
        public bool Matched { get; set; }
        public string TargetImeName { get; set; } = string.Empty;
        public IntPtr TargetHkl { get; set; }
    }

    public class WindowWatcherService
    {
        private static WindowWatcherService? _instance;
        public static WindowWatcherService Instance => _instance ??= new WindowWatcherService();

        private IntPtr _hHook = IntPtr.Zero;
        private Win32Native.WinEventDelegate? _procDelegate;
        private IntPtr _lastHwnd = IntPtr.Zero;
        private readonly uint _currentProcessId;

        public event EventHandler<WindowSwitchEventArgs>? WindowSwitched;

        // Map from lowercase exe name -> (HKL, DisplayName)
        private readonly ConcurrentDictionary<string, (IntPtr Hkl, string Name)> _ruleMap =
            new(StringComparer.OrdinalIgnoreCase);

        public WindowWatcherService()
        {
            _currentProcessId = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
        }

        public void UpdateRules(IEnumerable<Models.ImeInfo> imes)
        {
            _ruleMap.Clear();
            foreach (var ime in imes)
            {
                foreach (var app in ime.Apps)
                {
                    string exe = app.ExeName.Trim().ToLowerInvariant();
                    if (!string.IsNullOrEmpty(exe))
                    {
                        _ruleMap[exe] = (ime.Hkl, ime.DisplayName);
                    }
                }
            }
        }

        public void Start()
        {
            if (_hHook != IntPtr.Zero)
                return;

            _procDelegate = OnWinEvent;
            _hHook = Win32Native.SetWinEventHook(
                Win32Native.EVENT_SYSTEM_FOREGROUND,
                Win32Native.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                _procDelegate,
                0,
                0,
                Win32Native.WINEVENT_OUTOFCONTEXT | Win32Native.WINEVENT_SKIPOWNPROCESS);

            // Process current active window immediately
            IntPtr current = Win32Native.GetForegroundWindow();
            if (current != IntPtr.Zero)
            {
                ProcessWindow(current);
            }
        }

        public void Stop()
        {
            if (_hHook != IntPtr.Zero)
            {
                Win32Native.UnhookWinEvent(_hHook);
                _hHook = IntPtr.Zero;
            }
            _procDelegate = null;
        }

        private void OnWinEvent(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime)
        {
            if (eventType == Win32Native.EVENT_SYSTEM_FOREGROUND && hwnd != IntPtr.Zero)
            {
                ProcessWindow(hwnd);
            }
        }

        public void ProcessWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || hwnd == _lastHwnd)
                return;

            try
            {
                Win32Native.GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid == _currentProcessId || pid == 0)
                    return;

                _lastHwnd = hwnd;

                string procName = GetProcessName(pid);
                if (string.IsNullOrEmpty(procName))
                    return;

                string winTitle = GetWindowTitle(hwnd);

                if (_ruleMap.TryGetValue(procName, out var target))
                {
                    ImeEngine.SwitchToIme(target.Hkl, hwnd);
                    WindowSwitched?.Invoke(this, new WindowSwitchEventArgs
                    {
                        Hwnd = hwnd,
                        ProcessName = procName,
                        WindowTitle = winTitle,
                        Matched = true,
                        TargetHkl = target.Hkl,
                        TargetImeName = target.Name
                    });
                }
                else
                {
                    WindowSwitched?.Invoke(this, new WindowSwitchEventArgs
                    {
                        Hwnd = hwnd,
                        ProcessName = procName,
                        WindowTitle = winTitle,
                        Matched = false
                    });
                }
            }
            catch { }
        }

        public static string GetProcessName(uint pid)
        {
            IntPtr hProcess = Win32Native.OpenProcess(
                Win32Native.PROCESS_QUERY_LIMITED_INFORMATION,
                false,
                pid);

            if (hProcess == IntPtr.Zero)
            {
                hProcess = Win32Native.OpenProcess(
                    Win32Native.PROCESS_QUERY_INFORMATION,
                    false,
                    pid);
            }

            if (hProcess != IntPtr.Zero)
            {
                try
                {
                    var sb = new StringBuilder(1024);
                    int size = sb.Capacity;
                    if (Win32Native.QueryFullProcessImageName(hProcess, 0, sb, ref size))
                    {
                        return Path.GetFileName(sb.ToString()).ToLowerInvariant();
                    }
                }
                finally
                {
                    Win32Native.CloseHandle(hProcess);
                }
            }

            try
            {
                using var proc = System.Diagnostics.Process.GetProcessById((int)pid);
                return (proc.ProcessName + ".exe").ToLowerInvariant();
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string GetWindowTitle(IntPtr hwnd)
        {
            int length = Win32Native.GetWindowTextLength(hwnd);
            if (length > 0)
            {
                var sb = new StringBuilder(length + 2);
                Win32Native.GetWindowText(hwnd, sb, sb.Capacity);
                return sb.ToString();
            }
            return string.Empty;
        }
    }
}

