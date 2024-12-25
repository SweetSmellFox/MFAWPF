using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using HandyControl.Controls;
using HandyControl.Data;
using MFAWPF.Data;
using MFAWPF.Res.Localization;
using MFAWPF.Utils.Converters;
using MFAWPF.Views;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Reflection;
using System.Windows;

namespace MFAWPF.Utils;
// 移除了一些不必要的using，只保留实际用到的相关命名空间引用

public class VersionChecker
{
    private static readonly VersionChecker Checker = new();
    public Queue<MFATask> Queue = new();

    // 修改后的Check方法，改为同步执行逻辑，按顺序执行相关任务
    public static void Check()
    {
        if (DataSet.GetData("EnableAutoUpdateResource", false))
        {
            Checker.Queue.Enqueue(new MFATask
            {
                Action = () => Checker.UpdateResource(DataSet.GetData("EnableAutoUpdateMFA", false), () =>
                {
                    if (DataSet.GetData("EnableAutoUpdateMFA", false))
                        Checker.UpdateMFA();
                }),
                Name = "更新资源"
            });
        }
        else if (DataSet.GetData("EnableCheckVersion", true))
        {
            Checker.Queue.Enqueue(new MFATask
            {
                Action = () => Checker.CheckForResourceUpdates(),
                Name = "检测资源版本"
            });
        }

        if (DataSet.GetData("EnableAutoUpdateMFA", false) && !DataSet.GetData("EnableAutoUpdateResource", false))
        {
            Checker.Queue.Enqueue(new MFATask
            {
                Action = () => Checker.UpdateMFA(),
                Name = "更新软件"
            });
        }
        else if (DataSet.GetData("EnableCheckVersion", true))
        {
            Checker.Queue.Enqueue(new MFATask
            {
                Action = () => Checker.CheckForGUIUpdates(),
                Name = "检测资源版本"
            });
        }

        TaskManager.RunTaskAsync(() => Checker.ExecuteTasks(), () =>
        {
            Growls.Error("自动更新时发生错误！");
        }, "启动检测");
    }

    // 同步执行任务队列中的任务，保证顺序执行
    private void ExecuteTasks()
    {
        while (Queue.Count > 0)
        {
            var task = Queue.Dequeue();
            if (!task.Run())
            {
                break;
            }
        }
    }

    public static void CheckGUIVersionAsync()
    {
        TaskManager.RunTaskAsync(() => Checker.CheckForGUIUpdates());
    }

    public static void CheckResourceVersionAsync()
    {
        TaskManager.RunTaskAsync(() => Checker.CheckForResourceUpdates());
    }

    public static void UpdateResourceAsync()
    {
        TaskManager.RunTaskAsync(() => Checker.UpdateResource());

    }

    public static void UpdateMFAAsync()
    {
        TaskManager.RunTaskAsync(() => Checker.UpdateMFA());

    }

