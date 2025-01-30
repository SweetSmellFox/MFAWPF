using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using HandyControl.Controls;
using MFAWPF.Data;
using MFAWPF.Utils.Converters;
using MFAWPF.ViewModels;
using MFAWPF.Views;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO.Compression;
using System.Management;
using System.Net.Http;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using XNetEx.Guids.Generators;

namespace MFAWPF.Utils;

public class VersionChecker
{
    private static readonly VersionChecker Checker = new();
    public Queue<MFATask> Queue = new();

    public static void Check()
    {
        if (DataSet.GetData("EnableAutoUpdateResource", false))
        {
            Checker.Queue.Enqueue(new MFATask
            {
                Action = () => Checker.UpdateResourceBySelection(DataSet.GetData("EnableAutoUpdateMFA", false), true, () =>
                {
                    if (DataSet.GetData("EnableAutoUpdateMFA", false))
                        Checker.UpdateMFA(true);
                }),
                Name = "更新资源"
            });
        }
        else if (DataSet.GetData("EnableCheckVersion", true))
        {
            Checker.Queue.Enqueue(new MFATask
            {
                Action = () => Checker.CheckResourceBySelection(),
                Name = "检测资源版本"
            });
        }

        if (DataSet.GetData("EnableAutoUpdateMFA", false) && !DataSet.GetData("EnableAutoUpdateResource", false))
        {
            Checker.Queue.Enqueue(new MFATask
            {
                Action = () => Checker.UpdateMFA(true),
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

        TaskManager.RunTaskAsync(() => Checker.ExecuteTasks(), () => Growls.Error("自动更新时发生错误！"), "启动检测");
    }

    private void ExecuteTasks()
    {
        while (Queue.Count > 0)
        {
            var task = Queue.Dequeue();
            if (!task.Run()) break;
        }
    }

    public static void CheckGUIVersionAsync() => TaskManager.RunTaskAsync(() => Checker.CheckForGUIUpdates());
    public static void CheckResourceVersionAsync() => TaskManager.RunTaskAsync(() => Checker.CheckResourceBySelection());
    public static void UpdateResourceAsync() => TaskManager.RunTaskAsync(() => Checker.UpdateResourceBySelection());
    public static void UpdateMFAAsync() => TaskManager.RunTaskAsync(() => Checker.UpdateMFA());

    public void SetText(string text, DownloadDialog? dialog, bool noDialog = false)
    {
        if (noDialog)
            MainWindow.Data.OutputDownloadProgress(text.GetLocalizationString(), false);
        else
            dialog?.SetText(text.GetLocalizationString());
    }

    public void CheckResourceBySelection()
    {
        switch (MainWindow.Data.DownloadSourceIndex)
        {
            case 0:
                CheckForResourceUpdates();
                break;
            case 1:
                CheckResourceVersionWithMirrorApi();
                break;
        }
    }

     public async void UpdateResourceBySelection(bool closeDialog = false, bool noDialog = false, Action? action = null)
    {
        switch (MainWindow.Data.DownloadSourceIndex)
        {
            case 0: UpdateResource(closeDialog, noDialog, action); break;
            case 1: UpdateResourceWithMirrorApi(closeDialog, noDialog, action); break;
        }
    }

    public async void UpdateResourceWithMirrorApi(bool closeDialog = false, bool noDialog = false, Action? action = null)
    {
        MainWindow.Instance.SetUpdating(true);
        DownloadDialog? dialog = null;
        Growls.Process(() =>
        {
            if (!noDialog) dialog = new DownloadDialog("UpdateResource".GetLocalizationString());
            dialog?.Show();
        });

        var resid = MaaInterface.Instance?.Name ?? string.Empty;
        if (string.IsNullOrEmpty(resid))
        {
            MainWindow.Instance.SetUpdating(false);
            Growls.Process(() => dialog?.Close());
            return;
        }

        SetText("GettingLatestResources", dialog, noDialog);

        var resId = GetResourceID();
        var currentVersion = GetResourceVersion();
        var cdk = SimpleEncryptionHelper.Decrypt(DataSet.GetData("DownloadCDK", string.Empty));
        var spId = GetDeviceId().ToString();
        dialog?.UpdateProgress(10);

        if (string.IsNullOrWhiteSpace(currentVersion) || string.IsNullOrWhiteSpace(resId))
        {
            Growls.ErrorGlobal("FailToGetCurrentVersionInfo".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            Growls.Process(() => dialog?.Close());
            MainWindow.Data.ClearDownloadProgress();
            return;
        }

        string downloadUrl, latestVersion;
        try
        {
            GetDownloadUrlFromMirror(currentVersion, resId, spId, cdk, out downloadUrl, out latestVersion);
        }
        catch (Exception ex)
        {
            SetText(ex.Message, dialog, noDialog);
            MainWindow.Instance.SetUpdating(false);
            LoggerService.LogError(ex);
            return;
        }

        dialog?.UpdateProgress(50);

        if (string.IsNullOrWhiteSpace(latestVersion))
        {
            Growls.ErrorGlobal("FailToGetLatestVersionInfo".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            Growls.Process(() => dialog?.Close());
            MainWindow.Data.ClearDownloadProgress();
            return;
        }

        var localVersion = MaaInterface.Instance?.Version ?? string.Empty;

        if (!IsNewVersionAvailable(latestVersion, localVersion))
        {
            Growl.InfoGlobal("ResourcesAreLatestVersion".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            Growls.Process(() => dialog?.Close());
            MainWindow.Data.ClearDownloadProgress();
            action?.Invoke();
            return;
        }

        dialog?.UpdateProgress(100);

        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            Growls.ErrorGlobal("FailToGetDownloadUrl".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            MainWindow.Data.ClearDownloadProgress();
            return;
        }

        LoggerService.LogInfo(downloadUrl);
        var tempPath = Path.Combine(AppContext.BaseDirectory, "temp");
        Directory.CreateDirectory(tempPath);

        var tempZipFilePath = Path.Combine(tempPath, $"resource_{latestVersion}.zip");
        dialog?.SetText("Downloading".GetLocalizationString());
        dialog?.UpdateProgress(0);

        if (!await DownloadFileAsync(downloadUrl, tempZipFilePath, dialog, "GameResourceUpdated"))
        {
            SetText("DownloadFailed", dialog, noDialog);
            Growls.Process(() => dialog?.Close());
            return;
        }

        dialog?.SetText("ApplyingUpdate".GetLocalizationString());
        dialog?.UpdateProgress(5);

        var tempExtractDir = Path.Combine(tempPath, $"resource_{latestVersion}_extracted");
        if (Directory.Exists(tempExtractDir)) Directory.Delete(tempExtractDir, true);
        if (!File.Exists(tempZipFilePath))
        {
            SetText("DownloadFailed", dialog, noDialog);
            Growls.Process(() => dialog?.Close());
            return;
        }

        ZipFile.ExtractToDirectory(tempZipFilePath, tempExtractDir);
        dialog?.UpdateProgress(50);

        var interfacePath = Path.Combine(tempExtractDir, "interface.json");
        var resourceDirPath = Path.Combine(tempExtractDir, "resource");
        if (!File.Exists(interfacePath))
        {
            interfacePath = Path.Combine(tempExtractDir, "assets", "interface.json");
            resourceDirPath = Path.Combine(tempExtractDir, "assets", "resource");
        }

        string wpfDir = AppContext.BaseDirectory;
        var file = new FileInfo(interfacePath);
        if (file.Exists)
        {
            var targetPath = Path.Combine(wpfDir, "interface.json");
            file.CopyTo(targetPath, true);
        }

        dialog?.UpdateProgress(60);

        var di = new DirectoryInfo(resourceDirPath);
        if (di.Exists)
        {
            CopyFolder(resourceDirPath, Path.Combine(wpfDir, "resource"));
        }

        dialog?.UpdateProgress(70);

        File.Delete(tempZipFilePath);
        Directory.Delete(tempExtractDir, true);
        dialog?.UpdateProgress(80);

        var newInterfacePath = Path.Combine(wpfDir, "interface.json");
        if (File.Exists(newInterfacePath))
        {
            var jsonContent = await File.ReadAllTextAsync(newInterfacePath);
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
                @interface.RID = GetResourceID();
                @interface.Version = latestVersion;
            }
            var updatedJsonContent = JsonConvert.SerializeObject(@interface, settings);

            await File.WriteAllTextAsync(newInterfacePath, updatedJsonContent);
        }

        dialog?.UpdateProgress(100);
        MainWindow.Instance.settingsView.ResourceShieldTextBlock.Text = latestVersion;
        dialog?.SetText("UpdateCompleted".GetLocalizationString());
        dialog.SetRestartButtonVisibility(true);

        MainWindow.Instance.SetUpdating(false);

        Growls.Process(() =>
        {
            if (closeDialog) dialog?.Close();
            if (noDialog)
            {
                if (MessageBoxHelper.Show("GameResourceUpdated".GetLocalizationString(), buttons: MessageBoxButton.YesNo, icon: MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    Process.Start(Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);
                    Growls.Process(Application.Current.Shutdown);
                }
            }
        });

        DataSet.SetData("TaskItems", new List<DragItemViewModel>());
        MainWindow.Instance.InitializeData();
        action?.Invoke();
    }

    public void CheckResourceVersionWithMirrorApi()
    {
        MainWindow.Instance.SetUpdating(true);

        var resId = GetResourceID();
        var currentVersion = GetResourceVersion();
        var cdk = SimpleEncryptionHelper.Decrypt(DataSet.GetData("DownloadCDK", string.Empty));
        var spId = GetDeviceId().ToString();
        var userAgent = "MFA";

        var apiUrl = $"https://mirrorc.top/api/resources/{resId}/latest?current_version={currentVersion}&cdk={cdk}&sp_id={spId}&user_agent={userAgent}";

        try
        {
            using var httpClient = new HttpClient();
            var response = httpClient.GetAsync(apiUrl).Result;
            response.EnsureSuccessStatusCode();

            var jsonResponse = response.Content.ReadAsStringAsync().Result;
            var responseData = JObject.Parse(jsonResponse);

            if ((int)responseData["code"] == 0)
            {
                var data = responseData["data"];
                var versionName = data["version_name"]?.ToString();

                if (!string.IsNullOrEmpty(versionName))
                {
                    var localVersion = GetResourceVersion();
                    if (IsNewVersionAvailable(versionName, localVersion))
                    {
                        Growl.Info("ResourceOption".GetLocalizationString() + "NewVersionAvailableLatestVersion".GetLocalizationString() + versionName);
                    }
                    else
                    {
                        Growl.Info("ResourcesAreLatestVersion".GetLocalizationString());
                    }
                }
                else
                {
                    Growls.Error("FailToGetLatestVersionInfo".GetLocalizationString());
                }
            }
            else
            {
                Growls.ErrorGlobal($"{"FailToGetLatestVersionInfo".GetLocalizationString()}: {responseData["msg"]}");
            }
        }
        catch (Exception ex)
        {
            MainWindow.Instance.SetUpdating(false);
            Growls.Error($"{"FailToGetLatestVersionInfo".GetLocalizationString()}: {ex.Message}");
            LoggerService.LogError(ex);
        }
        finally
        {
            MainWindow.Instance.SetUpdating(false);
        }
    }

    public async void UpdateResource(bool closeDialog = false, bool noDialog = false, Action? action = null)
    {
        MainWindow.Instance.SetUpdating(true);
        DownloadDialog? dialog = null;
        Growls.Process(() =>
        {
            if (!noDialog) dialog = new DownloadDialog("UpdateResource".GetLocalizationString());
            dialog?.Show();
        });

        var url = MaaInterface.Instance?.Url ?? string.Empty;
        if (string.IsNullOrEmpty(url))
        {
            MainWindow.Instance.SetUpdating(false);
            Growls.Process(() => dialog?.Close());
            return;
        }

        SetText("GettingLatestResources", dialog, noDialog);

        dialog?.UpdateProgress(10);
        var strings = GetRepoFromUrl(url);
        string latestVersion = string.Empty;
        try
        {
            latestVersion = strings.Length > 1 ? GetLatestVersionFromGithub(strings[0], strings[1]) : string.Empty;
        }
        catch (Exception ex)
        {
            SetText($"{"FailToGetLatestVersionInfo".GetLocalizationString()}: {ex.Message}", dialog, noDialog);
            MainWindow.Instance.SetUpdating(false);
            LoggerService.LogError(ex);
            return;
        }

        dialog?.UpdateProgress(50);

        if (string.IsNullOrWhiteSpace(latestVersion))
        {
            Growls.ErrorGlobal("FailToGetLatestVersionInfo".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            Growls.Process(() => dialog?.Close());
            MainWindow.Data.ClearDownloadProgress();
            return;
        }

        var localVersion = MaaInterface.Instance?.Version ?? string.Empty;

        if (string.IsNullOrWhiteSpace(localVersion))
        {
            Growls.ErrorGlobal("FailToGetCurrentVersionInfo".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            Growls.Process(() => dialog?.Close());
            MainWindow.Data.ClearDownloadProgress();
            return;
        }

        if (!IsNewVersionAvailable(latestVersion, localVersion))
        {
            Growl.InfoGlobal("ResourcesAreLatestVersion".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            Growls.Process(() => dialog?.Close());
            MainWindow.Data.ClearDownloadProgress();
            action?.Invoke();
            return;
        }

        string downloadUrl = string.Empty;
        try
        {
            downloadUrl = GetDownloadUrlFromGitHubRelease(latestVersion, strings[0], strings[1]);
        }
        catch (Exception ex)
        {
            SetText($"{"FailToGetDownloadUrl".GetLocalizationString()}: {ex.Message}", dialog, noDialog);
            MainWindow.Instance.SetUpdating(false);
            LoggerService.LogError(ex);
            return;
        }

        dialog?.UpdateProgress(100);

        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            Growls.ErrorGlobal("FailToGetDownloadUrl".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            MainWindow.Data.ClearDownloadProgress();
            return;
        }

        var tempPath = Path.Combine(AppContext.BaseDirectory, "temp");
        Directory.CreateDirectory(tempPath);

        var tempZipFilePath = Path.Combine(tempPath, $"resource_{latestVersion}.zip");
        dialog?.SetText("Downloading".GetLocalizationString());
        dialog?.UpdateProgress(0);

        if (!await DownloadFileAsync(downloadUrl, tempZipFilePath, dialog, "GameResourceUpdated"))
        {
            SetText("DownloadFailed", dialog, noDialog);
            Growls.Process(() => dialog?.Close());
            return;
        }

        dialog?.SetText("ApplyingUpdate".GetLocalizationString());
        dialog?.UpdateProgress(5);

        var tempExtractDir = Path.Combine(tempPath, $"resource_{latestVersion}_extracted");
        if (Directory.Exists(tempExtractDir)) Directory.Delete(tempExtractDir, true);
        if (!File.Exists(tempZipFilePath))
        {
            SetText("DownloadFailed", dialog, noDialog);
            Growls.Process(() => dialog?.Close());
            return;
        }

        ZipFile.ExtractToDirectory(tempZipFilePath, tempExtractDir);
        dialog?.UpdateProgress(50);

        var interfacePath = Path.Combine(tempExtractDir, "interface.json");
        var resourceDirPath = Path.Combine(tempExtractDir, "resource");

        string wpfDir = AppContext.BaseDirectory;
        var file = new FileInfo(interfacePath);
        if (file.Exists)
        {
            var targetPath = Path.Combine(wpfDir, "interface.json");
            file.CopyTo(targetPath, true);
        }

        dialog?.UpdateProgress(60);

        var di = new DirectoryInfo(resourceDirPath);
        if (di.Exists)
        {
            CopyFolder(resourceDirPath, Path.Combine(wpfDir, "resource"));
        }

        dialog?.UpdateProgress(70);

        File.Delete(tempZipFilePath);
        Directory.Delete(tempExtractDir, true);
        dialog?.UpdateProgress(80);

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

        dialog?.UpdateProgress(100);
        MainWindow.Instance.settingsView.ResourceShieldTextBlock.Text = latestVersion;
        dialog?.SetText("UpdateCompleted".GetLocalizationString());
        dialog?.SetRestartButtonVisibility(true);

        MainWindow.Instance.SetUpdating(false);

        Growls.Process(() =>
        {
            if (closeDialog) dialog?.Close();
            if (noDialog)
            {
                if (MessageBoxHelper.Show("GameResourceUpdated".GetLocalizationString(), buttons: MessageBoxButton.YesNo, icon: MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    Process.Start(Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);
                    Growls.Process(Application.Current.Shutdown);
                }
            }
        });

        DataSet.SetData("TaskItems", new List<DragItemViewModel>());
        MainWindow.Instance.settingsView.ResourceShieldTextBlock.Text = latestVersion;
        MainWindow.Instance.InitializeData();
        action?.Invoke();
    }

    public async void UpdateMFA(bool noDialog = false)
    {
        MainWindow.Instance.SetUpdating(true);

        DownloadDialog? dialog = null;
        Growls.Process(() =>
        {
            if (!noDialog) dialog = new DownloadDialog("SoftwareUpdate".GetLocalizationString());
            dialog?.Show();
        });

        SetText("GettingLatestSoftware", dialog, noDialog);

        var url = MFAUrls.GitHub;

        dialog?.UpdateProgress(10);

        var strings = GetRepoFromUrl(url);
        string latestVersion = string.Empty;
        try
        {
            latestVersion = GetLatestVersionFromGithub();
        }
        catch (Exception ex)
        {
            SetText($"{"FailToGetLatestVersionInfo".GetLocalizationString()}: {ex.Message}", dialog, noDialog);
            MainWindow.Instance.SetUpdating(false);
            LoggerService.LogError(ex);
            return;
        }

        dialog?.UpdateProgress(50);

        if (string.IsNullOrWhiteSpace(latestVersion))
        {
            Growls.ErrorGlobal("FailToGetLatestVersionInfo".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            Growls.Process(() => dialog?.Close());
            MainWindow.Data.ClearDownloadProgress();
            return;
        }

        var localVersion = GetLocalVersion();

        if (string.IsNullOrWhiteSpace(localVersion))
        {
            Growls.ErrorGlobal("FailToGetCurrentVersionInfo".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            Growls.Process(() => dialog?.Close());
            MainWindow.Data.ClearDownloadProgress();
            return;
        }

        if (!IsNewVersionAvailable(latestVersion, localVersion))
        {
            Growl.InfoGlobal("MFAIsLatestVersion".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            Growls.Process(() => dialog?.Close());
            MainWindow.Data.ClearDownloadProgress();
            return;
        }

        string downloadUrl = string.Empty;
        try
        {
            downloadUrl = GetDownloadUrlFromGitHubRelease(latestVersion, strings[0], strings[1]);
        }
        catch (Exception ex)
        {
            SetText($"{"FailToGetDownloadUrl".GetLocalizationString()}: {ex.Message}", dialog, noDialog);
            MainWindow.Instance.SetUpdating(false);
            Growls.Process(() => dialog?.Close());
            MainWindow.Data.ClearDownloadProgress();
            LoggerService.LogError(ex);
            return;
        }

        dialog?.UpdateProgress(100);

        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            Growls.ErrorGlobal("FailToGetDownloadUrl".GetLocalizationString());
            MainWindow.Instance.SetUpdating(false);
            Growls.Process(() => dialog?.Close());
            MainWindow.Data.ClearDownloadProgress();
            return;
        }

        var tempPath = Path.Combine(AppContext.BaseDirectory, "temp");
        Directory.CreateDirectory(tempPath);

        var tempZipFilePath = Path.Combine(tempPath, $"mfa_{latestVersion}.zip");
        dialog?.SetText("Downloading".GetLocalizationString());
        dialog?.UpdateProgress(0);

        if (!await DownloadFileAsync(downloadUrl, tempZipFilePath, dialog, "GameResourceUpdated"))
        {
            SetText("DownloadFailed", dialog, noDialog);
            Growls.Process(() => dialog?.Close());
            MainWindow.Data.ClearDownloadProgress();
            return;
        }

        var tempExtractDir = Path.Combine(tempPath, $"mfa_{latestVersion}_extracted");
        if (Directory.Exists(tempExtractDir)) Directory.Delete(tempExtractDir, true);
        if (!File.Exists(tempZipFilePath))
        {
            SetText("DownloadFailed", dialog, noDialog);
            Growls.Process(() => dialog?.Close());
            MainWindow.Data.ClearDownloadProgress();
            return;
        }

        ZipFile.ExtractToDirectory(tempZipFilePath, tempExtractDir);

        var currentExeFileName = Process.GetCurrentProcess().MainModule.ModuleName;

        var utf8Bytes = Encoding.UTF8.GetBytes(AppContext.BaseDirectory);
        var utf8BaseDirectory = Encoding.UTF8.GetString(utf8Bytes);
        var batFilePath = Path.Combine(utf8BaseDirectory, "temp", "update_mfa.bat");
        await using (var sw = new StreamWriter(batFilePath))
        {
            await sw.WriteLineAsync("@echo off");
            await sw.WriteLineAsync("chcp 65001");

            await sw.WriteLineAsync("ping 127.0.0.1 -n 3 > nul");
            var extractedPath = $"\"{utf8BaseDirectory}temp\\mfa_{latestVersion}_extracted\\*.*\"";
            var extracted = $"{utf8BaseDirectory}temp\\mfa_{latestVersion}_extracted\\";
            var targetPath = $"\"{utf8BaseDirectory}\"";
            await sw.WriteLineAsync($"copy /Y \"{extracted}{Assembly.GetEntryAssembly().GetName().Name}.exe\" \"{utf8BaseDirectory}{currentExeFileName}\"");
            await sw.WriteLineAsync("ping 127.0.0.1 -n 1 > nul");
            await sw.WriteLineAsync($"del \"{extracted}{Assembly.GetEntryAssembly().GetName().Name}.exe\"");
            await sw.WriteLineAsync("ping 127.0.0.1 -n 1 > nul");
            await sw.WriteLineAsync($"xcopy /E /Y {extractedPath} {targetPath}");
            await sw.WriteLineAsync("ping 127.0.0.1 -n 1 > nul");
            await sw.WriteLineAsync($"start /d \"{utf8BaseDirectory}\" {currentExeFileName}");
            await sw.WriteLineAsync($"rd /S /Q \"{utf8BaseDirectory}temp\"");
        }

        var psi = new ProcessStartInfo(batFilePath)
        {
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        Process.Start(psi);
        Thread.Sleep(50);
        Growls.Process(Application.Current.Shutdown);
    }

    private static void CopyFolder(string sourceFolder, string destinationFolder)
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

    private void GetDownloadUrlFromMirror(string version, string resId, string spId, string cdk, out string url, out string latest_version, string userAgent = "MFA")
    {
        var releaseUrl = $"https://mirrorc.top/api/resources/{resId}/latest?current_version={version}&cdk={cdk}&sp_id={spId}&user_agent={userAgent}";

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("request");
        httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
        try
        {
            var response = httpClient.GetAsync(releaseUrl).Result;
            response.EnsureSuccessStatusCode();

            var read = response.Content.ReadAsStringAsync();
            read.Wait();
            var jsonResponse = read.Result;
            var responseData = JObject.Parse(jsonResponse);

            if ((int)responseData["code"] == 0)
            {
                var data = responseData["data"];
                var versionName = data["version_name"]?.ToString();
                var downloadUrl = data["url"]?.ToString();
                url = downloadUrl;
                latest_version = versionName;
            }
            else
            {
                throw new Exception($"{"MirrorAutoUpdatePrompt".GetLocalizationString()}: {responseData["msg"]}");
            }
        }
        catch (Exception e)
        {
            throw new Exception($"{e.Message}");
        }
    }

    private string GetDownloadUrlFromGitHubRelease(string version, string owner, string repo)
    {
        var releaseUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/tags/{version}";
        using var httpClient = new HttpClient();
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
        return string.Empty;
    }

    private async Task<bool> DownloadFileAsync(string url, string filePath, DownloadDialog? dialog, string key)
    {
        try
        {
            using var client = new WebClient();
            client.Headers[HttpRequestHeader.UserAgent] = "request";
            client.Headers[HttpRequestHeader.Accept] = "application/json";

            var startTime = DateTime.Now;
            long totalBytesRead = 0;
            long bytesPerSecond = 0;
            long totalBytes = 0;

            client.DownloadProgressChanged += (_, e) =>
            {
                var currentTime = DateTime.Now;
                var timeSpan = currentTime - startTime;
                totalBytesRead = e.BytesReceived;
                totalBytes = e.TotalBytesToReceive;

                if (timeSpan.TotalSeconds >= 1)
                {
                    bytesPerSecond = totalBytesRead - (totalBytesRead - e.BytesReceived);
                    startTime = currentTime;
                }

                var progressPercentage = totalBytes > 0 ? (double)totalBytesRead / totalBytes * 100 : 0;
                dialog?.UpdateProgress(progressPercentage);
                Growls.Process(() =>
                    MainWindow.Data.OutputDownloadProgress(totalBytesRead, totalBytes, (int)bytesPerSecond, timeSpan.TotalSeconds));

                if (progressPercentage >= 100)
                {
                    Growls.Process(() => MainWindow.Data.OutputDownloadProgress(downloading: false, output: key.GetLocalizationString()));
                }
            };

            await client.DownloadFileTaskAsync(new Uri(url), filePath);
            return true;
        }
        catch (WebException webEx)
        {
            LoggerService.LogError($"HTTP请求出现异常: {webEx.Message}");
            return false;
        }
        catch (IOException ioEx)
        {
            LoggerService.LogError($"文件操作出现异常: {ioEx.Message}");
            return false;
        }
        catch (Exception ex)
        {
            LoggerService.LogError($"出现未知异常: {ex.Message}");
            return false;
        }
    }

    private bool IsPathWritable(string? path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        try
        {
            string testFilePath = Path.Combine(path, Path.GetRandomFileName());
            using (File.Create(testFilePath)) { }
            File.Delete(testFilePath);
            return true;
        }
        catch
        {
            return false;
        }
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

            return new[] { owner, repo };
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

    private string GetLocalVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "DEBUG";
    }

    private string GetResourceVersion()
    {
        return MaaInterface.Instance?.Version ?? "DEBUG";
    }


    private string GetResourceID()
    {
        return MaaInterface.Instance?.RID ?? MaaInterface.Instance?.Name ?? string.Empty;
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

    private static readonly Guid Namespace = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

    private static Guid? GetDeviceId()
    {
        var cpuSerial = GetCpuSerial();
        LoggerService.LogInfo($"CPU: {cpuSerial}");
        return GuidGenerator.Version5.NewGuid(Namespace, cpuSerial);
    }

    private static string GetCpuSerial()
    {
        var system = Environment.OSVersion.Platform.ToString();
        if (system.Contains("Win32"))
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT Growls.ProcessorId FROM Win32_Growls.Processor");
                foreach (ManagementObject mo in searcher.Get())
                {
                    return mo["Growls.ProcessorId"].ToString();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error getting CPU serial on Windows: {e}");
            }
        }
        else if (system.Contains("Unix"))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try
                {
                    Process Process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "sh",
                            Arguments = "-c \"cat /proc/cpuinfo | grep serial | cut -d ' ' -f 2\"",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    Process.Start();
                    var result = Process.StandardOutput.ReadToEnd().Trim();
                    Process.WaitForExit();
                    return result;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error getting CPU serial on Linux: {e}");
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    Process Process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "sh",
                            Arguments = "-c \"ioreg -l | grep IOPlatformSerialNumber\"",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    Process.Start();
                    var result = Process.StandardOutput.ReadToEnd().Trim();
                    Process.WaitForExit();
                    return result.Split('"')[3];
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error getting CPU serial on macOS: {e}");
                }
            }
        }
        return "UNKNOWN";
    }
}
