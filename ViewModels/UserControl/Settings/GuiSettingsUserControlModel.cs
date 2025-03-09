using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Configuration;
using MFAWPF.Helper;
using System.Collections.ObjectModel;

namespace MFAWPF.ViewModels.UserControl.Settings;

public partial class GuiSettingsUserControlModel: ViewModel
{
    public ObservableCollection<LanguageHelper.SupportedLanguage> SupportedLanguages => LanguageHelper.SupportedLanguages;
    
    [ObservableProperty] private int _languageIndex = ConfigurationHelper.GetValue(ConfigurationKeys.LangIndex, 0);

    partial void OnLanguageIndexChanged(int value)
    {
        LanguageHelper.ChangeLanguage(SupportedLanguages[value]);
        ConfigurationHelper.SetValue(ConfigurationKeys.LangIndex, value);
    }

    [ObservableProperty] private ObservableCollection<Tool.LocalizationViewModel> _themes =
    [
        new("LightColor"),
        new("DarkColor"),
        new("FollowingSystem"),
    ];
    
    [ObservableProperty] private int _themeIndex = ConfigurationHelper.GetValue(ConfigurationKeys.ThemeIndex, 0);
    
    partial void OnThemeIndexChanged(int value)
    {
        ThemeHelper.UpdateThemeIndexChanged(value);
        ConfigurationHelper.SetValue(ConfigurationKeys.ThemeIndex, value);
    }

    private bool _shouldMinimizeToTray = ConfigurationHelper.GetValue(ConfigurationKeys.ShouldMinimizeToTray, false);

    public bool ShouldMinimizeToTray
    {
        set
        {
            SetProperty(ref _shouldMinimizeToTray, value);
            ConfigurationHelper.SetValue(ConfigurationKeys.ShouldMinimizeToTray, value);
        }
        get => _shouldMinimizeToTray;
    }


}
