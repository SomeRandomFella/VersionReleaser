using System.IO;
using System.IO.Compression;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Diagnostics;
using System;

public class AutoUpdater : MonoBehaviour
{
    private string remoteVersionUrl = "https://raw.githubusercontent.com/SomeRandomFella/VersionReleaser/main/version.txt";
    private string releasesApiUrl = "https://api.github.com/repos/SomeRandomFella/VersionReleaser/releases/latest";
    private string localVersionFile;
    private string baseGamePath;
    private string zipPath;
    private string currentVersionFolder;

    void Start()
    {
        localVersionFile = Path.Combine(Application.persistentDataPath, "version.txt");
        baseGamePath = Path.Combine(Application.persistentDataPath, "Game");
        zipPath = Path.Combine(Application.persistentDataPath, "temp_download.zip");

        string localVersion = File.Exists(localVersionFile) ? File.ReadAllText(localVersionFile).Trim() : "0.0.0";
        currentVersionFolder = Path.Combine(baseGamePath, "Version_" + localVersion);

        if (!Directory.Exists(currentVersionFolder))
        {
            ShowErrorAndExit();
            return;
        }

        StartCoroutine(CheckForUpdates(localVersion));
    }

    IEnumerator CheckForUpdates(string localVersion)
    {
        UnityWebRequest www = UnityWebRequest.Get(remoteVersionUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
            yield break;

        string remoteVersion = www.downloadHandler.text.Trim();

        if (remoteVersion != localVersion)
            StartCoroutine(FetchLatestRelease(remoteVersion));
    }

    IEnumerator FetchLatestRelease(string remoteVersion)
    {
        UnityWebRequest www = UnityWebRequest.Get(releasesApiUrl);
        www.SetRequestHeader("User-Agent", "UnityAutoUpdater");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
            yield break;

        var json = www.downloadHandler.text;
        var assetUrl = ExtractZipUrlFromJson(json);

        if (!string.IsNullOrEmpty(assetUrl))
            StartCoroutine(DownloadAndUpdate(assetUrl, remoteVersion));
    }

    string ExtractZipUrlFromJson(string json)
    {
        int i = 0;
        while ((i = json.IndexOf("\"browser_download_url\"", i)) != -1)
        {
            int start = json.IndexOf("https://", i);
            int end = json.IndexOf("\"", start);
            string url = json.Substring(start, end - start);
            if (url.EndsWith(".zip") && !url.Contains("Source"))
                return url;
            i = end;
        }
        return null;
    }

    IEnumerator DownloadAndUpdate(string downloadUrl, string newVersion)
    {
        UnityWebRequest www = UnityWebRequest.Get(downloadUrl);
        www.downloadHandler = new DownloadHandlerFile(zipPath);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
            yield break;

        string newVersionPath = Path.Combine(baseGamePath, "Version_" + newVersion);

        if (!Directory.Exists(newVersionPath))
            Directory.CreateDirectory(newVersionPath);

        ZipFile.ExtractToDirectory(zipPath, newVersionPath);
        File.Delete(zipPath);
        File.WriteAllText(localVersionFile, newVersion);
        
        // Launch the new version
        string executablePath = Path.Combine(newVersionPath, "YourGameExecutable.exe");
        if (File.Exists(executablePath))
        {
            Process.Start(executablePath);
            Application.Quit(); // Close the current game
        }
    }

    void ShowErrorAndExit()
    {
        string message = "Error: This is an outdated version. Play the new version auto-updated in your game folder!";
        
        #if UNITY_STANDALONE_WIN
        System.Windows.Forms.MessageBox.Show(message, "Outdated Version", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
        #endif

        Debug.LogError(message);
        Application.Quit();
    }
}