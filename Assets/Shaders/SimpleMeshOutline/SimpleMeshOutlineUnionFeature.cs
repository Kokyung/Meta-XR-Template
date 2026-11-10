using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace BKK.Graphics
{
    [DisallowMultipleRendererFeature]
    public sealed class SimpleMeshOutlineUnionFeature : ScriptableRendererFeature
    {
        private const string ShaderName = "BKK/Simple Mesh Outline";
        private static readonly int SearchRadiusId = Shader.PropertyToID("_UnionSearchRadius");

        [SerializeField] private Shader outlineShader;

        [SerializeField]
        [Tooltip("아웃라인을 합성할 시점. 스카이박스 이후이면서 일반 반투명 직전이어야 표시가 기존과 같다. " +
                 "화면을 지우고 다시 그리는 다른 Renderer Feature가 있으면 그보다 뒤여야 아웃라인이 그 위에 남는다. " +
                 "Inverted Hull의 스텐실 마스크도 여기서 기록하므로 반투명보다 앞이어야 한다. " +
                 "같은 시점끼리는 렌더러 기능 목록의 위에서 아래 순서로 실행된다.")]
        private RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingTransparents;

        [SerializeField]
        [Tooltip("Screen Space Outline 재질을 그린다. 끄면 마스크 렌더 타깃과 전체 화면 확장 패스를 만들지 않고 " +
                 "중간 텍스처도 요구하지 않는다. 재질 인스펙터에서 설정을 바꾸면 프로젝트 재질에 맞춰 자동으로 정해진다.")]
        private bool supportScreenSpaceOutline = true;

        [SerializeField]
        [Tooltip("재질의 내부 색 채우기를 그린다. 재질 인스펙터에서 설정을 바꾸면 자동으로 정해진다.")]
        private bool supportInteriorFill = true;

        [SerializeField]
        [Tooltip("Inverted Hull의 스텐실 유니온 마스크를 기록한다. 재질 인스펙터에서 설정을 바꾸면 자동으로 정해진다.")]
        private bool supportHullStencilMask = true;

        [SerializeField]
        [Range(1, 16)]
        [Tooltip("Screen Space Outline 확장 패스가 검사하는 최대 반지름(픽셀). 화면 전체에서 샘플 수가 이 값에 비례한다. " +
                 "이보다 두꺼운 재질은 이 값으로 잘린다. 재질 인스펙터에서 설정을 바꾸면 프로젝트의 가장 두꺼운 값으로 맞춰진다.")]
        private int maxWidthPixels = 16;

        [SerializeField]
        [Tooltip("등록된 아웃라인 대상이 없어도 Screen Space Outline 패스를 항상 기록한다. " +
                 "SimpleMeshOutlineRuntime이나 SimpleMeshOutlineRegistry로 대상을 알리지 않는 프로젝트에서만 켠다. " +
                 "켜면 아웃라인이 하나도 없는 프레임에도 전체 화면 확장 비용과 중간 텍스처 비용이 그대로 든다.")]
        private bool renderWithoutRegisteredTarget;

        private Material material;
        private SurfacePass surfacePass;
        private ScreenSpacePass screenSpacePass;

        public bool SupportsScreenSpaceOutline => supportScreenSpaceOutline;
        public bool SupportsInteriorFill => supportInteriorFill;
        public bool SupportsHullStencilMask => supportHullStencilMask;
        public int MaxWidthPixels => maxWidthPixels;

        public void SetShader(Shader shader)
        {
            if (outlineShader == shader && material != null)
                return;
            outlineShader = shader;
            Create();
        }

        /// <summary>
        /// 프로젝트 재질이 실제로 쓰는 기능에 맞춰 기록할 패스와 확장 반지름을 정한다.
        /// 값이 바뀌었으면 true를 돌려준다.
        /// </summary>
        public bool Configure(bool screenSpaceOutline, bool interiorFill, bool hullStencilMask, int widthPixels)
        {
            int width = Mathf.Clamp(widthPixels, 1, 16);
            if (supportScreenSpaceOutline == screenSpaceOutline && supportInteriorFill == interiorFill &&
                supportHullStencilMask == hullStencilMask && maxWidthPixels == width)
                return false;

            supportScreenSpaceOutline = screenSpaceOutline;
            supportInteriorFill = interiorFill;
            supportHullStencilMask = hullStencilMask;
            maxWidthPixels = width;
            Create();
            return true;
        }

        public override void Create()
        {
            CoreUtils.Destroy(material);
            material = null;
            if (outlineShader == null)
                outlineShader = Shader.Find(ShaderName);
            if (outlineShader == null)
                return;
            material = CoreUtils.CreateEngineMaterial(outlineShader);
            // 전체 화면 확장 패스는 이 값만큼만 이웃을 검사한다. 재질별 두께는 마스크의 알파가 담는다.
            material.SetFloat(SearchRadiusId, Mathf.Clamp(maxWidthPixels, 1, 16));
            surfacePass = new SurfacePass(supportInteriorFill, supportHullStencilMask)
            {
                renderPassEvent = injectionPoint
            };
            screenSpacePass = new ScreenSpacePass(material)
            {
                renderPassEvent = injectionPoint
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (material == null)
                return;
            // 카메라 자신의 색·깊이에만 그리므로 중간 텍스처를 요구하지 않는다.
            if (supportInteriorFill || supportHullStencilMask)
                renderer.EnqueuePass(surfacePass);
            // 사용자 렌더 타깃과 카메라 깊이를 함께 붙이는 이 패스만 중간 텍스처를 요구한다.
            // 쓰지 않을 때 큐에 넣지 않아야 XR이 화면 버퍼에 바로 그리는 경로를 유지한다.
            if (!supportScreenSpaceOutline)
                return;
            // 대상이 없는 프레임에서는 전체 화면 확장 패스와 중간 텍스처를 통째로 건너뛴다.
            // 편집 중에는 등록이 일어나지 않으므로 판정을 적용하지 않는다.
            if (Application.isPlaying && !renderWithoutRegisteredTarget && !SimpleMeshOutlineRegistry.HasActiveTarget)
                return;
            renderer.EnqueuePass(screenSpacePass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
            material = null;
        }

        private static FilteringSettings CreateFiltering()
        {
            // 게임 쪽에서 제외한 레이어는 아웃라인 대상에서 뺀다.
            return new FilteringSettings(RenderQueueRange.transparent, SimpleMeshOutlineRenderSettings.LayerMask);
        }

        /// <summary>
        /// 카메라 색·깊이에 붙는 패스가 URP 기본 그리기 패스와 같은 포비에이션 상태를 쓰는지.
        /// 상태가 다르면 Render Graph가 네이티브 렌더 패스를 끊는다. Quest처럼 저장하지 않는 MSAA 눈 버퍼에
        /// 바로 그리면 끊긴 앞쪽의 색·깊이·스텐실이 사라져 불투명 오브젝트가 지워지고 헐이 실루엣 전체를 덮는다.
        /// </summary>
        private static bool UseFoveatedRasterization(UniversalCameraData camera, UniversalResourceData resources)
        {
            if (!camera.xr.enabled || !camera.xr.supportsFoveatedRendering)
                return false;
            if (resources.isActiveTargetBackBuffer)
                return true;
            // URP 내부의 canFoveateIntermediatePasses와 같은 조건이다.
            // 모바일 XR은 기본 뷰포트 배율에서만 중간 텍스처 패스를 포비에이션한다.
            return !IsUrpXRMobile() || XRSystem.GetRenderViewportScale() == 1f;
        }

        private static bool? urpXRMobile;

        /// <summary>
        /// URP가 모바일 XR로 판정했는지. URP는 이 값을 처음 읽을 때 한 번 계산해 캐시하므로,
        /// XR이 늦게 시작하면 플랫폼만 보고 판정한 값과 달라질 수 있다. 같은 캐시 값을 읽어 URP 패스와 맞춘다.
        /// </summary>
        private static bool IsUrpXRMobile()
        {
            if (urpXRMobile.HasValue)
                return urpXRMobile.Value;

            PropertyInfo property = typeof(UniversalRenderPipeline).Assembly
                .GetType("UnityEngine.Rendering.Universal.PlatformAutoDetect")
                ?.GetProperty("isXRMobile", BindingFlags.Static | BindingFlags.NonPublic);
            if (property != null && property.PropertyType == typeof(bool))
            {
                urpXRMobile = (bool)property.GetValue(null);
                return urpXRMobile.Value;
            }

            // URP 내부 구조가 바뀌어 읽지 못하면 URP와 같은 플랫폼 조건으로 대신한다.
            RuntimePlatform platform = Application.platform;
            urpXRMobile = platform == RuntimePlatform.Android || platform == RuntimePlatform.WSAPlayerARM ||
                          platform == RuntimePlatform.WSAPlayerX86 || platform == RuntimePlatform.WSAPlayerX64;
            Debug.LogWarning("[아웃라인] URP의 PlatformAutoDetect.isXRMobile을 읽지 못해 플랫폼으로 판정합니다. " +
                             "뷰포트 배율이 1이 아닐 때 포비에이션 상태가 URP 패스와 어긋날 수 있습니다.");
            return urpXRMobile.Value;
        }

        private sealed class SurfacePass : ScriptableRenderPass
        {
            private readonly bool interiorFill;
            private readonly bool hullStencilMask;
            private readonly ShaderTagId fillTag = new ShaderTagId("BKKOutlineFill");
            private readonly ShaderTagId hullMaskTag = new ShaderTagId("BKKOutlineHullMask");

            private sealed class PassData
            {
                public RendererListHandle renderers;
            }

            public SurfacePass(bool interiorFill, bool hullStencilMask)
            {
                this.interiorFill = interiorFill;
                this.hullStencilMask = hullStencilMask;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                UniversalResourceData resources = frameData.Get<UniversalResourceData>();
                UniversalCameraData camera = frameData.Get<UniversalCameraData>();
                UniversalRenderingData rendering = frameData.Get<UniversalRenderingData>();
                UniversalLightData lights = frameData.Get<UniversalLightData>();
                FilteringSettings filtering = CreateFiltering();
                bool foveated = UseFoveatedRasterization(camera, resources);

                // 두 패스의 첨부와 포비에이션 상태가 앞뒤 URP 패스와 같아 하나의 네이티브 패스로 합쳐진다.
                if (interiorFill)
                {
                    DrawingSettings drawing = RenderingUtils.CreateDrawingSettings(fillTag, rendering, camera, lights, SortingCriteria.CommonTransparent);
                    RendererListHandle list = graph.CreateRendererList(new RendererListParams(rendering.cullResults, drawing, filtering));
                    using (var builder = graph.AddRasterRenderPass<PassData>("아웃라인 내부 채우기", out var data))
                    {
                        data.renderers = list;
                        builder.UseRendererList(list);
                        builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                        builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
                        builder.EnableFoveatedRasterization(foveated);
                        builder.SetRenderFunc((PassData pass, RasterGraphContext context) =>
                            context.cmd.DrawRendererList(pass.renderers));
                    }
                }

                if (hullStencilMask)
                {
                    // Inverted Hull의 헐은 반투명 큐에서 그려지므로 그 전에 본체의 보이는 영역을 스텐실에 기록한다.
                    DrawingSettings drawing = RenderingUtils.CreateDrawingSettings(hullMaskTag, rendering, camera, lights, SortingCriteria.CommonTransparent);
                    RendererListHandle list = graph.CreateRendererList(new RendererListParams(rendering.cullResults, drawing, filtering));
                    using (var builder = graph.AddRasterRenderPass<PassData>("아웃라인 헐 스텐실 마스크", out var data))
                    {
                        data.renderers = list;
                        builder.UseRendererList(list);
                        builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                        builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.ReadWrite);
                        builder.EnableFoveatedRasterization(foveated);
                        builder.SetRenderFunc((PassData pass, RasterGraphContext context) =>
                            context.cmd.DrawRendererList(pass.renderers));
                    }
                }
            }
        }

        private sealed class ScreenSpacePass : ScriptableRenderPass
        {
            private readonly Material material;
            private readonly int clearIndex;
            private readonly int collapseIndex;
            private readonly int horizontalIndex;
            private readonly int compositeIndex;
            private readonly ShaderTagId maskTag = new ShaderTagId("BKKOutlineUnionMask");
            private static readonly int MaskMsId = Shader.PropertyToID("_OutlineMaskMS");
            private static readonly int MaskSamplesId = Shader.PropertyToID("_OutlineMaskSamples");

            private sealed class MaskData
            {
                public Material material;
                public int clearIndex;
                public RendererListHandle renderers;
            }

            private sealed class BlitData
            {
                public Material material;
                public int passIndex;
                public TextureHandle source;
            }

            private sealed class CollapseData
            {
                public Material material;
                public int passIndex;
                public TextureHandle source;
            }

            public ScreenSpacePass(Material material)
            {
                this.material = material;
                clearIndex = material.FindPass("UnionClear");
                collapseIndex = material.FindPass("UnionCollapse");
                horizontalIndex = material.FindPass("UnionHorizontal");
                compositeIndex = material.FindPass("UnionComposite");
                // 마스크 렌더 타깃과 카메라 깊이를 함께 붙이려면 크기·샘플 수가 맞는 카메라 타깃이 필요하다.
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                UniversalResourceData resources = frameData.Get<UniversalResourceData>();
                UniversalCameraData camera = frameData.Get<UniversalCameraData>();
                UniversalRenderingData rendering = frameData.Get<UniversalRenderingData>();
                UniversalLightData lights = frameData.Get<UniversalLightData>();
                // 카메라 색·깊이에 붙는 마스크·합성 패스만 URP 기본 패스를 따른다. 자체 텍스처끼리의 확장은 켜지 않는다.
                bool foveated = UseFoveatedRasterization(camera, resources);

                // 내장 화면 버퍼에는 조회 가능한 Render Graph 설명자가 없으므로 카메라 설정을 사용한다.
                RenderTextureDescriptor descriptor = camera.cameraTargetDescriptor;
                descriptor.graphicsFormat = SelectMaskFormat(camera.isHdrEnabled, descriptor.msaaSamples);
                descriptor.depthBufferBits = 0;
                descriptor.depthStencilFormat = GraphicsFormat.None;
                descriptor.bindMS = false;
                descriptor.enableRandomWrite = false;
                descriptor.useMipMap = false;
                descriptor.autoGenerateMips = false;
                // 마스크는 카메라 깊이와 함께 붙어 가림과 스텐실을 처리하므로 카메라의 샘플 수를 따른다.
                // MSAA면 평균으로 해석하지 않고 샘플을 직접 읽어 1샘플 텍스처로 모은 뒤 확장한다.
                int samples = descriptor.msaaSamples;
                bool multisampled = samples > 1;
                descriptor.bindMS = multisampled;
                TextureHandle mask = UniversalRenderer.CreateRenderGraphTexture(graph, descriptor,
                    "BKK Outline Mask", true, FilterMode.Point);
                material.SetInteger(MaskSamplesId, samples);
                descriptor.bindMS = false;
                descriptor.msaaSamples = 1;
                TextureHandle horizontal = UniversalRenderer.CreateRenderGraphTexture(graph, descriptor,
                    "BKK Outline Horizontal", false, FilterMode.Point);
                TextureHandle expandSource = multisampled
                    ? UniversalRenderer.CreateRenderGraphTexture(graph, descriptor, "BKK Outline Mask Resolved", false, FilterMode.Point)
                    : mask;

                DrawingSettings drawing = RenderingUtils.CreateDrawingSettings(maskTag, rendering, camera, lights, SortingCriteria.CommonTransparent);
                RendererListHandle list = graph.CreateRendererList(new RendererListParams(rendering.cullResults, drawing, CreateFiltering()));

                using (var builder = graph.AddRasterRenderPass<MaskData>("아웃라인 실루엣", out var data))
                {
                    data.material = material;
                    data.clearIndex = clearIndex;
                    data.renderers = list;
                    builder.UseRendererList(list);
                    builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.ReadWrite);
                    builder.EnableFoveatedRasterization(foveated);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc((MaskData pass, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), pass.material, pass.clearIndex);
                        context.cmd.DrawRendererList(pass.renderers);
                    });
                }

                if (multisampled)
                {
                    using (var builder = graph.AddRasterRenderPass<CollapseData>("아웃라인 마스크 샘플 모으기", out var data))
                    {
                        data.material = material;
                        data.passIndex = collapseIndex;
                        data.source = mask;
                        builder.UseTexture(mask, AccessFlags.Read);
                        builder.SetRenderAttachment(expandSource, 0, AccessFlags.Write);
                        builder.AllowGlobalStateModification(true);
                        builder.SetRenderFunc((CollapseData pass, RasterGraphContext context) =>
                        {
                            // 멀티샘플 텍스처는 일반 블릿 입력으로 묶을 수 없어 전용 이름으로 넘긴다.
                            context.cmd.SetGlobalTexture(MaskMsId, pass.source);
                            Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), pass.material, pass.passIndex);
                        });
                    }
                }

                using (var builder = graph.AddRasterRenderPass<BlitData>("아웃라인 가로 확장", out var data))
                {
                    data.material = material;
                    data.passIndex = horizontalIndex;
                    data.source = expandSource;
                    builder.UseTexture(expandSource, AccessFlags.Read);
                    builder.SetRenderAttachment(horizontal, 0, AccessFlags.Write);
                    builder.SetRenderFunc((BlitData pass, RasterGraphContext context) =>
                        Blitter.BlitTexture(context.cmd, pass.source, new Vector4(1, 1, 0, 0), pass.material, pass.passIndex));
                }

                using (var builder = graph.AddRasterRenderPass<BlitData>("아웃라인 세로 확장 및 합성", out var data))
                {
                    data.material = material;
                    data.passIndex = compositeIndex;
                    data.source = horizontal;
                    builder.UseTexture(horizontal, AccessFlags.Read);
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                    builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.ReadWrite);
                    builder.EnableFoveatedRasterization(foveated);
                    builder.SetRenderFunc((BlitData pass, RasterGraphContext context) =>
                        Blitter.BlitTexture(context.cmd, pass.source, new Vector4(1, 1, 0, 0), pass.material, pass.passIndex));
                }
            }

            private static GraphicsFormat SelectMaskFormat(bool hdr, int msaaSamples)
            {
                // 알파는 0~1로 정규화해 담으므로 8비트에서도 잘리지 않는다. HDR 카메라에서만 밝은 색을 보존한다.
                if (hdr)
                {
                    GraphicsFormatUsage usage = GraphicsFormatUsage.Render;
                    if (msaaSamples >= 8)
                        usage |= GraphicsFormatUsage.MSAA8x;
                    else if (msaaSamples >= 4)
                        usage |= GraphicsFormatUsage.MSAA4x;
                    else if (msaaSamples >= 2)
                        usage |= GraphicsFormatUsage.MSAA2x;
                    if (SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, usage))
                        return GraphicsFormat.R16G16B16A16_SFloat;
                }

                return GraphicsFormat.R8G8B8A8_UNorm;
            }
        }
    }
}
