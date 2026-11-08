using System;
using AutoIME.Services;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace AutoIME.Views
{
    public sealed partial class HotkeyDialog : ContentDialog
    {
        public string RecordedHotkey { get; private set; } = "^!i";
        private string _currentAhk = "^!i";

        public HotkeyDialog(string currentHotkey)
        {
            this.InitializeComponent();

            _currentAhk = string.IsNullOrWhiteSpace(currentHotkey) ? "^!i" : currentHotkey;
            RecordedHotkey = _currentAhk;

            ApplyLocalization();
            UpdateDisplay();
        }

        private void ApplyLocalization()
        {
            var loc = LocalizationService.Instance;
            Title = loc["hk_dialog_title"];
            TxtPrompt.Text = loc["hk_dialog_prompt"];
            TxtHelp.Text = loc["hk_dialog_help"];
            PrimaryButtonText = loc["hk_dialog_confirm"];
            SecondaryButtonText = loc["hk_dialog_reset"];
            CloseButtonText = loc["hk_dialog_cancel"];
        }

        private void UpdateDisplay()
        {
            TxtHotkeyDisplay.Text = HotkeyService.FormatDisplay(_currentAhk);
        }

        private void OnDialogKeyDown(object sender, KeyRoutedEventArgs e)
        {
            var key = e.Key;

            // Ignore standalone modifiers
            if (key == VirtualKey.Control || key == VirtualKey.LeftControl || key == VirtualKey.RightControl ||
                key == VirtualKey.Menu || key == VirtualKey.LeftMenu || key == VirtualKey.RightMenu ||
                key == VirtualKey.Shift || key == VirtualKey.LeftShift || key == VirtualKey.RightShift ||
                key == VirtualKey.LeftWindows || key == VirtualKey.RightWindows)
            {
                return;
            }

            // Check modifiers state
            bool ctrl = (Helpers.Win32Native.GetKeyState(0x11) & 0x8000) != 0; // VK_CONTROL
            bool alt = (Helpers.Win32Native.GetKeyState(0x12) & 0x8000) != 0;  // VK_MENU
            bool shift = (Helpers.Win32Native.GetKeyState(0x10) & 0x8000) != 0;// VK_SHIFT
            bool win = ((Helpers.Win32Native.GetKeyState(0x5B) & 0x8000) != 0) || // VK_LWIN
                       ((Helpers.Win32Native.GetKeyState(0x5C) & 0x8000) != 0);   // VK_RWIN

            string keyName = key switch
            {
                >= VirtualKey.A and <= VirtualKey.Z => key.ToString().ToLowerInvariant(),
                >= VirtualKey.Number0 and <= VirtualKey.Number9 => ((int)key - (int)VirtualKey.Number0).ToString(),
                >= VirtualKey.F1 and <= VirtualKey.F24 => key.ToString().ToLowerInvariant(),
                VirtualKey.Space => "space",
                VirtualKey.Tab => "tab",
                VirtualKey.Enter => "enter",
                VirtualKey.Escape => "escape",
                VirtualKey.Insert => "insert",
                VirtualKey.Delete => "delete",
                VirtualKey.Home => "home",
                VirtualKey.End => "end",
                VirtualKey.PageUp => "pgup",
                VirtualKey.PageDown => "pgdn",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(keyName))
                return;

            string prefix = "";
            if (ctrl) prefix += "^";
            if (alt) prefix += "!";
            if (shift) prefix += "+";
            if (win) prefix += "#";

            _currentAhk = prefix + keyName;
            UpdateDisplay();
            e.Handled = true;
        }

        private void OnDialogKeyUp(object sender, KeyRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            RecordedHotkey = _currentAhk;
        }

        private void OnSecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Reset to default (^!i)
            _currentAhk = "^!i";
            UpdateDisplay();
            args.Cancel = true; // Keep dialog open so user can see it's reset
        }
    }
}
