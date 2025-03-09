using MFAWPF.Configuration;
using MFAWPF.Helper;
using MFAWPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace MFAWPF.Views.UI;

public partial class NotifyIcon
{
    private readonly int _menuItemNum;

    public NotifyIcon()
    {
        InitializeComponent();

        InitIcon();

        if (notifyIcon.ContextMenu is not null)
        {
            _menuItemNum = notifyIcon.ContextMenu.Items.Count;
        }

    }

    private void InitIcon()
    {
        notifyIcon.Icon = IconHelper.ICON;
        notifyIcon.Visibility = ConfigurationHelper.GetValue(ConfigurationKeys.EnableShowIcon, true) ? Visibility.Visible : Visibility.Collapsed;

        notifyIcon.Click += NotifyIcon_MouseClick;
        notifyIcon.MouseDoubleClick += NotifyIcon_MouseClick;

        startMenu.Click += StartTask;
        stopMenu.Click += StopTask;
        exitMenu.Click += App_exit;
        hideMenu.Click += App_hide;
        showMenu.Click += App_show;
        foreach (var lang in LanguageHelper.SupportedLanguages)
        {
            var langMenu = new MenuItem
            {
                Header = lang.Name
            };
            langMenu.Click += (_, _) =>
            {
                LanguageHelper.ChangeLanguage(lang);
                var index = LanguageHelper.SupportedLanguages.ToList().FindIndex(language => language.Key == lang.Key);
                ConfigurationHelper.SetValue(ConfigurationKeys.LangIndex, index == -1 ? 0 : index);
            };

            switchLangMenu.Items.Add(langMenu);
        }
    }

    // 不知道是干嘛的，先留着
    // ReSharper disable once UnusedMember.Local
    private void AddMenuItemOnFirst(string text, Action action)
    {
        var menuItem = new MenuItem
        {
            Header = text
        };
        menuItem.Click += (_, _) => { action?.Invoke(); };
        if (notifyIcon.ContextMenu is null)
        {
            return;
        }

        if (notifyIcon.ContextMenu.Items.Count == _menuItemNum)
        {
            notifyIcon.ContextMenu.Items.Insert(0, menuItem);
        }
        else
        {
            notifyIcon.ContextMenu.Items[0] = menuItem;
        }
    }

    private static void NotifyIcon_MouseClick(object sender, RoutedEventArgs e) =>
        Instances.RootView.ShowWindow();


    private static void StartTask(object sender, RoutedEventArgs e) =>
        Instances.TaskQueueView.Start();


    private static void StopTask(object sender, RoutedEventArgs e) =>
        Instances.TaskQueueView.Stop();


    private static void App_exit(object sender, RoutedEventArgs e)
    {
        if (Instances.RootView.ConfirmExit())
            Application.Current.Shutdown();
    }

    private void App_hide(object sender, RoutedEventArgs e) =>
        Instances.RootViewModel.IsVisible = false;


    private void App_show(object sender, RoutedEventArgs e)
        => Instances.RootViewModel.IsVisible = true;
}
