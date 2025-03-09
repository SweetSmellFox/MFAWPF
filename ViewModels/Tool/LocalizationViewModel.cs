using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Extensions;
using MFAWPF.Helper;

namespace MFAWPF.ViewModels.Tool;

public partial class LocalizationViewModel : ViewModel
{
    [ObservableProperty] private string _resourceKey = string.Empty;

    partial void OnResourceKeyChanged(string value)
    {
        UpdateName();
    }

    public LocalizationViewModel() { }

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
    private void OnLanguageChanged(object sender, EventArgs e)
    {
        UpdateName();
    }
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private object? _other;

    private void UpdateName()
    {
        if (string.IsNullOrWhiteSpace(ResourceKey))
            return;
        if (_formatArgsKeys != null && _formatArgsKeys.Length != 0)
            Name = ResourceKey.ToLocalizationFormatted(true, _formatArgsKeys);
        else
            Name = ResourceKey.ToLocalization();
    }


    public override string ToString()
        => ResourceKey;
}
