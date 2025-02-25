using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFAWPF.Data;
using MFAWPF.Extensions;
using Microsoft.Win32;

namespace MFAWPF.ViewModels.UserControl.Settings;

public partial class StartSettingsUserControlModel: ViewModel
{
    [ObservableProperty] private bool _autoMinimize = MFAConfiguration.GetConfiguration("AutoMinimize", false);

    [ObservableProperty] private bool _autoHide = MFAConfiguration.GetConfiguration("AutoHide", false);

    [ObservableProperty] private string _softwarePath = MFAConfiguration.GetConfiguration("SoftwarePath", string.Empty);
    
    [ObservableProperty] private string _emulatorConfig = MFAConfiguration.GetConfiguration("EmulatorConfig", string.Empty);

    [ObservableProperty] private double _waitSoftwareTime = MFAConfiguration.GetConfiguration("WaitSoftwareTime", 60.0);


    partial void OnAutoMinimizeChanged(bool value)
    {
        MFAConfiguration.SetConfiguration("AutoMinimize", value);
    }

    partial void OnAutoHideChanged(bool value)
    {
        MFAConfiguration.SetConfiguration("AutoHide", value);
    }

    partial void OnSoftwarePathChanged(string value)
    {
        MFAConfiguration.SetConfiguration("SoftwarePath", value);
    }

    partial void OnEmulatorConfigChanged(string value)
    {
        MFAConfiguration.SetConfiguration("EmulatorConfig", value);
    }

    partial void OnWaitSoftwareTimeChanged(double value)
    {
        MFAConfiguration.SetConfiguration("WaitSoftwareTime", value);
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
