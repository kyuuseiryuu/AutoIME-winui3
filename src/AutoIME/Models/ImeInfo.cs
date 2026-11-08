using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoIME.Models
{
    public partial class ImeInfo : ObservableObject
    {
        [ObservableProperty]
        private IntPtr _hkl;

        [ObservableProperty]
        private string _hklHex = string.Empty;

        [ObservableProperty]
        private uint _langId;

        [ObservableProperty]
        private string _displayName = string.Empty;

        [ObservableProperty]
        private int _appCount;

        [ObservableProperty]
        private bool _isSelected;

        public ObservableCollection<AppRule> Apps { get; } = new();

        public ImeInfo()
        {
            Apps.CollectionChanged += OnAppsCollectionChanged;
        }

        public ImeInfo(IntPtr hkl, string hklHex, uint langId, string displayName) : this()
        {
            Hkl = hkl;
            HklHex = hklHex;
            LangId = langId;
            DisplayName = displayName;
        }

        private void OnAppsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            AppCount = Apps.Count;
        }
    }
}
