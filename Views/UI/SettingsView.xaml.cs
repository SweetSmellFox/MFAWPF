using HandyControl.Controls;
using MFAWPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using ComboBox = System.Windows.Controls.ComboBox;
using ScrollViewer = HandyControl.Controls.ScrollViewer;
using TextBox = System.Windows.Controls.TextBox;

namespace MFAWPF.Views.UI;

public partial class SettingsView
{
    public static ViewModels.SettingsViewModel ViewModel { get; set; }
    public SettingsView()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        DataContext = this;
        Loaded += (_, _) => { UpdateDividerPositions(); };
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
