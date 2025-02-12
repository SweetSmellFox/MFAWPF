using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Helper;
using System.Text.RegularExpressions;

namespace MFAWPF.ViewModels;

public partial class LocalizationViewModel : ViewModel
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

    public LocalizationViewModel(string resourceKey)
    {
        _resourceKey = resourceKey;
        
        UpdateName();
        LanguageHelper.LanguageChanged += OnLanguageChanged;
    }
    
    [ObservableProperty]
    private string? _name;
    
    private void UpdateName()
    {
        Name = ResourceKey.ToLocalization();
    }

    private void OnLanguageChanged(object sender, EventArgs e)
    {
        UpdateName();
    }
    public override string ToString()
        => ResourceKey;
}
