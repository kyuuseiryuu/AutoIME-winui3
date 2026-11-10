using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoIME.Models;
using AutoIME.Services;

namespace AutoIME.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public LocalizationService Loc => LocalizationService.Instance;

        [ObservableProperty]
        private AppSettings _settings = new();

        [ObservableProperty]
        private ObservableCollection<ImeInfo> _systemImes = new();

        [ObservableProperty]
        private ImeInfo? _selectedIme;

        [ObservableProperty]
        private ObservableCollection<AppRule> _currentFilteredApps = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private int _selectedCount = 0;

        [ObservableProperty]
        private bool _isAllSelected = false;

        [ObservableProperty]
        private string _formattedHotkey = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isStatusOpen = false;

        [ObservableProperty]
        private string _statusSeverity = "Informational";

        public Func<string, string, Task<bool>>? ConflictConfirmRequested { get; set; }

        public MainViewModel()
        {
            InitializeData();
        }

        public void InitializeData()
        {
            var (loadedSettings, imes) = ConfigService.Instance.LoadConfig();
            Settings = loadedSettings;
            SystemImes = new ObservableCollection<ImeInfo>(imes);

            Loc.CurrentLanguage = Settings.Language;
            FormattedHotkey = HotkeyService.FormatDisplay(Settings.Hotkey);

            if (SystemImes.Count > 0)
            {
                SelectedIme = SystemImes[0];
            }

            WindowWatcherService.Instance.UpdateRules(SystemImes);

            _ = LoadAllIconsAsync();
        }

        private async Task LoadAllIconsAsync()
        {
            foreach (var ime in SystemImes)
            {
                foreach (var app in ime.Apps)
                {
                    if (app.Icon == null)
                    {
                        app.Icon = await FileIconHelper.GetIconForProcessAsync(app.ExeName);
                    }
                }
            }
        }

        partial void OnSelectedImeChanged(ImeInfo? value)
        {
            foreach (var ime in SystemImes)
            {
                ime.IsSelected = (ime == value);
            }
            UpdateFilteredApps();
            UpdateSelectionCount();
        }

        partial void OnSearchTextChanged(string value)
        {
            UpdateFilteredApps();
        }

        public void UpdateFilteredApps()
        {
            CurrentFilteredApps.Clear();
            if (SelectedIme == null)
                return;

            string query = SearchText.Trim().ToLowerInvariant();
            var matched = SelectedIme.Apps
                .Where(a => string.IsNullOrEmpty(query) ||
                            a.ExeName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            a.Description.Contains(query, StringComparison.OrdinalIgnoreCase));

            foreach (var item in matched)
            {
                CurrentFilteredApps.Add(item);
            }
        }

        public void UpdateSelectionCount()
        {
            if (SelectedIme == null)
            {
                SelectedCount = 0;
                IsAllSelected = false;
                return;
            }

            int count = SelectedIme.Apps.Count(a => a.IsSelected);
            SelectedCount = count;
            IsAllSelected = SelectedIme.Apps.Count > 0 && count == SelectedIme.Apps.Count;
        }

        public async Task<bool> AddAppToCurrentImeAsync(string exe, string remark)
        {
            if (SelectedIme == null)
                return false;

            exe = exe.Trim().ToLowerInvariant();
            if (!exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                exe += ".exe";

            // Check if already in current tab
            if (SelectedIme.Apps.Any(a => a.ExeName.Equals(exe, StringComparison.OrdinalIgnoreCase)))
            {
                ShowStatus(Loc["already_in_tab", exe, SelectedIme.DisplayName], "Warning");
                return false;
            }

            // Check if exists in another tab (conflict)
            ImeInfo? conflictIme = null;
            AppRule? conflictRule = null;
            foreach (var ime in SystemImes)
            {
                if (ime == SelectedIme)
                    continue;

                var existing = ime.Apps.FirstOrDefault(a => a.ExeName.Equals(exe, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    conflictIme = ime;
                    conflictRule = existing;
                    break;
                }
            }

            if (conflictIme != null && conflictRule != null)
            {
                string prompt = Loc["conflict_prompt", exe, conflictIme.DisplayName, SelectedIme.DisplayName];
                string title = Loc["conflict_title"];

                bool confirmed = false;
                if (ConflictConfirmRequested != null)
                {
                    confirmed = await ConflictConfirmRequested.Invoke(title, prompt);
                }

                if (!confirmed)
                    return false;

                // Move from conflicting tab
                conflictIme.Apps.Remove(conflictRule);
            }

            var newRule = new AppRule(exe, remark);
            SelectedIme.Apps.Add(newRule);
            UpdateFilteredApps();

            // Load icon
            _ = Task.Run(async () =>
            {
                var icon = await FileIconHelper.GetIconForProcessAsync(newRule.ExeName);
                if (icon != null)
                {
                    App.MainWindowInstance?.DispatcherQueue.TryEnqueue(() => newRule.Icon = icon);
                }
            });

            AutoSaveConfig();
            ShowStatus(Loc["add_success", exe, SelectedIme.DisplayName], "Success");
            return true;
        }

        [RelayCommand]
        public void DeleteRule(AppRule? rule)
        {
            if (rule != null && SelectedIme != null)
            {
                SelectedIme.Apps.Remove(rule);
                UpdateFilteredApps();
                UpdateSelectionCount();
                AutoSaveConfig();
                ShowStatus(Loc["del_success"], "Success");
            }
        }

        [RelayCommand]
        public void DeleteSelected()
        {
            if (SelectedIme == null)
                return;

            var toRemove = SelectedIme.Apps.Where(a => a.IsSelected).ToList();
            if (toRemove.Count == 0)
            {
                ShowStatus(Loc["del_select_hint"], "Warning");
                return;
            }

            foreach (var r in toRemove)
            {
                SelectedIme.Apps.Remove(r);
            }

            UpdateFilteredApps();
            UpdateSelectionCount();
            AutoSaveConfig();
            ShowStatus(Loc["del_success"], "Success");
        }

        [RelayCommand]
        public void ClearCurrentTab()
        {
            if (SelectedIme == null || SelectedIme.Apps.Count == 0)
                return;

            SelectedIme.Apps.Clear();
            UpdateFilteredApps();
            UpdateSelectionCount();
            AutoSaveConfig();
            ShowStatus(Loc["del_success"], "Success");
        }

        [RelayCommand]
        public void ToggleSelectAll(bool isChecked)
        {
            if (SelectedIme == null)
                return;

            foreach (var app in SelectedIme.Apps)
            {
                app.IsSelected = isChecked;
            }

            UpdateSelectionCount();
        }

        public void AutoSaveConfig()
        {
            ConfigService.Instance.SaveConfig(Settings, SystemImes);
            WindowWatcherService.Instance.UpdateRules(SystemImes);
        }

        [RelayCommand]
        public void SaveConfig()
        {
            AutoSaveConfig();
            ShowStatus(Loc["save_success"], "Success");
        }

        [RelayCommand]
        public void RefreshImes()
        {
            var systemImes = ImeEngine.GetSystemImeList();
            var existingMap = SystemImes.ToDictionary(i => i.HklHex, StringComparer.OrdinalIgnoreCase);

            var merged = new List<ImeInfo>();
            foreach (var ime in systemImes)
            {
                if (existingMap.TryGetValue(ime.HklHex, out var existing))
                {
                    foreach (var app in existing.Apps)
                    {
                        ime.Apps.Add(app);
                    }
                }
                merged.Add(ime);
            }

            SystemImes = new ObservableCollection<ImeInfo>(merged);
            if (SystemImes.Count > 0)
            {
                SelectedIme = SystemImes[0];
            }

            WindowWatcherService.Instance.UpdateRules(SystemImes);
            _ = LoadAllIconsAsync();
            AutoSaveConfig();

            ShowStatus(Loc["ime_reloaded"], "Success");
        }

        public void UpdateHotkey(string newAhkHk)
        {
            Settings.Hotkey = newAhkHk;
            FormattedHotkey = HotkeyService.FormatDisplay(newAhkHk);
            AutoSaveConfig();
        }

        public void UpdateLanguage(string lang)
        {
            Settings.Language = lang;
            Loc.CurrentLanguage = lang;
            AutoSaveConfig();
        }

        public void UpdateTheme(string theme)
        {
            Settings.Theme = theme;
            AutoSaveConfig();
        }

        public void ToggleDebugMode(bool enabled)
        {
            Settings.DebugMode = enabled;
            AutoSaveConfig();
            ShowStatus(enabled ? Loc["debug_enabled"] : Loc["debug_disabled"], "Informational");
        }

        public void ToggleAutoStart(bool enabled)
        {
            Settings.AutoStart = enabled;
            bool success = StartupService.SetAutoStart(enabled);
            AutoSaveConfig();
            if (success)
            {
                ShowStatus(enabled ? Loc["startup_enabled"] : Loc["startup_disabled"], "Success");
            }
            else
            {
                ShowStatus(Loc["startup_failed", "Access Denied"], "Error");
            }
        }

        public void ShowStatus(string msg, string severity = "Informational")
        {
            StatusMessage = msg;
            StatusSeverity = severity;
            IsStatusOpen = true;
        }
    }
}
