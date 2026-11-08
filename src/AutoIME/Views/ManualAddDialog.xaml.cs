using System;
using AutoIME.Services;
using Microsoft.UI.Xaml.Controls;

namespace AutoIME.Views
{
    public sealed partial class ManualAddDialog : ContentDialog
    {
        public string ExeName { get; private set; } = string.Empty;
        public string Remark { get; private set; } = string.Empty;

        public ManualAddDialog()
        {
            this.InitializeComponent();
            ApplyLocalization();
        }

        private void ApplyLocalization()
        {
            var loc = LocalizationService.Instance;
            Title = loc["manual_title"];
            TxtPrompt.Text = loc["manual_prompt"];
            BoxExe.PlaceholderText = loc["manual_exe_placeholder"];
            TxtRemarkLabel.Text = loc["manual_remark_label"];
            BoxRemark.PlaceholderText = loc["manual_remark_placeholder"];
            PrimaryButtonText = loc["hk_dialog_confirm"];
            CloseButtonText = loc["hk_dialog_cancel"];
        }

        private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            string exe = BoxExe.Text.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(exe))
            {
                ErrInfoBar.Message = "请输入进程文件名！";
                ErrInfoBar.IsOpen = true;
                args.Cancel = true;
                return;
            }

            if (!exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                exe += ".exe";
            }

            string remark = BoxRemark.Text.Trim();
            if (string.IsNullOrWhiteSpace(remark))
            {
                remark = LocalizationService.Instance["manual_default_remark"];
            }

            ExeName = exe;
            Remark = remark;
        }
    }
}
