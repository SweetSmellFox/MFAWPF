using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFAWPF.Helper;
using MFAWPF.Views;
using MFAWPF.Views.UI;

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
        if (!MFAConfiguration.Configs.Any(c => c.Name.Equals(configName, StringComparison.OrdinalIgnoreCase)))
        {
            LoggerService.LogError($"Configuration {configName} does not exist");
            return;
        }

        if (MFAConfiguration.ConfigName == configName)
        {
            LoggerService.LogError($"Configuration {configName} is current configuration, cannot delete");
            return;
        }
        Instances.SettingsViewModel.ConfigurationList.Remove(config);
        MFAConfiguration.DeleteConfig(configName);
    }

    public override string ToString()
        => Name;
}
