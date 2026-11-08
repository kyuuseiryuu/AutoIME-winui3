using System;
using System.Text;
using AutoIME.Helpers;

namespace AutoIME.Services
{
    public class HotkeyService
    {
        public const int HOTKEY_ID = 9001;

        private IntPtr _hwnd = IntPtr.Zero;
        private IntPtr _oldWndProc = IntPtr.Zero;
        private Win32Native.WndProcDelegate? _wndProcDelegate;

        public event Action? HotkeyPressed;

        public void Initialize(IntPtr hwnd)
        {
            _hwnd = hwnd;
            _wndProcDelegate = WndProc;
            _oldWndProc = Win32Native.SetWindowLongPtr(
                _hwnd,
                Win32Native.GWLP_WNDPROC,
                System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
        }

        public void Cleanup()
        {
            Unregister();
            if (_hwnd != IntPtr.Zero && _oldWndProc != IntPtr.Zero)
            {
                Win32Native.SetWindowLongPtr(_hwnd, Win32Native.GWLP_WNDPROC, _oldWndProc);
                _oldWndProc = IntPtr.Zero;
            }
            _wndProcDelegate = null;
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == Win32Native.WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke();
                return IntPtr.Zero;
            }

            return Win32Native.CallNextHookEx(IntPtr.Zero, 0, wParam, lParam);
        }

        public bool Register(string hotkeyStr)
        {
            if (_hwnd == IntPtr.Zero)
                return false;

            Unregister();

            if (string.IsNullOrWhiteSpace(hotkeyStr))
                return false;

            if (ParseHotkey(hotkeyStr, out uint modifiers, out uint vk))
            {
                return Win32Native.RegisterHotKey(_hwnd, HOTKEY_ID, modifiers | Win32Native.MOD_NOREPEAT, vk);
            }

            return false;
        }

        public void Unregister()
        {
            if (_hwnd != IntPtr.Zero)
            {
                Win32Native.UnregisterHotKey(_hwnd, HOTKEY_ID);
            }
        }

        public static bool ParseHotkey(string ahkHk, out uint modifiers, out uint vk)
        {
            modifiers = 0;
            vk = 0;

            if (string.IsNullOrWhiteSpace(ahkHk))
                return false;

            int i = 0;
            while (i < ahkHk.Length)
            {
                char c = ahkHk[i];
                if (c == '^')
                {
                    modifiers |= Win32Native.MOD_CONTROL;
                    i++;
                }
                else if (c == '!')
                {
                    modifiers |= Win32Native.MOD_ALT;
                    i++;
                }
                else if (c == '+')
                {
                    modifiers |= Win32Native.MOD_SHIFT;
                    i++;
                }
                else if (c == '#')
                {
                    modifiers |= Win32Native.MOD_WIN;
                    i++;
                }
                else
                {
                    break;
                }
            }

            string keyPart = ahkHk[i..].Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(keyPart))
                return false;

            if (keyPart.Length == 1 && keyPart[0] >= 'A' && keyPart[0] <= 'Z')
            {
                vk = (uint)keyPart[0];
                return true;
            }
            if (keyPart.Length == 1 && keyPart[0] >= '0' && keyPart[0] <= '9')
            {
                vk = (uint)keyPart[0];
                return true;
            }

            if (keyPart.StartsWith("F") && int.TryParse(keyPart[1..], out int fNum) && fNum >= 1 && fNum <= 24)
            {
                vk = (uint)(0x70 + (fNum - 1)); // VK_F1 = 0x70
                return true;
            }

            // Other keys
            vk = keyPart switch
            {
                "SPACE" => 0x20,
                "TAB" => 0x09,
                "ENTER" => 0x0D,
                "ESC" or "ESCAPE" => 0x1B,
                "BACKSPACE" => 0x08,
                "INSERT" => 0x2D,
                "DELETE" or "DEL" => 0x2E,
                "HOME" => 0x24,
                "END" => 0x23,
                "PGUP" => 0x21,
                "PGDN" => 0x22,
                "`" or "~" => 0xC0,
                _ => 0
            };

            return vk != 0;
        }

        public static string FormatDisplay(string ahkHk)
        {
            if (string.IsNullOrWhiteSpace(ahkHk))
                return string.Empty;

            var sb = new StringBuilder();
            int i = 0;
            bool hasCtrl = false, hasAlt = false, hasShift = false, hasWin = false;

            while (i < ahkHk.Length)
            {
                char c = ahkHk[i];
                if (c == '^') { hasCtrl = true; i++; }
                else if (c == '!') { hasAlt = true; i++; }
                else if (c == '+') { hasShift = true; i++; }
                else if (c == '#') { hasWin = true; i++; }
                else break;
            }

            if (hasCtrl) sb.Append("Ctrl + ");
            if (hasAlt) sb.Append("Alt + ");
            if (hasShift) sb.Append("Shift + ");
            if (hasWin) sb.Append("Win + ");

            string keyPart = ahkHk[i..].Trim().ToUpperInvariant();
            sb.Append(keyPart);

            return sb.ToString();
        }
    }
}

