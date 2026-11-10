using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BKK.EditorSceneManagement
{
    public class SceneLoaderWindow : EditorWindow
    {
        private SceneLoaderData sceneLoaderData;

        private static readonly string windowTitle = "Scene Loader";
        private readonly string pathLabel = "Path: ";
        private readonly string chooseSceneFolderLabel = "Choose Scene Folder";
        private readonly string addSceneOnBuildLabel = "Add Current Scene List On Build";
        private readonly string autoLoadOnPlayLabel = "Auto Load On Play";
        private readonly string setMainAdditiveLoadLabel = "Set Additive scene as Main";
        private readonly string mergeLoadSceneLabel = "Merge On Load";
        private readonly string targetSceneLabel = "Target Scene";
        // private readonly string loadSceneModeLabel = "Load Scene Mode";
        // private readonly string openSceneModeLabel = "Open Scene Mode";
        // private readonly string noSceneInPathLabel = "No Scene in Path.";

        private readonly string folderSceneListLabel = "Folder Scene List";
        private readonly string loadedSceneListLabel = "Loaded Scene List";

        private readonly string loadButtonLabel = "Load";
        private readonly string addButtonLabel = "Add";
        private readonly string mergeButtonLabel = "Merge";
        private readonly string unloadButtonLabel = "Unload";

        private readonly string chooseFolderWarningLabel = "Please Choose Scene Folder";
        private readonly string scenesEmptyWarningLabel = "Scene Asset Not Found";

        private GUIStyle warningTextStyle;

        private bool sceneIsLoading = false;
        
        private Vector2 folderSceneListScrollPos = Vector2.zero;
        private Vector2 loadedSceneListScrollPos = Vector2.zero;

        private bool setMainAdditiveLoad = false;
        
        /// <summary>
        /// Scene Loader Window 메뉴 클릭시 윈도우를 연다.
        /// </summary>
        [MenuItem("BKK/Editor Scene Management/Scene Loader")]
        private static void Init()
        {
            SceneLoaderWindow window = GetWindow<SceneLoaderWindow>();
            window.titleContent = new GUIContent(windowTitle);
        }

        /// <summary>
        /// InitializeOnLoadMethod에 의해 유니티 에디터 로드 중에 호출된다.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void InitOnLoad()
        {
            // EditorApplication.playModeStateChanged += LoadDefaultScene;
            
            EditorSceneManager.sceneLoaded += (scene, mode) =>
            {
                DynamicGI.UpdateEnvironment();// 동적 라이트맵 갱신이 자동으로 안되서 호출.
            };
        }

        /// <summary>
        /// Edit 
        /// </summary>
        /// <param name="state"></param>
        private static void LoadDefaultScene(PlayModeStateChange state)
        {
            // Edit 모드 나갈때 현재 수정 상태를 저장한다.
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            }

            // 플레이 모드에 진입할때 autoLoadSceneOnPlay가 True이면 목표 Scene을 로드한다. 
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                SceneLoaderData sceneLoaderData = SceneLoaderData.GetData();

                bool autoLoadSceneOnPlay = EditorPrefs.GetBool(Application.productName + "_" + SceneLoaderString.editorPrefs_AutoLoadSceneOnPlay, false);
                
                if(!autoLoadSceneOnPlay) return;

                int loadSceneMode = EditorPrefs.GetInt(Application.productName + "_" + SceneLoaderString.editorPrefs_LoadSceneMode, 0);

                LoadSceneParameters loadSceneParameters = new LoadSceneParameters((LoadSceneMode) loadSceneMode);
                
                EditorSceneManager.LoadSceneAsyncInPlayMode(sceneLoaderData.GetAutoLoadTargetScenePath(), loadSceneParameters);
            }
        }

        private void OnEnable()
        {
            // Scene Loader Data의 존재 여부 확인. 없으면 생성.
            sceneLoaderData = Resources.Load<SceneLoaderData>(SceneLoaderString.defaultResourcesFilePath);

            if (sceneLoaderData == null)
            {
                sceneLoaderData = SceneLoaderData.CreateData();
            }
            
            sceneLoaderData.GetScenePathInFolder();// 폴더 내 Scene 에셋들의 경로를 가져온다.
            
            // 폴더에 Scene 에셋이 없을때 출력할 안내문 스타일.
            if (warningTextStyle == null)
                warningTextStyle = new GUIStyle
                {
                    normal =
                    {
                        textColor = Color.yellow,
                    },
                };
        }

        private void OnGUI()
        {
            Draw();
        }

        private void Draw()
        {
            if(GUILayout.Button(chooseSceneFolderLabel))
            {
                string newPath = GetFolderPath();

                if (!string.IsNullOrEmpty(newPath))
                    sceneLoaderData.relativeFolderPath = newPath;
            }
            
            GUILayout.Label(pathLabel + sceneLoaderData.relativeFolderPath);

            if (GUILayout.Button(addSceneOnBuildLabel))
            {
                List<EditorBuildSettingsScene> editorBuildSettingsScenes = new List<EditorBuildSettingsScene>();

                editorBuildSettingsScenes.AddRange(EditorBuildSettings.scenes);

                foreach (var sceneData in sceneLoaderData.sceneDataList)
                {
                    if (!string.IsNullOrEmpty(sceneData.scenePath))
                    {
                        if (EditorBuildSettings.scenes.Any(s => s.path == sceneData.scenePath)) continue;
                        
                        editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(sceneData.scenePath, true));
                    }
                }

                EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
            }
            
            sceneLoaderData.GetScenePathInFolder();
            
            // if (sceneLoaderData.sceneDataList.Count == 0)
            // {
            //     EditorGUILayout.LabelField(noSceneInPathLabel, warningTextStyle);
            //     return;
            // }

            DrawFolderSceneList();

            DrawLoadedSceneList();
            
            DrawLine();
            DrawPlayModeMenu();
        }
        
        private void DrawFolderSceneList()
        {
            DrawTitle(folderSceneListLabel);
            
            if (string.IsNullOrEmpty(sceneLoaderData.relativeFolderPath))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(chooseFolderWarningLabel, warningTextStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                return;
            }

            if (sceneLoaderData.sceneDataList.Count == 0)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(scenesEmptyWarningLabel, warningTextStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                return;
            }
            
            if (sceneIsLoading) GUI.enabled = false;

            int openSceneMode = EditorPrefs.GetInt(Application.productName + "_" + SceneLoaderString.editorPrefs_OpenSceneMode, 0);
            bool mergeLoadScene = EditorPrefs.GetBool(Application.productName + "_" + SceneLoaderString.editorPrefs_MergeLoadScene, false);
            OpenSceneMode openSceneModeEnum = (OpenSceneMode)openSceneMode;

            folderSceneListScrollPos = EditorGUILayout.BeginScrollView(folderSceneListScrollPos);
            
            for (int i = 0; i < sceneLoaderData.sceneDataList.Count; i++)
            {
                string sceneName = sceneLoaderData.sceneDataList[i].sceneName.Replace(".unity", "");
                string scenePath = sceneLoaderData.sceneDataList[i].scenePath;
                
                DrawLoadSceneItem(sceneName, scenePath);
            }
            
            EditorGUILayout.EndScrollView();
            
            if (openSceneModeEnum is OpenSceneMode.Additive or OpenSceneMode.AdditiveWithoutLoading)
            {
                bool mergeLoadSceneToggle = EditorGUILayout.Toggle(this.mergeLoadSceneLabel, mergeLoadScene);
                EditorPrefs.SetBool(Application.productName + "_" + SceneLoaderString.editorPrefs_MergeLoadScene, mergeLoadSceneToggle);
            }

            GUI.enabled = true;
        }

        private void DrawLoadedSceneList()
        {
            DrawTitle(loadedSceneListLabel);

            loadedSceneListScrollPos = EditorGUILayout.BeginScrollView(loadedSceneListScrollPos);
            
            for (int i = 0; i < EditorSceneManager.loadedRootSceneCount; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                Scene scene = EditorSceneManager.GetSceneAt(i);
                
                GUILayout.Label(scene.name);
                
                GUILayout.FlexibleSpace();

                string subSceneText = scene == EditorSceneManager.GetActiveScene() ? "Main" : "Sub";
                
                GUILayout.Label($"{subSceneText}");

                if (GUILayout.Button(unloadButtonLabel))
                {
                    EditorSceneManager.UnloadSceneAsync(scene, UnloadSceneOptions.None);
                    DynamicGI.UpdateEnvironment();// 동적 라이트맵 갱신이 자동으로 안되서 호출.
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }

        private async void DrawLoadSceneButton(string sceneName, string scenePath, LoadSceneMode loadSceneMode, OpenSceneMode openSceneMode, bool mergeLoadScene)
        {
            if (GUILayout.Button(sceneName))
            {
                Scene scene;

                sceneIsLoading = true;

                try
                {
                    if (Application.isPlaying)
                    {
                        LoadSceneParameters loadSceneParameters =
                            new LoadSceneParameters(loadSceneMode, LocalPhysicsMode.Physics3D);
                        await EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, loadSceneParameters);
                        scene = EditorSceneManager.GetSceneByPath(scenePath);
                    }
                    else
                    {
                        scene = EditorSceneManager.OpenScene(scenePath, openSceneMode);
                    }
                }
                catch
                {
                    sceneIsLoading = false;
                    return;
                }

                await UniTask.WaitUntil(() => scene.isLoaded);

                if (mergeLoadScene && openSceneMode is OpenSceneMode.Additive or OpenSceneMode.AdditiveWithoutLoading)
                {
                    SceneManager.MergeScenes(scene, SceneManager.GetSceneAt(0));
                }

                sceneIsLoading = false;
            }
        }
        
        private void DrawLoadSceneItem(string sceneName, string scenePath)
        {
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label(sceneName);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button(loadButtonLabel))
            {
                if (!Application.isPlaying) EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                
                LoadScene(scenePath, LoadSceneMode.Single, OpenSceneMode.Single, false, false);
            }
            
            if (GUILayout.Button(addButtonLabel))
            {
                LoadScene(scenePath, LoadSceneMode.Additive, OpenSceneMode.Additive, false, setMainAdditiveLoad);
            }

            if (GUILayout.Button(mergeButtonLabel))
            {
                LoadScene(scenePath, LoadSceneMode.Additive, OpenSceneMode.Additive, true, false);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private async void LoadScene(string scenePath, LoadSceneMode loadSceneMode, OpenSceneMode openSceneMode, bool mergeLoadScene, bool setMain)
        {
            Scene scene;

            sceneIsLoading = true;

            try
            {
                if (Application.isPlaying)
                {
                    LoadSceneParameters loadSceneParameters =
                        new LoadSceneParameters(loadSceneMode, LocalPhysicsMode.Physics3D);
                    await EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, loadSceneParameters);
                    scene = EditorSceneManager.GetSceneByPath(scenePath);
                }
                else
                {
                    scene = EditorSceneManager.OpenScene(scenePath, openSceneMode);
                }
            }
            catch
            {
                sceneIsLoading = false;
                return;
            }
                
            await UniTask.WaitUntil(() => scene.isLoaded);

            if (mergeLoadScene && openSceneMode is OpenSceneMode.Additive or OpenSceneMode.AdditiveWithoutLoading)
            {
                SceneManager.MergeScenes(SceneManager.GetSceneAt(0), scene);
                // SceneManager.MergeScenes(scene, SceneManager.GetSceneAt(0));
            }

            if (setMain)
            {
                var activated = SceneManager.SetActiveScene(scene);
                await UniTask.WaitUntil(() => activated);
                
            }

            sceneIsLoading = false;
            
            DynamicGI.UpdateEnvironment();// 동적 라이트맵 갱신이 자동으로 안되서 호출.
        }

        private void DrawPlayModeMenu()
        {
            bool autoLoadSceneOnPlay = EditorPrefs.GetBool(Application.productName + "_" + SceneLoaderString.editorPrefs_AutoLoadSceneOnPlay, false);
            
            bool autoLoadToggle = EditorGUILayout.Toggle(autoLoadOnPlayLabel, autoLoadSceneOnPlay);

            EditorPrefs.SetBool(Application.productName + "_" + SceneLoaderString.editorPrefs_AutoLoadSceneOnPlay, autoLoadToggle);

            if (autoLoadToggle)
            {
                string[] optionList = new string[sceneLoaderData.sceneDataList.Count];

                for (int i = 0; i < sceneLoaderData.sceneDataList.Count; i++)
                {
                    optionList.SetValue(sceneLoaderData.sceneDataList[i].sceneName, i);
                }

                int targetSceneEnumIndex = EditorPrefs.GetInt(Application.productName + "_" + SceneLoaderString.editorPrefs_TargetSceneEnumIndex, 0);

                int targetScenePopup = EditorGUILayout.Popup(targetSceneLabel, targetSceneEnumIndex, optionList);
                
                EditorPrefs.SetInt(Application.productName + "_" + SceneLoaderString.editorPrefs_TargetSceneEnumIndex, targetScenePopup);

                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneLoaderData.GetAutoLoadTargetScenePath());
            }
            else
            {
                EditorSceneManager.playModeStartScene = null;
            }
            
            setMainAdditiveLoad = EditorPrefs.GetBool(Application.productName + "_" + SceneLoaderString.editorPrefs_SetMainAdditiveLoad, false);
            
            setMainAdditiveLoad = EditorGUILayout.Toggle(setMainAdditiveLoadLabel, setMainAdditiveLoad);
            
            EditorPrefs.SetBool(Application.productName + "_" + SceneLoaderString.editorPrefs_SetMainAdditiveLoad, setMainAdditiveLoad);
        }

        private void DrawLine()
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        private void DrawTitle(string title)
        {
            DrawLine();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(title);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            DrawLine();
        }

        /// <summary>
        /// 폴더 경로 지정 패널을 열고 지정한 경로를 리턴한다.
        /// </summary>
        /// <returns></returns>
        private string GetFolderPath()
        {
            string absolutePath = EditorUtility.OpenFolderPanel(chooseSceneFolderLabel, Application.dataPath, "");

            if (string.IsNullOrEmpty(absolutePath)) return string.Empty;
            
            return absolutePath.Substring(absolutePath.IndexOf("Assets", StringComparison.Ordinal));
        }
    }
}