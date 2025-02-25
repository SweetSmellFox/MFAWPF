using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Data;
using MFAWPF.Helper;
using System.Collections.ObjectModel;

namespace MFAWPF.ViewModels.UserControl.Settings;

public partial class GuiSettingsUserControlModel: ViewModel
{
    public ObservableCollection<LanguageHelper.SupportedLanguage> SupportedLanguages => LanguageHelper.SupportedLanguages;
    
    [ObservableProperty] private int _languageIndex = MFAConfiguration.GetConfiguration("LangIndex", 0);

    partial void OnLanguageIndexChanged(int value)
    {
        LanguageHelper.ChangeLanguage(SupportedLanguages[value]);
        MFAConfiguration.SetConfiguration("LangIndex", value);
    }

    [ObservableProperty] private ObservableCollection<Tool.LocalizationViewModel> _themes =
    [
        new("LightColor"),
        new("DarkColor"),
        new("FollowingSystem"),
    ];
    
    [ObservableProperty] private int _themeIndex = MFAConfiguration.GetConfiguration("ThemeIndex", 0);
    
    partial void OnThemeIndexChanged(int value)
    {
        ThemeHelper.UpdateThemeIndexChanged(value);
        MFAConfiguration.SetConfiguration("ThemeIndex", value);
    }

    private bool _shouldMinimizeToTray = MFAConfiguration.GetConfiguration("ShouldMinimizeToTray", false);

    public bool ShouldMinimizeToTray
    {
        set
        {
            SetProperty(ref _shouldMinimizeToTray, value);
            MFAConfiguration.SetConfiguration("ShouldMinimizeToTray", value);
        }
        get => _shouldMinimizeToTray;
    }


}
