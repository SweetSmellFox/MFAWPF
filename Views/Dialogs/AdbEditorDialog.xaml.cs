using System.Windows;
using MaaFramework.Binding;
using MFAWPF.Helper;
using Microsoft.Win32;

namespace MFAWPF.Views;

public partial class AdbEditorDialog
{
    public AdbEditorDialog(AdbDeviceInfo? info = null)
    {
        InitializeComponent();
        if (info != null)
        {
            AdbName = info.Name;
            AdbPath = info.AdbPath;
            AdbSerial = info.AdbSerial;
            AdbConfig = info.Config;
        }
    }

    private void Load(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Title = "LoadFileTitle".ToLocalization(),
            Filter = "AllFilter".ToLocalization()
        };

        if (openFileDialog.ShowDialog().IsTrue())
        {
            AdbPath = openFileDialog.FileName;
        }
    }

    private void Save(object sender, RoutedEventArgs e)
    {
        Console.WriteLine($"{AdbName},{AdbPath},{AdbSerial}");
        DialogResult = true;
    }

    private void Cancel(object sender, RoutedEventArgs e)
    {
        Close();
    }

    public static readonly DependencyProperty AdbNameProperty =
        DependencyProperty.Register(
            nameof(AdbName),
            typeof(string),
            typeof(AdbEditorDialog),
            new FrameworkPropertyMetadata(
                "Emulator".ToLocalization(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string AdbName
    {
        get => (string)GetValue(AdbNameProperty);
        set => SetValue(AdbNameProperty, value);
    }

    public static readonly DependencyProperty AdbPathProperty =
        DependencyProperty.Register(
            nameof(AdbPath),
            typeof(string),
            typeof(AdbEditorDialog),
            new FrameworkPropertyMetadata(
                string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string AdbPath
    {
        get => (string)GetValue(AdbPathProperty);
        set => SetValue(AdbPathProperty, value);
    }

    public static readonly DependencyProperty AdbSerialProperty =
        DependencyProperty.Register(
            nameof(AdbSerial),
            typeof(string),
            typeof(AdbEditorDialog),
            new FrameworkPropertyMetadata(
                string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string AdbSerial
    {
        get => (string)GetValue(AdbSerialProperty);
        set => SetValue(AdbSerialProperty, value);
    }

    public static readonly DependencyProperty AdbConfigProperty =
        DependencyProperty.Register(
            nameof(AdbConfig),
            typeof(string),
            typeof(AdbEditorDialog),
            new FrameworkPropertyMetadata(
                "{}", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string AdbConfig
    {
        get => (string)GetValue(AdbConfigProperty);
        set => SetValue(AdbConfigProperty, value);
    }

    public AdbDeviceInfo Output => new (AdbName, AdbPath, AdbSerial, AdbScreencapMethods.Default,
        AdbInputMethods.MinitouchAndAdbKey, AdbConfig);
}