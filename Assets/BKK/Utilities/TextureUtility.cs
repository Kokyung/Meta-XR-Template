using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public static class TextureUtility
{
    private static List<ExternalTextureData> textures = new List<ExternalTextureData>();

    /// <param name="colorSpace">이미지 데이터의 색 공간. 일반 이미지(sRGB)는 Gamma, 노멀맵 등 데이터 텍스처는 Linear</param>
    public async static Task<Texture2D> LoadTexture2DAsync(string path, bool checkDuplicate = true,
        bool useMipmap = true, ColorSpace colorSpace = ColorSpace.Gamma)
    {
        if (string.IsNullOrEmpty(path)) return null;

        if (!File.Exists(path))
        {
            Debug.LogError($"경로에 파일이 존재하지 않습니다. {path}");
            return null;
        }

        if (checkDuplicate)
        {
            var index = textures.FindIndex(d => d.path == path);

            if (index >= 0)
            {
                if (textures[index].texture) return textures[index].texture as Texture2D;

                // 이미 파괴된 텍스처 항목은 제거하고 새로 로드한다.
                textures.RemoveAt(index);
            }
        }

        byte[] data = await File.ReadAllBytesAsync(path);

        Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, useMipmap, colorSpace == ColorSpace.Linear);

        bool loaded = texture.LoadImage(data);

        // await Task.Yield();
        // await UniTask.NextFrame();
        await Awaitable.NextFrameAsync();

        if (loaded)
        {
            textures.Add(new ExternalTextureData(texture, path));
        }

        return texture;
    }

    public static void UnLoadTexture2D(string path)
    {
        if (textures.Exists(d => d.path == path))
        {
            var index = textures.FindIndex(d => d.path == path);

            textures[index].Destroy();
            textures.RemoveAt(index);
        }
    }

    public static void UnLoadTexture2D(Texture texture)
    {
        if (textures.Exists(d => d.texture == texture))
        {
            var index = textures.FindIndex(d => d.texture == texture);

            textures[index].Destroy();
            textures.RemoveAt(index);
        }
        else
        {
            ExternalTextureData.DestroyTexture(texture);
        }
    }

    public static void UnLoadTexture2DAll()
    {
        for (int i = 0; i < textures.Count; i++)
        {
            textures[i].Destroy();
        }

        textures.Clear();
    }
}

public class ExternalTextureData
{
    public Texture texture;
    public string path;

    public ExternalTextureData(Texture texture, string path)
    {
        this.texture = texture;
        this.path = path;
    }

    public void Destroy()
    {
        path = null;
        DestroyTexture(texture);
    }

    public static void DestroyTexture(Texture texture)
    {
        if (!texture) return;

        // 런타임에는 Destroy, 에디트 모드에서는 Destroy가 동작하지 않으므로 DestroyImmediate를 사용한다.
        if (Application.isPlaying) Object.Destroy(texture);
        else Object.DestroyImmediate(texture);
    }
}
