using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HandyControl.Controls;
using MFAWPF.Controls;
using MessageBox = HandyControl.Controls.MessageBox;
using MFAWPF.Utils;

namespace MFAWPF.Views;

public partial class PresetSelectDialog : CustomWindow
{
    public string? SelectedPreset { get; private set; }
    
    public PresetSelectDialog()
    {
        InitializeComponent();
    }

    public PresetSelectDialog(IEnumerable<string> presets) : this()
    {
        PresetList.ItemsSource = presets;
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        if (PresetList.SelectedItem is string selectedPreset)
        {
            var result = MessageBox.Show($"确定要删除预设 '{selectedPreset}' 吗？", "确认删除", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                var presetManager = new PresetManager();
                presetManager.DeletePreset(selectedPreset);
                
                // 重新加载预设列表
                PresetList.ItemsSource = presetManager.GetPresetNames();
                
                Growl.Success($"预设 {selectedPreset} 已删除");
            }
        }
    }

    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        if (PresetList.SelectedItem is string selectedPreset)
        {
            SelectedPreset = selectedPreset;
            DialogResult = true;
        }
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}