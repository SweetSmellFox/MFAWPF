using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFAWPF.Helper;
using MFAWPF.Views;

namespace MFAWPF.Data;

public partial class MFAConfig : ObservableObject
{
    [ObservableProperty] public string _name;
    [ObservableProperty] public string _fileName;
    [ObservableProperty] public Dictionary<string, object> _config;


    [RelayCommand]
    public void DeleteConfiguration(MFAConfig config)
    {
        var configName = config.Name;
        if (!DataSet.Configs.Any(c => c.Name.Equals(configName, StringComparison.OrdinalIgnoreCase)))
        {
            LoggerService.LogError($"Configuration {configName} does not exist");
            return;
        }

        if (DataSet.ConfigName == configName)
        {
            LoggerService.LogError($"Configuration {configName} is current configuration, cannot delete");
            return;
        }
        SettingsView.ViewModel.ConfigurationList.Remove(config);
        DataSet.DeleteConfig(configName);
    }

    public override string ToString()
        => Name;
}
