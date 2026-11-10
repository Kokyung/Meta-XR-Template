using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public static class FileUtility
{
    public static string projectPath = Path.GetDirectoryName(Application.dataPath);

    public static string GetDirectoryName(string path)
    {
        return Path.GetDirectoryName(path);
    }
    
    public static void CopyFilesRecursively(string sourcePath, string targetPath, bool overwrite = false)
    {
        if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);

        string[] files = Directory.GetFiles(sourcePath);
        string[] folders = Directory.GetDirectories(sourcePath);

        foreach (var file in files)
        {
            string name = Path.GetFileName(file);
            string dest = Path.Combine(targetPath, name);
            File.Copy(file, dest, overwrite);
        }

        foreach (var folder in folders)
        {
            string name = Path.GetFileName(folder);
            string dest = Path.Combine(targetPath, name);
            CopyFilesRecursively(folder, dest, overwrite);
        }
    }
    
    public static string CopyFile(string sourceFilePath, string targetPath, bool overwrite = false)
    {
        if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);

        string fileName = Path.GetFileName(sourceFilePath);
        string fileDest = Path.Combine(targetPath, fileName);
        
        File.Copy(sourceFilePath, fileDest, overwrite);

        return fileDest;
    }
    
    public static async Task<string> CopyFileAsync(string sourceFilePath, string targetPath)
    {
        using (Stream source = File.OpenRead(sourceFilePath))
        {
            string directory = Path.GetDirectoryName(targetPath);
            
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            using (Stream dest = File.Create(targetPath))
            {
                await source.CopyToAsync(dest);
                
                return targetPath;
            }
        }
    }
    
    public static async Task<string> CopyFileIntoFolderAsync(string sourceFilePath, string targetFolderPath)
    {
        using (Stream source = File.OpenRead(sourceFilePath))
        {
            if (!Directory.Exists(targetFolderPath)) Directory.CreateDirectory(targetFolderPath);
            
            string fileName = Path.GetFileName(sourceFilePath);
            string fileDest = Path.Combine(targetFolderPath, fileName);
            
            using (Stream dest = File.Create(fileDest))
            {
                await source.CopyToAsync(dest);
                
                return fileDest;
            }
        }
    }
    
    public static async Task<string> CopyFileIntoFolderAsyncWithHashCode(string sourceFilePath, string targetFolderPath)
    {
        using (Stream source = File.OpenRead(sourceFilePath))
        {
            if (!Directory.Exists(targetFolderPath)) Directory.CreateDirectory(targetFolderPath);
            
            string fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
            string extension = Path.GetExtension(sourceFilePath);
            string hashcode = (fileName + System.DateTime.Now).GetBlackBombHashCode();
            
            string fileDest = Path.Combine(targetFolderPath, fileName + "-" + hashcode + extension);
            
            using (Stream dest = File.Create(fileDest))
            {
                await source.CopyToAsync(dest);
                
                return fileDest;
            }
        }
    }

    public static void DeleteFileIfInDirectory(string filePath, string checkContainsPath)
    {
        string target = AppendDirectorySeparator(NormalizePath(Path.GetDirectoryName(filePath)));
        string check = AppendDirectorySeparator(NormalizePath(checkContainsPath));

        // "C:/Data2"가 "C:/Data" 안으로 판정되지 않도록 구분자를 붙여 앞부분을 비교한다.
        // 대소문자만 다른 경로는 삭제하지 않는 쪽으로 판정한다.
        if (!target.StartsWith(check, StringComparison.Ordinal)) return;
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public static string NormalizePath(string path)
    {
        return Path.GetFullPath(Path.Combine(path));
    }

    private static string AppendDirectorySeparator(string path)
    {
        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
    }

    public static string GetFileNameWithoutExtension(string path)
    {
        return Path.GetFileNameWithoutExtension(path);
    }

    public static string GetBlackBombHashCode(this string input)
    {
        int hashCode = input.GetHashCode(StringComparison.InvariantCultureIgnoreCase);

        // int로 계산하면 Abs(int.MinValue)와 * 11에서 오버플로가 나므로 long으로 계산한다.
        long value = Math.Abs((long)hashCode) * 11;

        // 6자리 미만이면 앞을 0으로 채운다.
        return value.ToString().PadLeft(6, '0').Substring(0, 6);
    }
}
