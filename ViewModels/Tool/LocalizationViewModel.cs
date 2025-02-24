using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Helper;

namespace MFAWPF.ViewModels.Tool;

public partial class LocalizationViewModel : ViewModel
{
    private string _resourceKey = string.Empty;

    public string ResourceKey
    {
        get => _resourceKey;
        private init
        {
            SetProperty(ref _resourceKey, value);
            UpdateName();
        }
    }

    public LocalizationViewModel()
    {
    }
    private readonly string[]? _formatArgsKeys;

    public LocalizationViewModel(string resourceKey)
    {
        ResourceKey = resourceKey;
        LanguageHelper.LanguageChanged += OnLanguageChanged;
    }
    public LocalizationViewModel(string resourceKey, params string[] keys)
    {
        ResourceKey = resourceKey;
        _formatArgsKeys = keys;
        LanguageHelper.LanguageChanged += OnLanguageChanged;
    }
    [ObservableProperty] private string _name = string.Empty;

    private void UpdateName()
    {
        if (string.IsNullOrWhiteSpace(ResourceKey))
            return;
        if (_formatArgsKeys != null && _formatArgsKeys.Length != 0)
            Name = ResourceKey.ToLocalizationFormatted(_formatArgsKeys);
        else
            Name = ResourceKey.ToLocalization();
    }

    private void OnLanguageChanged(object sender, EventArgs e)
    {
        UpdateName();
    }

    public override string ToString()
        => ResourceKey;
}
