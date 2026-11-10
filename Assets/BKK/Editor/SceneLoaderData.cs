using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
// using UnityEngine.SceneManagement;

namespace BKK.EditorSceneManagement
{
    public class SceneLoaderData : ScriptableObject
    {
        public string relativeFolderPath = "Assets/";

        public List<SceneData> sceneDataList = new List<SceneData>();

        // public LoadSceneMode loadSceneMode = LoadSceneMode.Single;

        // public bool autoLoadSceneOnPlay = false;

        //  int targetSceneEnumIndex = 0;

        /// <summary>
        /// Scene Loader Data를 생성한다.
        /// </summary>
        /// <returns></returns>
        public static SceneLoaderData CreateData()
        {
            ScriptableObject instance = CreateInstance(typeof(SceneLoaderData));

            string absoluteDataPath = Application.dataPath + SceneLoaderString.defaultDataFolderPath;
            
            if (!Directory.Exists(absoluteDataPath))
            {
                Directory.CreateDirectory(absoluteDataPath);
            }

            AssetDatabase.CreateAsset(instance, SceneLoaderString.defaultDataFilePath);
            
            return Resources.Load<SceneLoaderData>(SceneLoaderString.defaultResourcesFilePath);
        }

        /// <summary>
        /// Scene Loader Data를 가져온다.
        /// 없을 경우 CreateData()로 생성해서 가져온다.
        /// </summary>
        /// <returns></returns>
        public static SceneLoaderData GetData()
        {
            SceneLoaderData data = Resources.Load<SceneLoaderData>(SceneLoaderString.defaultResourcesFilePath);

            return data ? data : CreateData();
        }

        /// <summary>
        /// 플레이시 자동 로드할 Scene의 경로를 가져온다. 
        /// </summary>
        /// <returns></returns>
        public string GetAutoLoadTargetScenePath()
        {
            if (sceneDataList.Count == 0) return string.Empty;

            int index = EditorPrefs.GetInt(Application.productName + "_" + SceneLoaderString.editorPrefs_TargetSceneEnumIndex, 0);
            
            return sceneDataList[index].scenePath;
        }

        /// <summary>
        /// 지정한 폴더 내에 모든 Scene 에셋을 각각 SceneData로 저장하여 sceneDataList에 저장한다.
        /// </summary>
        public void GetScenePathInFolder()
        {
            string[] scenePaths = Directory.GetFiles(Application.dataPath + relativeFolderPath.Substring(6),"*.unity");

            // Directory.GetFiles가 파일 이름 앞에 백슬래시를 붙여서 리턴하기 때문에 백슬래시를 슬래시로 변환한다.
            for (int i = 0; i < scenePaths.Length; i++)
            {
                scenePaths[i] = scenePaths[i].Replace(@"\", "/");
            }
            
            sceneDataList.Clear();
            
            for (int i = 0; i < scenePaths.Length; i++)
            {
                string sceneName = scenePaths[i].Substring(scenePaths[i].LastIndexOf('/') + 1);
                string scenePath = scenePaths[i].Substring(scenePaths[i].IndexOf("Assets", StringComparison.Ordinal));
                sceneDataList.Add(new SceneData(sceneName, scenePath));
            }

            EditorUtility.SetDirty(this);// 저장이 안되는 문제가 있어서 Dirty 체크
        }
    }

    [System.Serializable]
    public class SceneData
    {
        public string sceneName = "";

        public string scenePath = "";

        public SceneData(string name, string path)
        {
            sceneName = name;
            scenePath = path;
        }
    }

    public static class SceneLoaderString
    {
        public const string defaultDataFilePath = "Assets/Resources/SceneLoader/SceneLoaderData.asset";
        public const string defaultDataFolderPath = "/Resources/SceneLoader/";
        public const string defaultResourcesFilePath = "SceneLoader/SceneLoaderData";

        public const string editorPrefs_LoadSceneMode = "SL_LSM";
        public const string editorPrefs_OpenSceneMode = "SL_OSM";
        public const string editorPrefs_AutoLoadSceneOnPlay = "SL_ALSOP";
        public const string editorPrefs_SetMainAdditiveLoad = "SL_SMAL";
        public const string editorPrefs_MergeLoadScene = "SL_MLS";
        public const string editorPrefs_TargetSceneEnumIndex = "SL_TSEI";
    }
}