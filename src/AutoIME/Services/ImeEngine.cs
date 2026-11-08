using System;
using System.Collections.Generic;
using System.Text;
using AutoIME.Helpers;
using AutoIME.Models;
using Microsoft.Win32;

namespace AutoIME.Services
{
    public static class ImeEngine
    {
        public static ImeInfo GetImeDetails(IntPtr hkl)
        {
            ulong hklVal = (ulong)hkl.ToInt64() & 0xFFFFFFFF;
            string hklHex = $"0x{hklVal:X8}";
            uint langId = (uint)(hklVal & 0xFFFF);
            uint layoutId = (uint)((hklVal >> 16) & 0xFFFF);
            string klid = $"{(layoutId != 0 ? layoutId : langId):X8}";

            string desc = string.Empty;
            uint descLen = Win32Native.ImmGetDescriptionW(hkl, null, 0);
            if (descLen > 0)
            {
                var sb = new StringBuilder((int)descLen + 2);
                Win32Native.ImmGetDescriptionW(hkl, sb, descLen + 1);
                desc = sb.ToString();
            }

            string resolvedLayout = string.Empty;
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts\{klid}");
                if (key != null)
                {
                    var regDisplayName = key.GetValue("Layout Display Name") as string;
                    if (!string.IsNullOrEmpty(regDisplayName))
                    {
                        var resBuf = new StringBuilder(512);
                        if (Win32Native.SHLoadIndirectString(regDisplayName, resBuf, 256, IntPtr.Zero) == 0)
                        {
                            resolvedLayout = resBuf.ToString();
                        }
                    }

                    if (string.IsNullOrEmpty(resolvedLayout))
                    {
                        resolvedLayout = (key.GetValue("Layout Text") as string) ?? string.Empty;
                    }
                }
            }
            catch { }

            string nativeLang = string.Empty;
            var nameBuf = new StringBuilder(512);
            if (Win32Native.GetLocaleInfoW(langId, Win32Native.LOCALE_SNATIVELANGNAME, nameBuf, 256) > 0)
            {
                nativeLang = nameBuf.ToString();
            }
            if (string.IsNullOrEmpty(nativeLang))
            {
                nameBuf.Clear();
                if (Win32Native.GetLocaleInfoW(langId, Win32Native.LOCALE_SENGLANGUAGE, nameBuf, 256) > 0)
                {
                    nativeLang = nameBuf.ToString();
                }
            }

            string displayName;
            if (!string.IsNullOrEmpty(desc))
            {
                displayName = desc;
            }
            else if (!string.IsNullOrEmpty(resolvedLayout))
            {
                if (!string.IsNullOrEmpty(nativeLang) && resolvedLayout.Contains(nativeLang, StringComparison.OrdinalIgnoreCase))
                {
                    displayName = resolvedLayout;
                }
                else
                {
                    displayName = string.IsNullOrEmpty(nativeLang)
                        ? resolvedLayout
                        : $"{nativeLang} ({resolvedLayout})";
                }
            }
            else
            {
                displayName = string.IsNullOrEmpty(nativeLang)
                    ? hklHex
                    : $"{nativeLang} ({hklHex})";
            }

            displayName = displayName.Replace("\r", "").Replace("\n", "").Replace("\t", "").Trim();

            // Friendly canonical replacements for common layouts
            if (langId == 0x0804 && displayName.Contains("美式键盘"))
            {
                displayName = "中文(简体) - 微软拼音";
            }
            else if (langId == 0x0409 && (displayName == "美式键盘" || displayName.Contains("美式键盘")))
            {
                displayName = "英语(美国) - 美式键盘";
            }

            return new ImeInfo(hkl, hklHex, langId, displayName);
        }

        public static List<ImeInfo> GetSystemImeList()
        {
            var list = new List<ImeInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int count = Win32Native.GetKeyboardLayoutList(0, null);
            if (count > 0)
            {
                var hkls = new IntPtr[count];
                Win32Native.GetKeyboardLayoutList(count, hkls);
                foreach (var hkl in hkls)
                {
                    ulong val = (ulong)hkl.ToInt64() & 0xFFFFFFFF;
                    string hex = $"0x{val:X8}";
                    if (!seen.Add(hex))
                        continue;

                    list.Add(GetImeDetails(hkl));
                }
            }

            // Also check HKCU\Keyboard Layout\Preload
            try
            {
                using var preloadKey = Registry.CurrentUser.OpenSubKey(@"Keyboard Layout\Preload");
                if (preloadKey != null)
                {
                    var valueNames = preloadKey.GetValueNames();
                    foreach (var name in valueNames)
                    {
                        var val = preloadKey.GetValue(name) as string;
                        if (string.IsNullOrWhiteSpace(val))
                            continue;

                        if (uint.TryParse(val, System.Globalization.NumberStyles.HexNumber, null, out uint klidInt))
                        {
                            if ((klidInt >> 16) == 0)
                            {
                                klidInt = (klidInt << 16) | klidInt;
                            }

                            string hex = $"0x{klidInt:X8}";
                            if (!seen.Add(hex))
                                continue;

                            list.Add(GetImeDetails((IntPtr)klidInt));
                        }
                    }
                }
            }
            catch { }

            return list;
        }

        public static void SwitchToIme(IntPtr targetHkl, IntPtr targetHwnd)
        {
            try
            {
                if (targetHwnd == IntPtr.Zero)
                    targetHwnd = Win32Native.GetForegroundWindow();

                if (targetHwnd == IntPtr.Zero)
                    return;

                // 1. Send WM_INPUTLANGCHANGEREQUEST (0x0050)
                Win32Native.PostMessage(targetHwnd, Win32Native.WM_INPUTLANGCHANGEREQUEST, (IntPtr)1, targetHkl);

                // 2. Adjust IMM32 open/close state if Chinese or English
                uint langId = (uint)((ulong)targetHkl.ToInt64() & 0xFFFF);
                if (langId == 0x0804)
                {
                    SetImeOpenStatus(targetHwnd, true);
                }
                else if (langId == 0x0409)
                {
                    SetImeOpenStatus(targetHwnd, false);
                }
            }
            catch { }
        }

        public static void SetImeOpenStatus(IntPtr hwnd, bool open)
        {
            try
            {
                if (hwnd == IntPtr.Zero)
                    hwnd = Win32Native.GetForegroundWindow();

                if (hwnd == IntPtr.Zero)
                    return;

                IntPtr imeHwnd = Win32Native.ImmGetDefaultIMEWnd(hwnd);
                if (imeHwnd == IntPtr.Zero)
                    return;

                Win32Native.SendMessage(
                    imeHwnd,
                    Win32Native.WM_IME_CONTROL,
                    (IntPtr)Win32Native.IMC_SETOPENSTATUS,
                    (IntPtr)(open ? 1 : 0));
            }
            catch { }
        }
    }
}
