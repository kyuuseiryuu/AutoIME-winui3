using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AutoIME.Models;

namespace AutoIME.Services
{
    public class ConfigService
    {
        private static ConfigService? _instance;
        public static ConfigService Instance => _instance ??= new ConfigService();

        public string ConfigFilePath { get; private set; }

        public ConfigService()
        {
            // First check application directory
            string appDir = AppContext.BaseDirectory;
            string localPath = Path.Combine(appDir, "AutoIME_config.ini");

            // Also check AppData
            string appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AutoIME");

            // If local directory has config or is writable, prefer it for portability
            try
            {
                string testFile = Path.Combine(appDir, ".write_test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                ConfigFilePath = localPath;
            }
            catch
            {
                if (!Directory.Exists(appDataDir))
                    Directory.CreateDirectory(appDataDir);
                ConfigFilePath = Path.Combine(appDataDir, "AutoIME_config.ini");
            }
        }

        public (AppSettings Settings, List<ImeInfo> Imes) LoadConfig()
        {
            var settings = new AppSettings();
            var systemImes = ImeEngine.GetSystemImeList();
            var imeMap = new Dictionary<string, ImeInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (var ime in systemImes)
            {
                imeMap[ime.HklHex] = ime;
            }

            bool isFirstRun = !File.Exists(ConfigFilePath);
            if (!isFirstRun)
            {
                ParseIniFile(ConfigFilePath, settings, imeMap);
            }
            else
            {
                InitDefaultRules(systemImes);
                SaveConfig(settings, systemImes);
            }

            return (settings, systemImes);
        }

        private void ParseIniFile(string filePath, AppSettings settings, Dictionary<string, ImeInfo> imeMap)
        {
            try
            {
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                string currentSection = string.Empty;

                foreach (var rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                        continue;

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentSection = line[1..^1].Trim();
                        continue;
                    }

                    int equalsIdx = line.IndexOf('=');
                    if (equalsIdx < 0)
                        continue;

                    string key = line[..equalsIdx].Trim();
                    string val = line[(equalsIdx + 1)..].Trim();

                    if (currentSection.Equals("Settings", StringComparison.OrdinalIgnoreCase))
                    {
                        if (key.Equals("Language", StringComparison.OrdinalIgnoreCase))
                            settings.Language = val;
                        else if (key.Equals("Hotkey", StringComparison.OrdinalIgnoreCase))
                            settings.Hotkey = val;
                        else if (key.Equals("Theme", StringComparison.OrdinalIgnoreCase))
                            settings.Theme = val;
                    }
                    else if (currentSection.StartsWith("IME_", StringComparison.OrdinalIgnoreCase))
                    {
                        string hklHex = currentSection[4..].Trim();
                        if (key.Equals("DisplayName", StringComparison.OrdinalIgnoreCase))
                        {
                            if (imeMap.TryGetValue(hklHex, out var ime))
                            {
                                if (!string.IsNullOrEmpty(val))
                                    ime.DisplayName = val;
                            }
                        }
                        else
                        {
                            string exe = key.ToLowerInvariant();
                            if (imeMap.TryGetValue(hklHex, out var ime))
                            {
                                ime.Apps.Add(new AppRule(exe, val));
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void InitDefaultRules(List<ImeInfo> systemImes)
        {
            var loc = LocalizationService.Instance;
            foreach (var ime in systemImes)
            {
                uint langId = ime.LangId;
                if (langId == 0x0409) // English
                {
                    string devDesc = loc["desc_dev_tool"];
                    string[] englishApps =
                    {
                        "code.exe", "windowsterminal.exe", "cmd.exe", "powershell.exe",
                        "pwsh.exe", "idea64.exe", "pycharm64.exe", "clion64.exe",
                        "devenv.exe", "sublime_text.exe", "git-bash.exe", "antigravity ide.exe"
                    };

                    foreach (var app in englishApps)
                    {
                        ime.Apps.Add(new AppRule(app, devDesc));
                    }
                }
                else if (langId == 0x0804) // Chinese
                {
                    string chatDesc = loc["desc_chat_tool"];
                    string[] chineseApps =
                    {
                        "antigravity.exe", "weixin.exe", "qq.exe", "dingtalk.exe", "feishu.exe"
                    };

                    foreach (var app in chineseApps)
                    {
                        ime.Apps.Add(new AppRule(app, chatDesc));
                    }
                }
            }
        }

        public void SaveConfig(AppSettings settings, IEnumerable<ImeInfo> imes)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("; ==============================================================================");
                sb.AppendLine("; AutoIME Configuration / 配置文件");
                sb.AppendLine("; ==============================================================================");
                sb.AppendLine();
                sb.AppendLine("[Settings]");
                sb.AppendLine($"Language={settings.Language}");
                sb.AppendLine($"Hotkey={settings.Hotkey}");
                sb.AppendLine($"Theme={settings.Theme}");
                sb.AppendLine();

                foreach (var ime in imes)
                {
                    sb.AppendLine($"[IME_{ime.HklHex}]");
                    sb.AppendLine($"DisplayName={ime.DisplayName}");
                    foreach (var app in ime.Apps)
                    {
                        sb.AppendLine($"{app.ExeName}={app.Description}");
                    }
                    sb.AppendLine();
                }

                File.WriteAllText(ConfigFilePath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }
    }
}

