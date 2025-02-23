using HandyControl.Controls;
using HandyControl.Data;
using HandyControl.Interactivity;
using HandyControl.Themes;
using MFAWPF.Data;
using MFAWPF.Helper;
using MFAWPF.ViewModels;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;

namespace MFAWPF.Views;

public partial class SettingsView
{
    public static ViewModels.SettingViewModel ViewModel { get; set; }
    public SettingsView(ViewModels.SettingViewModel model, ViewModels.MainViewModel mainViewModel)
    {
        InitializeComponent();
        ViewModel = model;
        DataContext = this;
        Initialize();
    }

    void Initialize()
    {
        // 重构消失了
    }


    private void SetSettingOption(ComboBox comboBox,
        string titleKey,
        IEnumerable<string> options,
        string datatype,
        int defaultValue = 0)
    {
        comboBox.SelectedIndex = DataSet.GetData(datatype, defaultValue);
        comboBox.ItemsSource = options;
        comboBox.BindLocalization(titleKey);
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Left);
        comboBox.SelectionChanged += (sender, _) =>
        {
            var index = (sender as ComboBox)?.SelectedIndex ?? 0;
            DataSet.SetData(datatype, index);
            MaaProcessor.Instance.SetCurrentTasker();
        };
    }

    private void SetBindSettingOption(ComboBox comboBox,
        string titleKey,
        IEnumerable<string> options,
        string datatype,
        int defaultValue = 0)

    {
        comboBox.SelectedIndex = DataSet.GetData(datatype, defaultValue);

        foreach (var s in options)
        {
            var comboBoxItem = new ComboBoxItem();
            comboBoxItem.BindLocalization(s, ContentProperty);
            comboBox.Items.Add(comboBoxItem);
        }

        comboBox.BindLocalization(titleKey);
        comboBox.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Left);
        comboBox.SelectionChanged += (sender, _) =>
        {
            var index = (sender as ComboBox)?.SelectedIndex ?? 0;
            DataSet.SetData(datatype, index);
            MaaProcessor.Instance.SetCurrentTasker();
        };
    }
    
    private void SwapFiles(string file1Path, string file2Path)
    {
        // 备份文件
        string backupFilePath = $"{file1Path}.bak";
        File.Copy(file1Path, backupFilePath, true);

        // 读取文件内容
        string file1Content = File.ReadAllText(file1Path);
        string file2Content = File.ReadAllText(file2Path);

        // 只更换 config.json 的内容
        File.WriteAllText(file1Path, file2Content);
    }
}
