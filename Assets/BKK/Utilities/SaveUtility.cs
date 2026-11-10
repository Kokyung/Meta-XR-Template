using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public static class SaveUtility
{
    public static Task Save<T>(T data, string path = "")
    {
        if (string.IsNullOrEmpty(path))
        {
            // 저장하지 못했으므로 await한 호출자가 실패를 알 수 있도록 실패한 Task를 반환한다.
            Debug.LogError("경로가 없습니다.");
            return Task.FromException(new ArgumentException("경로가 없습니다.", nameof(path)));
        }

        string json = JsonConvert.SerializeObject(data, Formatting.Indented, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });

        string folderPath = Path.GetDirectoryName(path);

        CreateDirectoryNotExist(folderPath);

        return File.WriteAllTextAsync(path, json);
    }

    public static async Task<T> Load<T>(string path = "")
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("경로가 없습니다.");
            return default;
        }

        if (!FileExists(path))
        {
            return default;
        }

        string json = await File.ReadAllTextAsync(path);

        return JsonConvert.DeserializeObject<T>(json);
    }

    public static bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public static bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public static void CreateDirectoryNotExist(string path)
    {
        // 파일명만 넘어오면 폴더 경로가 빈 문자열이 되므로 현재 폴더로 보고 생성하지 않는다.
        if (string.IsNullOrEmpty(path)) return;

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    public static string GetLastFolderName(string path)
    {
        return Path.GetFileName(path);
    }
}
