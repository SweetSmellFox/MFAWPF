using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFAWPF.Helper;


namespace MFAWPF.Configuration;

public partial class MFAConfiguration : ObservableObject
{
    [ObservableProperty] public string _name;
    [ObservableProperty] public string _fileName;
    [ObservableProperty] public Dictionary<string, object> _config;


    [RelayCommand]
    public void DeleteConfiguration(MFAConfiguration configuration)
    {
        var configName = configuration.Name;
        if (!ConfigurationHelper.Configs.Any(c => c.Name.Equals(configName, StringComparison.OrdinalIgnoreCase)))
        {
            LoggerService.LogError($"Configuration {configName} does not exist");
            return;
        }

        if (ConfigurationHelper.ConfigName == configName)
        {
            LoggerService.LogError($"Configuration {configName} is current configuration, cannot delete");
            return;
        }
        Instances.SettingsViewModel.ConfigurationList.Remove(configuration);
        ConfigurationHelper.DeleteConfig(configName);
    }

    public override string ToString()
        => Name;
}
