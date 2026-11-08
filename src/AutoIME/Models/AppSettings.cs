using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoIME.Models
{
    public partial class AppSettings : ObservableObject
    {
        [ObservableProperty]
        private bool _debugMode = false;

        [ObservableProperty]
        private string _language = "zh";

        [ObservableProperty]
        private string _hotkey = "^!i";

        [ObservableProperty]
        private string _theme = "system";

        [ObservableProperty]
        private bool _autoStart = false;
    }
}
