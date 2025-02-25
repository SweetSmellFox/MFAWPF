using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Data;
using MFAWPF.Extensions;
using MFAWPF.Extensions.Maa;
using MFAWPF.Helper;
using System.Collections.ObjectModel;
using System.Windows;

namespace MFAWPF.ViewModels.UserControl.Settings;

public partial class GameSettingsUserControlModel : ViewModel
{
    [ObservableProperty] private bool _enableRecording = MFAConfiguration.MaaConfig.GetConfig("recording", false);

    [ObservableProperty] private bool _enableSaveDraw = MFAConfiguration.MaaConfig.GetConfig("save_draw", false);

    [ObservableProperty] private bool _showHitDraw = MFAConfiguration.MaaConfig.GetConfig("show_hit_draw", false);

    [ObservableProperty] private string _prescript = MFAConfiguration.GetConfiguration("Prescript", string.Empty);

    [ObservableProperty] private string _postScript = MFAConfiguration.GetConfiguration("Post-script", string.Empty);

    [ObservableProperty] private bool _isDebugMode = MFAConfiguration.MaaConfig.GetConfig("recording", false) || MFAConfiguration.MaaConfig.GetConfig("save_draw", false) || MFAConfiguration.MaaConfig.GetConfig("show_hit_draw", false);
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
        MFAConfiguration.MaaConfig.SetConfig("recording", value);
        IsDebugMode = EnableSaveDraw || EnableRecording || ShowHitDraw;
    }

    partial void OnEnableSaveDrawChanged(bool value)
    {
        MFAConfiguration.MaaConfig.SetConfig("save_draw", value);
        IsDebugMode = EnableSaveDraw || EnableRecording || ShowHitDraw;
    }

    partial void OnShowHitDrawChanged(bool value)
    {
        MFAConfiguration.MaaConfig.SetConfig("show_hit_draw", value);
        IsDebugMode = EnableSaveDraw || EnableRecording || ShowHitDraw;
    }

    partial void OnPrescriptChanged(string value)
    {
        MFAConfiguration.SetConfiguration("Prescript", value);
    }

    partial void OnPostScriptChanged(string value)
    {
        MFAConfiguration.SetConfiguration("Post-script", value);
    }

    [ObservableProperty] private ObservableCollection<MaaInterface.MaaCustomResource> _currentResources = [];

    [ObservableProperty] private string _currentResource = MFAConfiguration.GetConfiguration("Resource", string.Empty);

    partial void OnCurrentResourceChanged(string value)
    {
        MFAConfiguration.SetConfiguration("Resource", value);
    }
}
