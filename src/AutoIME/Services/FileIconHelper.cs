using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using AutoIME.Helpers;
using Microsoft.UI.Xaml.Media.Imaging;

namespace AutoIME.Services
{
    public static class FileIconHelper
    {
        private static readonly ConcurrentDictionary<string, byte[]> _iconBytesCache = new(StringComparer.OrdinalIgnoreCase);

        public static async Task<BitmapImage?> GetIconForProcessAsync(string exeName)
        {
            if (string.IsNullOrWhiteSpace(exeName))
                return null;

            exeName = exeName.Trim().ToLowerInvariant();

            if (_iconBytesCache.TryGetValue(exeName, out var cachedBytes))
            {
                return await CreateBitmapImageAsync(cachedBytes);
            }

            var bytes = await Task.Run(() => ExtractIconBytes(exeName));
            if (bytes != null && bytes.Length > 0)
            {
                _iconBytesCache[exeName] = bytes;
                return await CreateBitmapImageAsync(bytes);
            }

            return null;
        }

        private static byte[]? ExtractIconBytes(string exeName)
        {
            try
            {
                string? fullPath = FindExecutablePath(exeName);
                if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                    return null;

                var shinfo = new Win32Native.SHFILEINFO();
                IntPtr hImg = Win32Native.SHGetFileInfo(
                    fullPath,
                    0,
                    ref shinfo,
                    (uint)System.Runtime.InteropServices.Marshal.SizeOf(shinfo),
                    Win32Native.SHGFI_ICON | Win32Native.SHGFI_LARGEICON);

                if (shinfo.hIcon != IntPtr.Zero)
                {
                    try
                    {
                        using var icon = Icon.FromHandle(shinfo.hIcon);
                        using var bmp = icon.ToBitmap();
                        using var ms = new MemoryStream();
                        bmp.Save(ms, ImageFormat.Png);
                        return ms.ToArray();
                    }
                    finally
                    {
                        Win32Native.DestroyIcon(shinfo.hIcon);
                    }
                }
            }
            catch
            {
                // Ignore extraction failures
            }

            return null;
        }

        private static string? FindExecutablePath(string exeName)
        {
            try
            {
                // Check if absolute path or exists directly
                if (Path.IsPathRooted(exeName) && File.Exists(exeName))
                    return exeName;

                // Check System32
                string sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
                string sys32Path = Path.Combine(sys32, exeName);
                if (File.Exists(sys32Path))
                    return sys32Path;

                // Check Windows
                string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string winPath = Path.Combine(winDir, exeName);
                if (File.Exists(winPath))
                    return winPath;

                // Check App Paths in Registry
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{exeName}");
                if (key != null)
                {
                    var val = key.GetValue(null) as string;
                    if (!string.IsNullOrEmpty(val) && File.Exists(val))
                        return val;
                }

                // Check PATH environment variable
                var pathEnv = Environment.GetEnvironmentVariable("PATH");
                if (!string.IsNullOrEmpty(pathEnv))
                {
                    var dirs = pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var dir in dirs)
                    {
                        try
                        {
                            string p = Path.Combine(dir.Trim(), exeName);
                            if (File.Exists(p))
                                return p;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return null;
        }

        private static async Task<BitmapImage?> CreateBitmapImageAsync(byte[] bytes)
        {
            try
            {
                var bitmapImage = new BitmapImage();
                using var stream = new MemoryStream(bytes);
                using var randomAccessStream = stream.AsRandomAccessStream();
                await bitmapImage.SetSourceAsync(randomAccessStream);
                return bitmapImage;
            }
            catch
            {
                return null;
            }
        }
    }
}

