using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Configuration;
using MFAWPF.Extensions;
using MFAWPF.Extensions.Maa;
using MFAWPF.Helper;
using System.Collections.ObjectModel;
using System.Windows;

namespace MFAWPF.ViewModels.UserControl.Settings;

public partial class GameSettingsUserControlModel : ViewModel
{
    [ObservableProperty] private bool _enableRecording = ConfigurationHelper.MaaConfig.GetConfig(ConfigurationKeys.Recording, false);

    [ObservableProperty] private bool _enableSaveDraw = ConfigurationHelper.MaaConfig.GetConfig(ConfigurationKeys.SaveDraw, false);

    [ObservableProperty] private bool _showHitDraw = ConfigurationHelper.MaaConfig.GetConfig(ConfigurationKeys.ShowHitDraw, false);

    [ObservableProperty] private string _prescript = ConfigurationHelper.GetValue(ConfigurationKeys.Prescript, string.Empty);

    [ObservableProperty] private string _postScript = ConfigurationHelper.GetValue(ConfigurationKeys.Postscript, string.Empty);

    [ObservableProperty] private bool _isDebugMode = ConfigurationHelper.MaaConfig.GetConfig(ConfigurationKeys.Recording, false)
        || ConfigurationHelper.MaaConfig.GetConfig(ConfigurationKeys.SaveDraw, false)
        || ConfigurationHelper.MaaConfig.GetConfig(ConfigurationKeys.ShowHitDraw, false);
    private bool _shouldTip = true;

    partial void OnIsDebugModeChanged(bool value)
    {
        if (value && _shouldTip)
        {
            MessageBoxHelper.Show("DebugModeWarning".ToLocalization(), "Tip".ToLocalization(), MessageBoxButton.OK, MessageBoxImage.Warning);
            _shouldTip = false;
        }
    }

    partial void OnEnableRecordingChanged(bool value)
    {
        ConfigurationHelper.MaaConfig.SetConfig(ConfigurationKeys.Recording, value);
        IsDebugMode = EnableSaveDraw || EnableRecording || ShowHitDraw;
    }

    partial void OnEnableSaveDrawChanged(bool value)
    {
        ConfigurationHelper.MaaConfig.SetConfig(ConfigurationKeys.SaveDraw, value);
        IsDebugMode = EnableSaveDraw || EnableRecording || ShowHitDraw;
    }

    partial void OnShowHitDrawChanged(bool value)
    {
        ConfigurationHelper.MaaConfig.SetConfig(ConfigurationKeys.ShowHitDraw, value);
        IsDebugMode = EnableSaveDraw || EnableRecording || ShowHitDraw;
    }

    partial void OnPrescriptChanged(string value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.Prescript, value);
    }

    partial void OnPostScriptChanged(string value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.Postscript, value);
    }

    [ObservableProperty] private ObservableCollection<MaaInterface.MaaCustomResource> _currentResources = [];

    [ObservableProperty] private string _currentResource = ConfigurationHelper.GetValue(ConfigurationKeys.Resource, string.Empty);

    partial void OnCurrentResourceChanged(string value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.Resource, value);
    }
}
