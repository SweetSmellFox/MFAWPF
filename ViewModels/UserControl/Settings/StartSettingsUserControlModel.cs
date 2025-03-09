using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFAWPF.Configuration;
using MFAWPF.Extensions;
using Microsoft.Win32;

namespace MFAWPF.ViewModels.UserControl.Settings;

public partial class StartSettingsUserControlModel: ViewModel
{
    [ObservableProperty] private bool _autoMinimize = ConfigurationHelper.GetValue(ConfigurationKeys.AutoMinimize, false);

    [ObservableProperty] private bool _autoHide = ConfigurationHelper.GetValue(ConfigurationKeys.AutoHide, false);

    [ObservableProperty] private string _softwarePath = ConfigurationHelper.GetValue(ConfigurationKeys.SoftwarePath, string.Empty);
    
    [ObservableProperty] private string _emulatorConfig = ConfigurationHelper.GetValue(ConfigurationKeys.EmulatorConfig, string.Empty);

    [ObservableProperty] private double _waitSoftwareTime = ConfigurationHelper.GetValue(ConfigurationKeys.WaitSoftwareTime, 60.0);


    partial void OnAutoMinimizeChanged(bool value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.AutoMinimize, value);
    }

    partial void OnAutoHideChanged(bool value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.AutoHide, value);
    }

    partial void OnSoftwarePathChanged(string value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.SoftwarePath, value);
    }

    partial void OnEmulatorConfigChanged(string value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.EmulatorConfig, value);
    }

    partial void OnWaitSoftwareTimeChanged(double value)
    {
        ConfigurationHelper.SetValue(ConfigurationKeys.WaitSoftwareTime, value);
    }


    [RelayCommand]
    private void SelectSoft()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "SelectExecutableFile".ToLocalization(),
            Filter = "ExeFilter".ToLocalization()
        };

        if (openFileDialog.ShowDialog().IsTrue())
        {
            SoftwarePath = openFileDialog.FileName;
        }
    }
}
