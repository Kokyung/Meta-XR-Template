using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BKK.Graphics
{
    public sealed class SimpleMeshOutlineShaderGUI : ShaderGUI
    {
        private static readonly string[] ModeNames = { "Inverted Hull", "Fresnel", "Screen Space Outline" };

        // 마지막으로 Renderer Feature에 반영한 재질 설정. 같은 설정으로 프로젝트를 다시 훑지 않게 한다.
        private static int lastSyncedSignature;

        // 설정이 바뀌었지만 아직 반영하지 않았는지. 슬라이더를 끄는 동안에는 놓을 때까지 미룬다.
        private static bool syncPending;

        // 이 셰이더를 쓰는 재질 경로. 전체 재질을 훑는 비용이 커서 프로젝트가 바뀔 때만 다시 모은다.
        private static List<string> outlineMaterialPaths;

        private struct Usage
        {
            public bool screenSpace;
            public bool fill;
            public bool hullStencil;
            public int width;
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            EditorGUI.BeginChangeCheck();

            MaterialProperty mode = FindProperty("_OutlineMode", properties);
            EditorGUI.showMixedValue = mode.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            int selectedMode = EditorGUILayout.Popup(mode.displayName, Mathf.RoundToInt(mode.floatValue), ModeNames);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo("아웃라인 모드 변경");
                mode.floatValue = selectedMode;
            }
            EditorGUI.showMixedValue = false;
            DrawProperty(materialEditor, properties, "_OutlineColor");

            // 서로 다른 모드의 재질을 함께 선택하면 양쪽 설정을 모두 표시한다.
            if (mode.hasMixedValue || Mathf.RoundToInt(mode.floatValue) == 0)
            {
                DrawProperty(materialEditor, properties, "_OutlineWidth");
                // 각진 메시에서 모서리가 벌어지면 켠다.
                DrawProperty(materialEditor, properties, "_HullHardEdgeFix");
                // 본체가 보이는 영역의 내부 윤곽선을 없앤다. 렌더러 기능이 있어야 동작한다.
                DrawProperty(materialEditor, properties, "_HullStencilUnionMask");
                // 원본 위에 따로 겹쳐 놓은 복제 메시에 적용할 때만 켠다.
                DrawProperty(materialEditor, properties, "_HullOverlappedMesh");
            }

            if (mode.hasMixedValue || Mathf.RoundToInt(mode.floatValue) == 1)
            {
                DrawProperty(materialEditor, properties, "_FresnelScale");
                DrawProperty(materialEditor, properties, "_FresnelPower");
                DrawProperty(materialEditor, properties, "_FresnelIntensity");
            }

            if (mode.hasMixedValue || Mathf.RoundToInt(mode.floatValue) == 2)
                DrawProperty(materialEditor, properties, "_UnionWidthPixels");

            if (mode.hasMixedValue || Mathf.RoundToInt(mode.floatValue) != 1)
            {
                MaterialProperty fill = FindProperty("_FillInterior", properties);
                materialEditor.ShaderProperty(fill, fill.displayName);
                if (fill.hasMixedValue || fill.floatValue > 0.5f)
                    DrawProperty(materialEditor, properties, "_FillColor");
            }

            if (EditorGUI.EndChangeCheck())
                syncPending = true;
            Material[] targets = ToMaterials(materialEditor.targets);
            // 설정을 실제로 바꿨을 때만 Renderer Feature를 맞춘다. 리페인트마다 에셋을 훑지 않고,
            // 슬라이더를 끄는 중이면 놓은 뒤 한 번만 반영한다.
            if (syncPending && GUIUtility.hotControl == 0)
            {
                syncPending = false;
                if (ComputeSignature(targets) != lastSyncedSignature)
                    SyncRendererFeatures(targets);
            }
            DrawFeatureStatus(targets);

            EditorGUILayout.Space();
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();

            foreach (Material target in targets)
            {
                ValidateMaterial(target);
            }
        }

        private static void DrawProperty(MaterialEditor materialEditor, MaterialProperty[] properties, string name)
        {
            MaterialProperty property = FindProperty(name, properties);
            materialEditor.ShaderProperty(property, property.displayName);
        }

        public override void ValidateMaterial(Material material)
        {
            // 모드에 맞춰 단일 패스의 컬링, 블렌딩, 셰이더 변형을 함께 전환한다.
            int mode = Mathf.RoundToInt(material.GetFloat("_OutlineMode"));
            bool useFresnel = mode == 1;
            bool useUnion = mode == 2;
            if (useFresnel)
            {
                material.EnableKeyword("_OUTLINE_FRESNEL");
            }
            else
            {
                material.DisableKeyword("_OUTLINE_FRESNEL");
            }

            // Inverted Hull에서만 헐 전용 스텐실 비트(2)로 본체가 보이는 영역을 걸러 낸다.
            bool useHullStencil = mode == 0 && material.GetFloat("_HullStencilUnionMask") > 0.5f;
            material.SetFloat("_StencilRef", useHullStencil ? 4f : 0f);
            material.SetFloat("_StencilReadMask", useHullStencil ? 4f : 0f);
            material.SetFloat("_StencilComp", (float)(useHullStencil ? CompareFunction.NotEqual : CompareFunction.Always));

            material.SetFloat("_Cull", (float)(useFresnel ? CullMode.Back : CullMode.Front));
            material.SetFloat("_SrcBlend", (float)(useFresnel ? BlendMode.SrcAlpha : BlendMode.One));
            material.SetFloat("_DstBlend", (float)(useFresnel ? BlendMode.OneMinusSrcAlpha : BlendMode.Zero));
            material.SetShaderPassEnabled("SRPDefaultUnlit", !useUnion);
            material.SetShaderPassEnabled("BKKOutlineUnionMask", useUnion);
            material.SetShaderPassEnabled("BKKOutlineFill", !useFresnel && material.GetFloat("_FillInterior") > 0.5f);
            material.SetShaderPassEnabled("BKKOutlineHullMask", useHullStencil);
        }

        private static Material[] ToMaterials(Object[] targets)
        {
            var materials = new Material[targets.Length];
            for (int i = 0; i < targets.Length; i++)
                materials[i] = (Material)targets[i];
            return materials;
        }

        private static int ComputeSignature(Material[] materials)
        {
            unchecked
            {
                int hash = 17;
                foreach (Material material in materials)
                {
                    Usage usage = default;
                    Accumulate(ref usage, material, material.shader);
                    hash = hash * 31 + material.GetEntityId().GetHashCode();
                    hash = hash * 31 + (usage.screenSpace ? 1 : 0);
                    hash = hash * 31 + (usage.fill ? 1 : 0);
                    hash = hash * 31 + (usage.hullStencil ? 1 : 0);
                    hash = hash * 31 + usage.width;
                }

                return hash;
            }
        }

        private static void Accumulate(ref Usage usage, Material material, Shader shader)
        {
            if (material == null || material.shader != shader)
                return;
            int mode = Mathf.RoundToInt(material.GetFloat("_OutlineMode"));
            if (mode == 2)
            {
                usage.screenSpace = true;
                usage.width = Mathf.Max(usage.width, Mathf.CeilToInt(material.GetFloat("_UnionWidthPixels")));
            }

            if (mode != 1 && material.GetFloat("_FillInterior") > 0.5f)
                usage.fill = true;
            if (mode == 0 && material.GetFloat("_HullStencilUnionMask") > 0.5f)
                usage.hullStencil = true;
        }

        // 프로젝트의 아웃라인 재질이 실제로 쓰는 기능을 모은다. 쓰지 않는 패스는 기록하지 않기 위해서다.
        private static Usage ScanProject(Shader shader, Material[] edited)
        {
            var usage = new Usage { width = 1 };
            foreach (Material material in edited)
                Accumulate(ref usage, material, shader);

            foreach (string path in FindOutlineMaterialPaths(shader))
                Accumulate(ref usage, AssetDatabase.LoadAssetAtPath<Material>(path), shader);

            return usage;
        }

        private static List<string> FindOutlineMaterialPaths(Shader shader)
        {
            if (outlineMaterialPaths != null)
                return outlineMaterialPaths;

            outlineMaterialPaths = new List<string>();
            string shaderPath = AssetDatabase.GetAssetPath(shader);
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.Array.IndexOf(AssetDatabase.GetDependencies(path, false), shaderPath) >= 0)
                    outlineMaterialPaths.Add(path);
            }

            return outlineMaterialPaths;
        }

        /// <summary>재질이 추가·삭제·이동되거나 다시 임포트되면 모아 둔 경로를 버린다.</summary>
        internal static void InvalidateMaterialPaths()
        {
            outlineMaterialPaths = null;
        }

        private static void SyncRendererFeatures(Material[] edited)
        {
            if (edited.Length == 0 || edited[0].shader == null)
                return;
            Shader shader = edited[0].shader;
            Usage usage = ScanProject(shader, edited);
            bool needed = usage.screenSpace || usage.fill || usage.hullStencil;
            foreach (UniversalRendererData rendererData in ProjectRendererData())
            {
                SimpleMeshOutlineUnionFeature feature = FindFeature(rendererData);
                // 쓰는 기능이 없으면 새로 등록하지 않는다. 이미 있으면 모든 패스를 꺼서 비용을 없앤다.
                if (feature == null && !needed)
                    continue;

                Undo.RegisterCompleteObjectUndo(rendererData, "아웃라인 렌더러 기능 설정");
                if (feature == null)
                {
                    feature = ScriptableObject.CreateInstance<SimpleMeshOutlineUnionFeature>();
                    feature.name = "Simple Mesh Outline Union";
                    Undo.RegisterCreatedObjectUndo(feature, "아웃라인 렌더러 기능 설정");
                    AssetDatabase.AddObjectToAsset(feature, rendererData);
                    rendererData.rendererFeatures.Add(feature);
                    UpdateFeatureMap(rendererData);
                }
                else
                {
                    Undo.RecordObject(feature, "아웃라인 렌더러 기능 설정");
                }

                feature.SetShader(shader);
                feature.SetActive(true);
                feature.Configure(usage.screenSpace, usage.fill, usage.hullStencil, usage.width);
                rendererData.SetDirty();
                EditorUtility.SetDirty(feature);
                EditorUtility.SetDirty(rendererData);
                AssetDatabase.SaveAssetIfDirty(rendererData);
            }

            lastSyncedSignature = ComputeSignature(edited);
        }

        private static void DrawFeatureStatus(Material[] targets)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null || targets.Length == 0)
                return;

            Usage usage = default;
            foreach (Material material in targets)
                Accumulate(ref usage, material, material.shader);
            if (!usage.screenSpace && !usage.fill && !usage.hullStencil)
                return;

            bool ready = false;
            foreach (ScriptableRendererData data in pipeline.rendererDataList)
            {
                SimpleMeshOutlineUnionFeature feature = FindFeature(data as UniversalRendererData);
                if (feature == null || !feature.isActive)
                    continue;
                ready = (!usage.screenSpace || feature.SupportsScreenSpaceOutline) &&
                        (!usage.fill || feature.SupportsInteriorFill) &&
                        (!usage.hullStencil || feature.SupportsHullStencilMask) &&
                        (!usage.screenSpace || feature.MaxWidthPixels >= usage.width);
                break;
            }

            if (ready)
                return;
            EditorGUILayout.HelpBox("이 재질이 쓰는 기능이 현재 URP 렌더러의 Simple Mesh Outline Union에 " +
                                    "등록되어 있지 않거나 꺼져 있습니다.", MessageType.Warning);
            if (GUILayout.Button("Renderer Feature 설정 맞추기"))
                SyncRendererFeatures(targets);
        }

        private static SimpleMeshOutlineUnionFeature FindFeature(UniversalRendererData rendererData)
        {
            if (rendererData == null)
                return null;
            foreach (ScriptableRendererFeature existing in rendererData.rendererFeatures)
            {
                if (existing is SimpleMeshOutlineUnionFeature unionFeature)
                    return unionFeature;
            }

            return null;
        }

        // 현재·기본·품질별 URP 설정의 렌더러 중 프로젝트 에셋만 모은다.
        // 패키지나 읽기 전용 위치의 에셋은 저장할 수 없으므로 건드리지 않는다.
        private static IEnumerable<UniversalRendererData> ProjectRendererData()
        {
            var pipelines = new List<UniversalRenderPipelineAsset>();
            AddPipeline(pipelines, GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset);
            AddPipeline(pipelines, GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset);
            for (int i = 0; i < QualitySettings.names.Length; i++)
                AddPipeline(pipelines, QualitySettings.GetRenderPipelineAssetAt(i) as UniversalRenderPipelineAsset);

            var result = new List<UniversalRendererData>();
            foreach (UniversalRenderPipelineAsset pipeline in pipelines)
            {
                foreach (ScriptableRendererData data in pipeline.rendererDataList)
                {
                    if (data is UniversalRendererData universal && IsProjectAsset(universal) && !result.Contains(universal))
                        result.Add(universal);
                }
            }

            return result;
        }

        private static void AddPipeline(List<UniversalRenderPipelineAsset> pipelines, UniversalRenderPipelineAsset pipeline)
        {
            if (pipeline != null && IsProjectAsset(pipeline) && !pipelines.Contains(pipeline))
                pipelines.Add(pipeline);
        }

        private static bool IsProjectAsset(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            return !string.IsNullOrEmpty(path) && path.StartsWith("Assets/", System.StringComparison.Ordinal);
        }

        // 재시작 후에도 서브에셋 참조를 복원할 수 있도록 URP의 대응표를 갱신한다.
        private static void UpdateFeatureMap(UniversalRendererData rendererData)
        {
            var rendererObject = new SerializedObject(rendererData);
            SerializedProperty featureMap = rendererObject.FindProperty("m_RendererFeatureMap");
            featureMap.arraySize = rendererData.rendererFeatures.Count;
            for (int index = 0; index < rendererData.rendererFeatures.Count; index++)
            {
                long localId = 0;
                ScriptableRendererFeature entry = rendererData.rendererFeatures[index];
                if (entry != null)
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(entry, out string guid, out localId);
                featureMap.GetArrayElementAtIndex(index).longValue = localId;
            }

            rendererObject.ApplyModifiedProperties();
        }
    }

    // 렌더러 에셋 저장처럼 재질과 무관한 임포트에는 반응하지 않도록 재질 경로만 본다.
    internal sealed class SimpleMeshOutlineMaterialWatcher : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (HasMaterial(importedAssets) || HasMaterial(deletedAssets) || HasMaterial(movedAssets) ||
                HasMaterial(movedFromAssetPaths))
                SimpleMeshOutlineShaderGUI.InvalidateMaterialPaths();
        }

        private static bool HasMaterial(string[] paths)
        {
            foreach (string path in paths)
            {
                if (path.EndsWith(".mat", System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
