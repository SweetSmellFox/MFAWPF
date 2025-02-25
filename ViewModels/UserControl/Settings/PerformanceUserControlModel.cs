using CommunityToolkit.Mvvm.ComponentModel;
using MFAWPF.Data;
using System.Collections.ObjectModel;

namespace MFAWPF.ViewModels.UserControl.Settings;

public partial class PerformanceUserControlModel: ViewModel
{
    public static ObservableCollection<Tool.LocalizationViewModel> GpuOptions => [new("GpuOptionAuto"), new("GpuOptionDisable")];

    [ObservableProperty] private int _gpuIndex = MFAConfiguration.GetConfiguration("EnableGPU", false) ? 0 : 1;

    partial void OnGpuIndexChanged(int value)
    {
        MFAConfiguration.SetConfiguration("EnableGPU", value == 0);
    }
}
