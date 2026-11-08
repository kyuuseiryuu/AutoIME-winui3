using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace AutoIME.Services
{
    public static class StartupService
    {
        private static string ShortcutPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            "AutoIME.lnk");

        public static bool IsAutoStartEnabled()
        {
            return File.Exists(ShortcutPath);
        }

        public static bool SetAutoStart(bool enable)
        {
            try
            {
                if (enable)
                {
                    string? exePath = Environment.ProcessPath;
                    if (string.IsNullOrEmpty(exePath))
                    {
                        exePath = Path.Combine(AppContext.BaseDirectory, "AutoIME.exe");
                    }

                    IShellLinkW link = (IShellLinkW)new ShellLink();
                    link.SetPath(exePath);
                    link.SetWorkingDirectory(AppContext.BaseDirectory);
                    link.SetDescription("AutoIME - Automatic Input Method Switcher");

                    IPersistFile file = (IPersistFile)link;
                    file.Save(ShortcutPath, false);
                    return true;
                }
                else
                {
                    if (File.Exists(ShortcutPath))
                    {
                        File.Delete(ShortcutPath);
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }
    }
}
