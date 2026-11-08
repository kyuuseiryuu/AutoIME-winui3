using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using AutoIME.Helpers;

namespace AutoIME.Services
{
    public class WindowCaptureResult
    {
        public bool Success { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public string WindowTitle { get; set; } = string.Empty;
        public IntPtr Hwnd { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class WindowCaptureService
    {
        private static WindowCaptureService? _instance;
        public static WindowCaptureService Instance => _instance ??= new WindowCaptureService();

        private IntPtr _hMouseHook = IntPtr.Zero;
        private IntPtr _hKeyboardHook = IntPtr.Zero;
        private Win32Native.HookProc? _mouseProc;
        private Win32Native.HookProc? _keyboardProc;
        private Timer? _trackingTimer;

        private IntPtr _ownHwnd = IntPtr.Zero;
        private bool _isCapturing = false;
        private string _targetImeName = string.Empty;

        public event Action<string, string, int, int>? HoverUpdated; // process, title, cursorX, cursorY
        public event Action<WindowCaptureResult>? CaptureCompleted;

        public bool IsCapturing => _isCapturing;

        public void StartCapture(IntPtr ownHwnd, string targetImeName)
        {
            if (_isCapturing)
                return;

            _isCapturing = true;
            _ownHwnd = ownHwnd;
            _targetImeName = targetImeName;

            _mouseProc = MouseHookCallback;
            _keyboardProc = KeyboardHookCallback;

            IntPtr hMod = IntPtr.Zero;

            _hMouseHook = Win32Native.SetWindowsHookEx(
                Win32Native.WH_MOUSE_LL,
                _mouseProc,
                hMod,
                0);

            _hKeyboardHook = Win32Native.SetWindowsHookEx(
                Win32Native.WH_KEYBOARD_LL,
                _keyboardProc,
                hMod,
                0);

            // Timer to track cursor and hover info every 40ms
            _trackingTimer = new Timer(OnTrackingTick, null, 0, 40);
        }

        public void StopCapture()
        {
            if (!_isCapturing)
                return;

            _isCapturing = false;

            _trackingTimer?.Dispose();
            _trackingTimer = null;

            if (_hMouseHook != IntPtr.Zero)
            {
                Win32Native.UnhookWindowsHookEx(_hMouseHook);
                _hMouseHook = IntPtr.Zero;
            }

            if (_hKeyboardHook != IntPtr.Zero)
            {
                Win32Native.UnhookWindowsHookEx(_hKeyboardHook);
                _hKeyboardHook = IntPtr.Zero;
            }

            _mouseProc = null;
            _keyboardProc = null;
        }

        private void OnTrackingTick(object? state)
        {
            if (!_isCapturing)
                return;

            if (Win32Native.GetCursorPos(out var pt))
            {
                IntPtr hwnd = Win32Native.WindowFromPoint(pt);
                if (hwnd != IntPtr.Zero)
                {
                    hwnd = GetRootAncestor(hwnd);
                    Win32Native.GetWindowThreadProcessId(hwnd, out uint pid);
                    string proc = WindowWatcherService.GetProcessName(pid);
                    string title = WindowWatcherService.GetWindowTitle(hwnd);

                    HoverUpdated?.Invoke(proc, title, pt.X, pt.Y);
                }
            }
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _isCapturing)
            {
                uint msg = (uint)wParam.ToInt64();
                if (msg == Win32Native.WM_LBUTTONDOWN)
                {
                    ThreadPool.QueueUserWorkItem(_ => ConfirmCapture());
                    return (IntPtr)1; // Consume click
                }
                else if (msg == Win32Native.WM_RBUTTONDOWN)
                {
                    ThreadPool.QueueUserWorkItem(_ => CancelCapture());
                    return (IntPtr)1;
                }
            }
            return Win32Native.CallNextHookEx(_hMouseHook, nCode, wParam, lParam);
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _isCapturing)
            {
                uint msg = (uint)wParam.ToInt64();
                if (msg == Win32Native.WM_KEYDOWN || msg == Win32Native.WM_SYSKEYDOWN)
                {
                    var kbd = Marshal.PtrToStructure<Win32Native.KBDLLHOOKSTRUCT>(lParam);
                    if (kbd.vkCode == 0x1B) // ESC
                    {
                        ThreadPool.QueueUserWorkItem(_ => CancelCapture());
                        return (IntPtr)1;
                    }
                    else if (kbd.vkCode == 0x0D || kbd.vkCode == 0x20 || kbd.vkCode == 0x77) // Enter, Space, F8
                    {
                        ThreadPool.QueueUserWorkItem(_ => ConfirmCapture());
                        return (IntPtr)1;
                    }
                }
            }
            return Win32Native.CallNextHookEx(_hKeyboardHook, nCode, wParam, lParam);
        }

        private void ConfirmCapture()
        {
            Win32Native.GetCursorPos(out var pt);
            IntPtr hwnd = Win32Native.WindowFromPoint(pt);
            if (hwnd != IntPtr.Zero)
            {
                hwnd = GetRootAncestor(hwnd);
            }

            StopCapture();

            if (hwnd == IntPtr.Zero)
            {
                CaptureCompleted?.Invoke(new WindowCaptureResult
                {
                    Success = false,
                    ErrorMessage = "capture_invalid"
                });
                return;
            }

            if (hwnd == _ownHwnd)
            {
                CaptureCompleted?.Invoke(new WindowCaptureResult
                {
                    Success = false,
                    ErrorMessage = "capture_self_window"
                });
                return;
            }

            Win32Native.GetWindowThreadProcessId(hwnd, out uint pid);
            string proc = WindowWatcherService.GetProcessName(pid);
            string title = WindowWatcherService.GetWindowTitle(hwnd);

            if (string.IsNullOrEmpty(proc))
            {
                CaptureCompleted?.Invoke(new WindowCaptureResult
                {
                    Success = false,
                    ErrorMessage = "capture_failed_proc"
                });
                return;
            }

            CaptureCompleted?.Invoke(new WindowCaptureResult
            {
                Success = true,
                Hwnd = hwnd,
                ProcessName = proc,
                WindowTitle = title
            });
        }

        private void CancelCapture()
        {
            StopCapture();
            CaptureCompleted?.Invoke(new WindowCaptureResult
            {
                Success = false,
                ErrorMessage = "capture_cancelled"
            });
        }

        private static IntPtr GetRootAncestor(IntPtr hwnd)
        {
            [DllImport("user32.dll")]
            static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
            const uint GA_ROOT = 2;
            IntPtr root = GetAncestor(hwnd, GA_ROOT);
            return root != IntPtr.Zero ? root : hwnd;
        }
    }
}

