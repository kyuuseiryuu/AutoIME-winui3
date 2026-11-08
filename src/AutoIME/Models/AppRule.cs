using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;

namespace AutoIME.Models
{
    public partial class AppRule : ObservableObject
    {
        [ObservableProperty]
        private string _exeName = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private BitmapImage? _icon;

        public AppRule()
        {
        }

        public AppRule(string exeName, string description)
        {
            _exeName = exeName.ToLowerInvariant();
            _description = description;
        }
    }
}