    public async void UpdateResource(bool closeDialog = false, Action? action = null)
    {
        MainWindow.Instance.SetUpdating(true);
        DownloadDialog? dialog = null;
        Growls.Process(() =>
        {
            dialog = new DownloadDialog("UpdateResource".GetLocalizationString());
            dialog.Show();
        });

        dialog.SetText("GettingLatestResources".GetLocalizationString());

        var url = MaaInterface.Instance?.Url ?? string.Empty;
        dialog.UpdateProgress(10);
        var strings = GetRepoFromUrl(url);

        var latestVersion = strings.Length > 1 ? GetLatestVersionFromGithub(strings[0], strings[1]) : string.Empty;
        dialog.UpdateProgress(50);

        if (string.IsNullOrWhiteSpace(latestVersion))
        {
            Growls.ErrorGlobal("FailToGetLatestVersionInfo".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            Growls.Process(() =>
            {
                dialog.Close();
            });
            return;
        }

        var localVersion = MaaInterface.Instance?.Version ?? string.Empty;

        if (string.IsNullOrWhiteSpace(localVersion))
        {
            Growls.ErrorGlobal("FailToGetCurrentVersionInfo".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            Growls.Process(() =>
            {
                dialog.Close();
            });
            return;
        }

        if (!IsNewVersionAvailable(latestVersion, localVersion))
        {
            Growl.InfoGlobal("ResourcesAreLatestVersion".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            Growls.Process(() =>
            {
                dialog.Close();
            });
            return;
        }

        var downloadUrl = GetDownloadUrlFromGitHubRelease(latestVersion, strings[0], strings[1]);
        dialog.UpdateProgress(100);

        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            Growls.ErrorGlobal("FailToGetDownloadUrl".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            return;
        }

        var tempPath = Path.Combine(AppContext.BaseDirectory, "temp");
        if (!Directory.Exists(tempPath))
        {
            Directory.CreateDirectory(tempPath);
        }
        var tempZipFilePath = Path.Combine(tempPath, $"resource_{latestVersion}.zip");
        dialog.SetText("Downloading".GetLocalizationString());
        dialog.UpdateProgress(0);
        await DownloadFileAsync(downloadUrl, tempZipFilePath, dialog, () => { });
        dialog.SetText("ApplyingUpdate".GetLocalizationString());
        dialog.UpdateProgress(5);

        var tempExtractDir = Path.Combine(tempPath, $"resource_{latestVersion}_extracted");
        if (Directory.Exists(tempExtractDir))
        {
            Directory.Delete(tempExtractDir, true);
        }
        ZipFile.ExtractToDirectory(tempZipFilePath, tempExtractDir);
        dialog.UpdateProgress(50);

        var interfacePath = Path.Combine(tempExtractDir, "interface.json");
        var resourceDirPath = Path.Combine(tempExtractDir, "resource");

        string wpfDir = AppContext.BaseDirectory;
        var file = new FileInfo(interfacePath);
        if (file.Exists)
        {
            var targetPath = Path.Combine(wpfDir, "interface.json");
            file.CopyTo(targetPath, true);
        }
        dialog.UpdateProgress(60);

        var di = new DirectoryInfo(resourceDirPath);
        if (di.Exists)
        {
            CopyFolder(resourceDirPath, Path.Combine(wpfDir, "resource"));
        }
        dialog.UpdateProgress(70);

        File.Delete(tempZipFilePath);
        Directory.Delete(tempExtractDir, true);
        dialog.UpdateProgress(80);

        var newInterfacePath = Path.Combine(wpfDir, "interface.json");
        if (File.Exists(newInterfacePath))
        {
            var jsonContent = File.ReadAllText(newInterfacePath);
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            settings.Converters.Add(new MaaInterfaceSelectOptionConverter(true));

            var @interface = JsonConvert.DeserializeObject<MaaInterface>(jsonContent, settings);
            if (@interface != null)
            {
                @interface.Url = MaaInterface.Instance?.Url;
                @interface.Version = latestVersion;
            }
            var updatedJsonContent = JsonConvert.SerializeObject(@interface, settings);

            File.WriteAllText(newInterfacePath, updatedJsonContent);
        }
        dialog.UpdateProgress(100);

        dialog.SetText("UpdateCompleted".GetLocalizationString());
        dialog.Dispatcher.Invoke(() =>
        {
            dialog.RestartButton.Visibility = Visibility.Visible;
        });
        MainWindow.Instance.SetUpdating(false);
        Growls.Process(() =>
        {
            if (closeDialog)
                dialog.Close();
        });
        action?.Invoke();
    }

    public async void UpdateMFA()
    {
        MainWindow.Instance.SetUpdating(true);

        DownloadDialog? dialog = null;
        Growls.Process(() =>
        {
            dialog = new DownloadDialog("SoftwareUpdate".GetLocalizationString());
            dialog.Show();
        });

        dialog.SetText("GettingLatestResources".GetLocalizationString());

        var url = "https://github.com/SweetSmellFox/MFAWPF";

        dialog.UpdateProgress(10);

        var strings = GetRepoFromUrl(url);
        var latestVersion = GetLatestVersionFromGithub();
        dialog.UpdateProgress(50);

        if (string.IsNullOrWhiteSpace(latestVersion))
        {
            Growls.ErrorGlobal("FailToGetLatestVersionInfo".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            Growls.Process(() =>
            {
                dialog.Close();
            });
            return;
        }

        var localVersion = GetLocalVersion();

        if (string.IsNullOrWhiteSpace(localVersion))
        {
            Growls.ErrorGlobal("FailToGetCurrentVersionInfo".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            Growls.Process(() =>
            {
                dialog.Close();
            });
            return;
        }

        if (!IsNewVersionAvailable(latestVersion, localVersion))
        {
            Growl.InfoGlobal("MFAIsLatestVersion".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            Growls.Process(() =>
            {
                dialog.Close();
            });
            return;
        }

        var downloadUrl = GetDownloadUrlFromGitHubRelease(latestVersion, strings[0], strings[1]);
        dialog.UpdateProgress(100);

        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            Growls.ErrorGlobal("FailToGetDownloadUrl".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            return;
        }

        var tempPath = Path.Combine(AppContext.BaseDirectory, "temp");
        if (!Directory.Exists(tempPath))
        {
            Directory.CreateDirectory(tempPath);
        }
        var tempZipFilePath = Path.Combine(tempPath, $"mfa_{latestVersion}.zip");
        dialog.SetText("Downloading".GetLocalizationString());
        dialog.UpdateProgress(0);
        await DownloadFileAsync(downloadUrl, tempZipFilePath, dialog, () => { });
        var tempExtractDir = Path.Combine(tempPath, $"mfa_{latestVersion}_extracted");
        if (Directory.Exists(tempExtractDir))
        {
            Directory.Delete(tempExtractDir, true);
        }
        ZipFile.ExtractToDirectory(tempZipFilePath, tempExtractDir);

        var currentExeFileName = Assembly.GetEntryAssembly().GetName().Name + ".exe";

        var batFilePath = Path.Combine(AppContext.BaseDirectory, "temp", "update_mfa.bat");
        using (StreamWriter sw = new StreamWriter(batFilePath))
        {
            sw.WriteLine("ping 127.0.0.1 -n 3 > nul");
            sw.WriteLine($"xcopy /E /Y \"{AppContext.BaseDirectory}temp\\mfa_{latestVersion}_extracted\\*.*\" \"{AppContext.BaseDirectory}\"");
            sw.WriteLine($"start /d \"{AppContext.BaseDirectory}\" {currentExeFileName}");
            sw.WriteLine("ping 127.0.0.1 -n 1 > nul");
            sw.WriteLine($"rd /S /Q \"{AppContext.BaseDirectory}temp\"");
        }

        var psi = new ProcessStartInfo(batFilePath)
        {
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        Process.Start(psi);
        Growls.Process(Application.Current.Shutdown);
    }

    static void CopyFolder(string sourceFolder, string destinationFolder)
    {
        if (!Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
        }
        var files = Directory.GetFiles(sourceFolder);
        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            string destinationFile = Path.Combine(destinationFolder, fileName);
            File.Copy(file, destinationFile, true);
        }
        var subDirectories = Directory.GetDirectories(sourceFolder);
        foreach (string subDirectory in subDirectories)
        {
            string subDirectoryName = Path.GetFileName(subDirectory);
            string destinationSubDirectory = Path.Combine(destinationFolder, subDirectoryName);
            CopyFolder(subDirectory, destinationSubDirectory);
        }
    }

    private string GetDownloadUrlFromGitHubRelease(string version, string owner, string repo)
    {
        var releaseUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/tags/{version}";
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("request");
        httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
        try
        {
            var response = httpClient.GetAsync(releaseUrl).Result;
            if (response.IsSuccessStatusCode)
            {
                var read = response.Content.ReadAsStringAsync();
                read.Wait();
                var jsonResponse = read.Result;
                var releaseData = JObject.Parse(jsonResponse);
                if (releaseData["assets"] is JArray assets && assets.Count > 0)
                {
                    var targetUrl = "";
                    foreach (var asset in assets)
                    {
                        var browserDownloadUrl = asset["browser_download_url"]?.ToString();
                        if (!string.IsNullOrEmpty(browserDownloadUrl))
                        {
                            if (browserDownloadUrl.EndsWith(".zip") || browserDownloadUrl.EndsWith(".7z") || browserDownloadUrl.EndsWith(".rar"))
                            {
                                targetUrl = browserDownloadUrl;
                                break;
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(targetUrl))
                    {
                        targetUrl = assets[0]["browser_downloadUrl"]?.ToString();
                    }
                    return targetUrl;
                }
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden && response.ReasonPhrase.Contains("403"))
            {
                LoggerService.LogError("GitHub API速率限制已超出，请稍后再试。");
                throw new Exception("GitHub API速率限制已超出，请稍后再试。");
            }
            else
            {
                LoggerService.LogError($"请求GitHub时发生错误: {response.StatusCode} - {response.ReasonPhrase}");
                throw new Exception($"请求GitHub时发生错误: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception e)
        {
            LoggerService.LogError($"处理GitHub响应时发生错误: {e.Message}");
            throw new Exception($"处理GitHub响应时发生错误: {e.Message}");
        }
        finally
        {
            httpClient.Dispose();
        }

        return string.Empty;
    }

    private async Task DownloadFileAsync(string url, string filePath, DownloadDialog dialog, Action action)
    {
        using var client = new WebClient();
        client.Headers.Add("User-Agent", "request");
        client.Headers.Add("Accept", "application/json");

        client.DownloadFileCompleted += (_, _) =>
        {
            action();
        };
        client.DownloadProgressChanged += (_, e) =>
        {
            var progressPercentage = (double)e.BytesReceived / e.TotalBytesToReceive * 100;
            dialog.UpdateProgress(progressPercentage);
        };

        await client.DownloadFileTaskAsync(url, filePath);
    }


    public void CheckForGUIUpdates()
    {
        try
        {
            MainWindow.Instance.SetUpdating(true);
            var latestVersion = GetLatestVersionFromGithub();
            var localVersion = GetLocalVersion();
            if (IsNewVersionAvailable(latestVersion, localVersion))
            {
                Growl.Info("MFA" + "NewVersionAvailableLatestVersion".GetLocalizationString() + latestVersion);
            }
            MainWindow.Instance.SetUpdating(false);
        }
        catch (Exception ex)
        {
            Growls.Error($"检查MFA最新版时发生错误: {ex.Message}");
            MainWindow.Instance.SetUpdating(false);
            LoggerService.LogError(ex);
        }
    }

    private static string[] GetRepoFromUrl(string githubUrl)
    {
        var pattern = @"^https://github\.com/(?<owner>[^/]+)/(?<repo>[^/]+)$";
        var match = Regex.Match(githubUrl, pattern);

        if (match.Success)
        {
            string owner = match.Groups["owner"].Value;
            string repo = match.Groups["repo"].Value;

            return
            [

                owner,
                repo
            ];
        }

        throw new FormatException("输入的 GitHub URL 格式不正确: " + githubUrl);
    }

    private static string ConvertToApiUrl(string githubUrl)
    {
        var pattern = @"^https://github\.com/(?<owner>[^/]+)/(?<repo>[^/]+)$";
        var match = Regex.Match(githubUrl, pattern);

        if (match.Success)
        {
            string owner = match.Groups["owner"].Value;
            string repo = match.Groups["repo"].Value;

            return $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
        }

        throw new FormatException("输入的 GitHub URL 格式不正确: " + githubUrl);
    }

    public void CheckForResourceUpdates()
    {
        MainWindow.Instance.SetUpdating(true);
        var url = MaaInterface.Instance?.Url ?? string.Empty;
        var strings = GetRepoFromUrl(url);
        try
        {
            var latestVersion = strings.Length > 1 ? GetLatestVersionFromGithub(strings[0], strings[1]) : string.Empty;
            if (string.IsNullOrWhiteSpace(latestVersion))
            {
                MainWindow.Instance.SetUpdating(false);
                return;
            }

            var localVersion = GetResourceVersion();
            if (string.IsNullOrWhiteSpace(localVersion))
            {
                MainWindow.Instance.SetUpdating(false);
                return;
            }

            if (IsNewVersionAvailable(latestVersion, localVersion))
            {
                Growl.Info("ResourceOption".GetLocalizationString() + "NewVersionAvailableLatestVersion".GetLocalizationString() + latestVersion);
            }
            else
            {
                Growl.Info("ResourcesAreLatestVersion".GetLocalizationString());
            }
            MainWindow.Instance.SetUpdating(false);
        }
        catch (Exception ex)
        {
            MainWindow.Instance.SetUpdating(false);
            Growls.Error($"检查资源最新版时发生错误: {ex.Message}");
            LoggerService.LogError(ex);
        }
    }

    // private string GetLatestVersionFromGitHubUrl(string url)
    // {
    //
    //     if (string.IsNullOrWhiteSpace(url))
    //     {
    //         return string.Empty;
    //     }
    //     using var client = new WebClient();
    //     client.Headers.Add("User-Agent", "request");
    //     client.Headers.Add("Accept", "application/json");
    //
    //     try
    //     {
    //         string jsonResponse = client.DownloadString(url);
    //         var releaseData = JObject.Parse(jsonResponse);
    //
    //         return releaseData["tag_name"]?.ToString();
    //     }
    //     catch (WebException e) when (e.Message.Contains("403"))
    //     {
    //         LoggerService.LogError("GitHub API速率限制已超出，请稍后再试。");
    //         throw;
    //     }
    //     catch (WebException e)
    //     {
    //         LoggerService.LogError($"请求GitHub时发生错误: {e.Message}");
    //         throw;
    //     }
    //     catch (Exception e)
    //     {
    //         LoggerService.LogError($"处理GitHub响应时发生错误: {e.Message}");
    //         throw;
    //     }
    // }
    //
    public string GetLatestVersionFromGithub(string owner = "SweetSmellFox", string repo = "MFAWPF")
    {
        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
            return string.Empty;
        var releaseUrl = $"https://api.github.com/repos/{owner}/{repo}/releases";
        int page = 1;
        const int perPage = 5;
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("request");
        httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");

        while (page < 101)
        {
            var urlWithParams = $"{releaseUrl}?per_page={perPage}&page={page}";
            try
            {
                var response = httpClient.GetAsync(urlWithParams).Result;
                if (response.IsSuccessStatusCode)
                {
                    var read = response.Content.ReadAsStringAsync();
                    read.Wait();
                    string json = read.Result;
                    var tags = JArray.Parse(json);
                    if (tags.Count == 0)
                    {
                        break;
                    }
                    foreach (var tag in tags)
                    {
                        if ((bool)tag["prerelease"])
                        {
                            continue;
                        }
                        return tag["tag_name"]?.ToString();
                    }
                }
                else if (response.StatusCode == HttpStatusCode.Forbidden && response.ReasonPhrase.Contains("403"))
                {
                    LoggerService.LogError("GitHub API速率限制已超出，请稍后再试。");
                    throw new Exception("GitHub API速率限制已超出，请稍后再试。");
                }
                else
                {
                    LoggerService.LogError($"请求GitHub时发生错误: {response.StatusCode} - {response.ReasonPhrase}");
                    throw new Exception($"请求GitHub时发生错误: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception e)
            {
                LoggerService.LogError($"处理GitHub响应时发生错误: {e.Message}");
                throw new Exception($"处理GitHub响应时发生错误: {e.Message}");
            }
            finally
            {
                httpClient.Dispose();
            }
            page++;
        }
        return string.Empty;
    }

    // private string GetLatestVersionFromGitHub(string owner, string repo)
    // {
    //     var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
    //     using var client = new WebClient();
    //     client.Headers.Add("User-Agent", "request");
    //     client.Headers.Add("Accept", "application/json");
    //
    //     try
    //     {
    //         string jsonResponse = client.DownloadString(url);
    //         var releaseData = JObject.Parse(jsonResponse);
    //         return releaseData["tag_name"]?.ToString();
    //     }
    //     catch (WebException e) when (e.Message.Contains("403"))
    //     {
    //         LoggerService.LogError("GitHub API速率限制已超出，请稍后再试。");
    //         throw;
    //     }
    //     catch (WebException e)
    //     {
    //         LoggerService.LogError($"请求GitHub时发生错误: {e.Message}");
    //         throw;
    //     }
    //     catch (Exception e)
    //     {
    //         LoggerService.LogError($"处理GitHub响应时发生错误: {e.Message}");
    //         throw;
    //     }
    // }

    private string GetLocalVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "DEBUG";
    }

    private string GetResourceVersion()
    {
        return MaaInterface.Instance?.Version ?? "DEBUG";
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
            LoggerService.LogError(ex);
            return false;
        }
    }

    private string ExtractVersionNumber(string versionString)
    {
        if (versionString == "Debug")
            versionString = "0.0.1";

        if (versionString.StartsWith("v") || versionString.StartsWith("V"))
        {
            versionString = versionString.Substring(1);
        }

        var parts = versionString.Split('-');
        var mainVersionPart = parts[0];

        var versionComponents = mainVersionPart.Split('.');
        while (versionComponents.Length < 4)
        {
            mainVersionPart += ".0";
            versionComponents = mainVersionPart.Split('.');
        }

        if (Version.TryParse(mainVersionPart, out _))
        {
            return mainVersionPart;
        }

        throw new FormatException("无法解析版本号: " + versionString);
    }
}
