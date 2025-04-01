using System.IO;
using System.IO.Compression;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class AutoUpdater : MonoBehaviour
{
    private string remoteVersionUrl = "https://raw.githubusercontent.com/SomeRandomFella/VersionReleaser/main/version.txt";
    private string releasesApiUrl = "https://api.github.com/repos/SomeRandomFella/VersionReleaser/releases/latest";
    private string localVersionFile;
    private string gamePath;
    private string zipPath;

    void Start()
    {
        localVersionFile = Path.Combine(Application.persistentDataPath, "version.txt");
        gamePath = Path.Combine(Application.persistentDataPath, "Game");
        zipPath = Path.Combine(Application.persistentDataPath, "temp_download.zip");
        StartCoroutine(CheckForUpdates());
    }

    IEnumerator CheckForUpdates()
    {
        UnityWebRequest www = UnityWebRequest.Get(remoteVersionUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
            yield break;

        string remoteVersion = www.downloadHandler.text.Trim();
        string localVersion = File.Exists(localVersionFile) ? File.ReadAllText(localVersionFile).Trim() : "0.0.0";

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

        if (Directory.Exists(gamePath))
            Directory.Delete(gamePath, true);

        ZipFile.ExtractToDirectory(zipPath, gamePath);
        File.Delete(zipPath);
        File.WriteAllText(localVersionFile, newVersion);
        Application.OpenURL("file://" + gamePath);
    }
}
