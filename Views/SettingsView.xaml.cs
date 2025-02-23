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
using System.Windows.Media;
using System.Windows.Threading;
using ComboBox = System.Windows.Controls.ComboBox;
using ScrollViewer = HandyControl.Controls.ScrollViewer;
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
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateDividerPositions();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateDividerPositions();
    }

    private void ScrollViewer_LayoutUpdated(object sender, EventArgs e)
    {
        UpdateDividerPositions();
    }

    private void UpdateDividerPositions()
    {
        if (Viewer.Content is Grid grid)
        {
            var stackPanel = grid.Children.OfType<StackPanel>().FirstOrDefault();
            if (stackPanel == null) return;

            double currentY = 0;
            var dividerPositions = new List<double>();
            foreach (var child in stackPanel.Children)
            {
                if (child is FrameworkElement element)
                {
                    if (child is Divider)
                    {
                        dividerPositions.Add(currentY);
                    }
                    currentY += element.ActualHeight + element.Margin.Top + element.Margin.Bottom;
                }
            }
            ViewModel.DividerVerticalOffsetList = dividerPositions;
        }
    }
}
