using HandyControl.Controls;

namespace MFAWPF.Utils;

using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class VersionChecker
{
    private static VersionChecker CHECKER = new();

    public static void CheckVersion()
    {
        TaskManager.RunTaskAsync(() => { CHECKER.CheckForUpdatesAsync(); }, null, "检测版本");
    }

    public async Task CheckForUpdatesAsync(string owner = "SweetSmellFox", string repo = "MFAWPF")
    {
        string latestVersion = await GetLatestVersionFromGitHub(owner, repo);
        string localVersion = GetLocalVersion();
        if (IsNewVersionAvailable(latestVersion, localVersion))
        {
            Growl.Info("新版本可用！最新版本: " + latestVersion);
        }
    }

    private async Task<string> GetLatestVersionFromGitHub(string owner, string repo)
    {
        string url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("request");

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync();
            JObject releaseData = JObject.Parse(jsonResponse);
            return releaseData["tag_name"].ToString(); // 从name字段获取版本信息
        }
    }

    private string GetLocalVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }

    private bool IsNewVersionAvailable(string latestVersion, string localVersion)
    {
        try
        {
            string latestVersionNumber = ExtractVersionNumber(latestVersion);
            string localVersionNumber = ExtractVersionNumber(localVersion);

            Version latest = new Version(latestVersionNumber);
            Version local = new Version(localVersionNumber);

            return latest.CompareTo(local) > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            LoggerService.Logger.LogError(ex);
            return false;
        }
    }

    private string ExtractVersionNumber(string versionString)
    {
        string[] parts = versionString.Replace(" v", " ").Split(' ');

        foreach (var part in parts)
        {
            if (Version.TryParse(part, out _))
                return part;
        }

        throw new FormatException("无法解析版本号: " + versionString);
    }
}