using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Utils;
using System.Text.RegularExpressions;

namespace MFAWPF.ViewModels;

public class SettingViewModel : ObservableObject
{
    private string? _resourceKey;

    public string? ResourceKey
    {
        get => _resourceKey;
        set
        {
            if (SetProperty(ref _resourceKey, value))
            {
                UpdateName();
            }
        }
    }

    public SettingViewModel(string resourceKey)
    {
        _resourceKey = resourceKey;
        
        UpdateName();
        LanguageManager.LanguageChanged += OnLanguageChanged;
    }
    
    private string? _name;

    public string? Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private void UpdateName()
    {
        Name = ResourceKey.GetLocalizationString();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateName();
    }
    public override string ToString()
        => ResourceKey;
}
