using CommunityToolkit.Mvvm.ComponentModel;
using HandyControl.Tools.Extension;
using MFAWPF.Configuration;
using MFAWPF.Helper;
using MFAWPF.Views.UI;
using System.IO;
using System.Windows;
using Exception = System.Exception;

namespace MFAWPF.ViewModels.UI;

public partial class AnnouncementViewModel : ViewModel
{
    public static readonly string AnnouncementFileName = "Announcement.md";
    [ObservableProperty] private string _announcementInfo = string.Empty;

    [ObservableProperty] private bool _doNotRemindThisAnnouncementAgain = Convert.ToBoolean(GlobalConfiguration.GetValue(ConfigurationKeys.DoNotShowAgain, bool.FalseString));
    partial void OnDoNotRemindThisAnnouncementAgainChanged(bool value)
    {
        GlobalConfiguration.SetValue(ConfigurationKeys.DoNotShowAgain, value.ToString());
    }


    public void CheckAnnouncement()
    {
        if (DoNotRemindThisAnnouncementAgain) return;
        try
        {
            var resourcePath = Path.Combine(AppContext.BaseDirectory, "resource");
            var mdPath = Path.Combine(resourcePath, AnnouncementFileName);

            if (File.Exists(mdPath))
            {
                var content = File.ReadAllText(mdPath);
                AnnouncementInfo = content;
            }
        }
        catch (Exception ex)
        {
            LoggerService.LogError($"读取公告文件失败: {ex.Message}");
            AnnouncementInfo = "";
        }
        finally
        {

            if (!string.IsNullOrWhiteSpace(AnnouncementInfo) && !AnnouncementInfo.Trim().Equals("placeholder", StringComparison.OrdinalIgnoreCase))
            {
                var announcementView = new AnnouncementView();
                announcementView.Show();
            }
        }
    }
}
