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
    public ViewModels.MainViewModel MainViewModel { get; set; }
    public SettingsView(ViewModels.SettingViewModel model, ViewModels.MainViewModel mainViewModel)
    {
        InitializeComponent();
        ViewModel = model;
        DataContext = this;
        Initialize();
    }

    void Initialize()
    {
        MinimizeToTrayCheckBox.IsChecked = DataSet.GetData("ShouldMinimizeToTray", true);
        MinimizeToTrayCheckBox.Checked += (_, _) => { DataSet.SetData("ShouldMinimizeToTray", true); };
        MinimizeToTrayCheckBox.Unchecked += (_, _) => { DataSet.SetData("ShouldMinimizeToTray", false); };

        //语言设置

        languageSettings.ItemsSource = LanguageHelper.SupportedLanguages;
        languageSettings.DisplayMemberPath = "Name";
        languageSettings.BindLocalization("LanguageOption");
        languageSettings.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);

        languageSettings.SelectionChanged += (sender, _) =>
        {
            if ((sender as ComboBox)?.SelectedItem is LanguageHelper.SupportedLanguage language)
                LanguageHelper.ChangeLanguage(language);
            DataSet.SetData("LangIndex", (sender as ComboBox)?.SelectedIndex ?? 0);
        };
        var binding2 = new Binding("LanguageIndex")
        {
            Source = ViewModel,
            Mode = BindingMode.OneWay
        };
        languageSettings.SetBinding(ComboBox.SelectedIndexProperty, binding2);

        //主题设置
        themeSettings.ItemsSource = new ObservableCollection<LocalizationViewModel>()
        {
            new("LightColor"),
            new("DarkColor"),
            new("LightColor"),
        };
        themeSettings.DisplayMemberPath = "Name";
        themeSettings.BindLocalization("ThemeOption");
        themeSettings.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Top);

        themeSettings.SelectionChanged += (sender, _) =>
        {
            var index = (sender as ComboBox)?.SelectedIndex  ?? 0;

            switch (index)
            {
                case 0:
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                    break;
                case 1:
                    ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                    break;
                default:
                    MainWindow.FollowSystemTheme();
                    break;
            }

            ThemeManager.Current.ApplicationTheme = index == 0 ? ApplicationTheme.Light : ApplicationTheme.Dark;
            DataSet.SetData("ThemeIndex", index);
        };
        themeSettings.SelectedIndex = DataSet.GetData("ThemeIndex", 0);

        //性能设置
        performanceSettings.IsChecked = DataSet.GetData("EnableGPU", true);
        performanceSettings.Checked += (_, _) => { DataSet.SetData("EnableGPU", true); };
        performanceSettings.Unchecked += (_, _) => { DataSet.SetData("EnableGPU", false); };

        //运行设置
        enableRecordingSettings.IsChecked = DataSet.MaaConfig.GetConfig("recording", false);
        enableRecordingSettings.Checked += (_, _) => { DataSet.MaaConfig.SetConfig("recording", true); };
        enableRecordingSettings.Unchecked += (_, _) => { DataSet.MaaConfig.SetConfig("recording", false); };

        enableSaveDrawSettings.IsChecked = DataSet.MaaConfig.GetConfig("save_draw", false);
        enableSaveDrawSettings.Checked += (_, _) => { DataSet.MaaConfig.SetConfig("save_draw", true); };
        enableSaveDrawSettings.Unchecked += (_, _) => { DataSet.MaaConfig.SetConfig("save_draw", false); };

        showHitDrawSettings.IsChecked = DataSet.MaaConfig.GetConfig("show_hit_draw", false);
        showHitDrawSettings.Checked += (_, _) => { DataSet.MaaConfig.SetConfig("show_hit_draw", true); };
        showHitDrawSettings.Unchecked += (_, _) => { DataSet.MaaConfig.SetConfig("show_hit_draw", false); };

        beforeTaskSettings.Text = DataSet.GetData("Prescript", string.Empty);
        beforeTaskSettings.BindLocalization("Prescript");
        beforeTaskSettings.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Left);
        beforeTaskSettings.TextChanged += (_, _) => { DataSet.SetData("Prescript", beforeTaskSettings.Text); };

        afterTaskSettings.Text = DataSet.GetData("Post-script", string.Empty);
        afterTaskSettings.BindLocalization("Post-script");
        afterTaskSettings.SetValue(TitleElement.TitlePlacementProperty, TitlePlacementType.Left);
        afterTaskSettings.TextChanged += (_, _) => { DataSet.SetData("Post-script", afterTaskSettings.Text); };

        //启动设置
        AutoMinimizeCheckBox.IsChecked = DataSet.GetData("AutoMinimize", false);
        AutoMinimizeCheckBox.Checked += (_, _) => { DataSet.SetData("AutoMinimize", true); };
        AutoMinimizeCheckBox.Unchecked += (_, _) => { DataSet.SetData("AutoMinimize", false); };

        AutoHideCheckBox.IsChecked = DataSet.GetData("AutoHide", false);
        AutoHideCheckBox.Checked += (_, _) => { DataSet.SetData("AutoHide", true); };
        AutoHideCheckBox.Unchecked += (_, _) => { DataSet.SetData("AutoHide", false); };

        SoftwarePathTextBox.Text = DataSet.GetData("SoftwarePath", string.Empty);
        SoftwarePathTextBox.TextChanged += (sender, _) =>
        {
            var text = (sender as TextBox)?.Text ?? string.Empty;
            DataSet.SetData("SoftwarePath", text);
        };
        SoftwarePathTextBox.PreviewDrop += File_Drop;

        SoftwarePathSelectButton.Click += (_, _) =>
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "SelectExecutableFile".ToLocalization(),
                Filter = "ExeFilter".ToLocalization()
            };

            if (openFileDialog.ShowDialog().IsTrue())
            {
                SoftwarePathTextBox.Text = openFileDialog.FileName;
            }
        };

        ExtrasTextBox.Text = DataSet.GetData("EmulatorConfig", string.Empty);
        ExtrasTextBox.TextChanged += (sender, _) =>
        {
            var text = (sender as TextBox)?.Text ?? string.Empty;
            DataSet.SetData("EmulatorConfig", text);
        };

        WaitSoftwareTimeTextBox.Value = DataSet.GetData("WaitSoftwareTime", 60.0);
        WaitSoftwareTimeTextBox.ValueChanged += (sender, _) =>
        {
            var value = (sender as NumericUpDown)?.Value ?? 60;
            DataSet.SetData("WaitSoftwareTime", value);
        };
        ExtrasTextBox.TextChanged += (sender, _) =>
        {
            var text = (sender as TextBox)?.Text ?? string.Empty;
            DataSet.SetData("EmulatorConfig", text);
        };
        //切换配置
        var configPath = Path.Combine(Environment.CurrentDirectory, "config");
        foreach (string file in Directory.GetFiles(configPath))
        {
            string fileName = Path.GetFileName(file);
            if (fileName.EndsWith(".json") && fileName != "maa_option.json")
            {
                swtichConfigs.Items.Add(fileName);
            }
        }

        //连接设置
        SetSettingOption(adbCaptureComboBox, "CaptureModeOption",
            [
                "Default", "RawWithGzip", "RawByNetcat",
                "Encode", "EncodeToFileAndPull", "MinicapDirect", "MinicapStream",
                "EmulatorExtras"
            ],
            "AdbControlScreenCapType");
        SetBindSettingOption(adbInputComboBox, "InputModeOption",
            ["MiniTouch", "MaaTouch", "AdbInput", "AutoDetect"],
            "AdbControlInputType");

        SetRememberAdbOption(rememberAdbButton);

        SetSettingOption(win32CaptureComboBox, "CaptureModeOption",
            ["FramePool", "DXGIDesktopDup", "GDI"],
            "Win32ControlScreenCapType");

        SetSettingOption(win32InputComboBox, "InputModeOption",
            ["Seize", "SendMessage"],
            "Win32ControlInputType");


        swtichConfigs.SelectionChanged += (sender, _) =>
        {
            string selectedItem = (string)swtichConfigs.SelectedItem;
            if (selectedItem == "config.json")
            {
                //
            }
            // else if (selectedItem == "maa_option.json")
            // {
            //     // 什么都不做，等待后续添加逻辑
            // }
            else if (selectedItem == "config.json.bak")
            {
                string _currentFile = Path.Combine(configPath, "config.json");
                string _selectedItem = Path.Combine(configPath, "config.json.bak");
                string _bakContent = File.ReadAllText(_selectedItem);
                File.WriteAllText(_currentFile, _bakContent);
                MainWindow.Instance.RestartMFA();
            }
            else
            {
                // 恢复成绝对路径
                string _currentFile = Path.Combine(configPath, "config.json");
                string _selectedItem = Path.Combine(configPath, selectedItem);
                SwapFiles(_currentFile, _selectedItem);
                MainWindow.Instance.RestartMFA();
            }
        };
        //软件更新
        //  CdkPassword.UnsafePassword = SimpleEncryptionHelper.Decrypt(DataSet.GetData("DownloadCDK", string.Empty));
        //  CdkPassword.PasswordChanged += (_, _) => { DataSet.SetData("DownloadCDK", SimpleEncryptionHelper.Encrypt( CdkPassword.Password)); };

        enableCheckVersionSettings.IsChecked = DataSet.GetData("EnableCheckVersion", true);
        enableCheckVersionSettings.Checked += (_, _) => { DataSet.SetData("EnableCheckVersion", true); };
        enableCheckVersionSettings.Unchecked += (_, _) => { DataSet.SetData("EnableCheckVersion", false); };

        enableAutoUpdateResourceSettings.IsChecked = DataSet.GetData("EnableAutoUpdateResource", false);
        enableAutoUpdateResourceSettings.Checked += (_, _) => { DataSet.SetData("EnableAutoUpdateResource", true); };
        enableAutoUpdateResourceSettings.Unchecked += (_, _) => { DataSet.SetData("EnableAutoUpdateResource", false); };

        enableAutoUpdateMFASettings.IsChecked = DataSet.GetData("EnableAutoUpdateMFA", false);
        enableAutoUpdateMFASettings.Checked += (_, _) => { DataSet.SetData("EnableAutoUpdateMFA", true); };
        enableAutoUpdateMFASettings.Unchecked += (_, _) => { DataSet.SetData("EnableAutoUpdateMFA", false); };
        
        if (!string.IsNullOrWhiteSpace(MaaInterface.Instance?.RID))
        {
        }
        //关于我们
        AddAbout();

    }
    private void StartsWithScript_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        // Note that you can have more than one file.
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        beforeTaskSettings.Text = files?[0] ?? string.Empty;
    }

    private void EndsWithScript_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        // Note that you can have more than one file.
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        afterTaskSettings.Text = files?[0] ?? string.Empty;
    }

    private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
    }

    private void UpdateResource(object sender, RoutedEventArgs e)
    {
        VersionChecker.UpdateResourceAsync();
    }

    private void CheckResourceUpdate(object sender, RoutedEventArgs e)
    {
        VersionChecker.CheckResourceVersionAsync();
    }

    private void UpdateMFA(object sender, RoutedEventArgs e)
    {
        VersionChecker.UpdateMFAAsync();
    }
    private void UpdateMaaFW(object sender, RoutedEventArgs e)
    {
        VersionChecker.UpdateMaaFwAsync();
    }
    private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo(((Hyperlink)sender).NavigateUri.AbsoluteUri)
        {
            UseShellExecute = true
        });
    }
    private void ExternalNotificationSendTest(object sender, RoutedEventArgs e)
    {
        MaaProcessor.ExternalNotificationAsync();
    }
    private void AddAbout()
    {
        StackPanel s1 = new()
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Center
            },
            s2 = new()
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(3),
                HorizontalAlignment = HorizontalAlignment.Center
            };
        var t1 = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        t1.BindLocalization("ProjectLink", TextBlock.TextProperty);
        s1.Children.Add(t1);
        s1.Children.Add(new Shield
        {
            Status = "MFAWPF",
            Subject = "Github",
            Margin = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Center,
            Command = ControlCommands.OpenLink,
            CommandParameter = "https://github.com/SweetSmellFox/MFAWPF"
        });
        var resourceLink = MaaInterface.Instance?.Url;
        if (!string.IsNullOrWhiteSpace(resourceLink))
        {
            var t2 = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2)
            };
            t2.BindLocalization("ResourceLink", TextBlock.TextProperty);
            s2.Children.Add(t2);
            s2.Children.Add(new Shield
            {
                Status = MaaInterface.Instance?.Name ?? "Resource",
                Subject = "Github",
                Margin = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Center,
                Command = ControlCommands.OpenLink,
                CommandParameter = resourceLink
            });
        }
       
        // var resourceVersion = MaaInterface.Instance?.Version;
        // if (!string.IsNullOrWhiteSpace(resourceVersion))
        // {
        //     ViewModel.ResourceVersion = resourceVersion;
        // }
        // else
        // {
        //     ResourceShield.Visibility = Visibility.Collapsed;
        // }
        settingStackPanel.Children.Add(s1);
        settingStackPanel.Children.Add(s2);
    }

    private void File_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        // Note that you can have more than one file.
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (sender is TextBox textBox)
            textBox.Text = files?[0] ?? string.Empty;
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

    private void SetRememberAdbOption(CheckBox checkBox)
    {
        checkBox.IsChecked = DataSet.GetData("RememberAdb", true);
        checkBox.BindLocalization("RememberAdb", ContentProperty);
        checkBox.Click += (_, _) => { DataSet.SetData("RememberAdb", checkBox.IsChecked); };
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
