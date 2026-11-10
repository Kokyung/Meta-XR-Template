Shader "BKK/Simple Mesh Outline"
{
    Properties
    {
        [Enum(Inverted Hull, 0, Fresnel, 1, Screen Space Outline, 2)] _OutlineMode ("아웃라인 모드", Float) = 0
        [HDR] _OutlineColor ("아웃라인 색상", Color) = (0, 0, 0, 1)
        [ToggleUI] _FillInterior ("내부 색 채우기", Float) = 0
        [HDR] _FillColor ("내부 색", Color) = (1, 1, 1, 1)
        _OutlineWidth ("아웃라인 두께 ", Range(0, 0.05)) = 0.003
        [ToggleUI] _HullHardEdgeFix ("각진 메시 모서리 보정", Float) = 1
        [ToggleUI] _HullStencilUnionMask ("스텐실 유니온 마스크", Float) = 1
        [ToggleUI] _HullOverlappedMesh ("중첩된 메시에 사용", Float) = 0
        _UnionWidthPixels ("아웃라인 두께", Range(0, 16)) = 3
        _FresnelScale ("Fresnel 스케일", Range(1, 1.2)) = 1
        _FresnelPower ("Fresnel 밝기", Range(0.5, 16)) = 4
        _FresnelIntensity ("Fresnel 세기", Range(0, 1)) = 1
        [HideInInspector] _UnionSearchRadius ("Union Search Radius", Float) = 16
        [HideInInspector] _StencilRef ("Stencil Ref", Float) = 4
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 4
        [HideInInspector] _StencilComp ("Stencil Comp", Float) = 6
        [HideInInspector] _Cull ("Cull", Float) = 1
        [HideInInspector] _SrcBlend ("Source Blend", Float) = 1
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Transparent-100" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _OutlineColor;
            half4 _FillColor;
            float _FillInterior;
            float _OutlineWidth;
            float _HullHardEdgeFix;
            float _HullStencilUnionMask;
            float _HullOverlappedMesh;
            float _UnionWidthPixels;
            float _UnionSearchRadius;
            float _OutlineMode;
            float _FresnelScale;
            float _FresnelPower;
            float _FresnelIntensity;
            float _StencilRef;
            float _StencilReadMask;
            float _StencilComp;
            float _Cull;
            float _SrcBlend;
            float _DstBlend;
        CBUFFER_END

        // Screen Space Outline 마스크의 알파에 '반지름 제곱 + 1'을 0~1로 정규화해 담는다.
        // 1보다 큰 값을 그대로 쓰면 8비트 타깃이나 형식 대체 시 1로 잘려 확장이 사라진다.
        // 255단계 정수로 반올림해 읽으므로 8비트와 부동소수점 타깃에서 같은 결과가 나온다.
        // 비어 있는 곳은 0이라 0픽셀 반지름(1)과도 구분된다.
        #define OUTLINE_RANGE_SCALE 255.0
        float EncodeOutlineRange(float range) { return min(range, OUTLINE_RANGE_SCALE) / OUTLINE_RANGE_SCALE; }
        float DecodeOutlineRange(float encoded) { return round(encoded * OUTLINE_RANGE_SCALE); }
        ENDHLSL

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull [_Cull]
            ZTest LEqual
            ZWrite Off
            // 패스스루처럼 눈 버퍼 알파로 합성하는 환경에서는 알파가 0인 픽셀이 보이지 않으므로 알파도 누적한다.
            Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
            ColorMask RGBA
            // 유니온 마스크가 켜지면 본체가 보이는 화면 영역에서는 헐을 버려 내부 윤곽선을 없앤다.
            Stencil { Ref [_StencilRef] ReadMask [_StencilReadMask] WriteMask 0 Comp [_StencilComp] Pass Keep Fail Keep ZFail Keep }
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            #pragma multi_compile_instancing
            #pragma shader_feature_local _ _OUTLINE_FRESNEL
            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                #if defined(_OUTLINE_FRESNEL)
                    float3 positionWS : TEXCOORD0;
                    half3 normalWS : TEXCOORD1;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings OutlineVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionOS = input.positionOS;
                #if defined(_OUTLINE_FRESNEL)
                    positionOS *= max(_FresnelScale, 1.0);
                #endif
                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_OUTLINE_FRESNEL)
                    output.positionWS = positionWS;
                    output.normalWS = normalWS;
                    output.positionCS = TransformWorldToHClip(positionWS);
                #else
                    float3 offsetDirWS = normalWS;
                    if (_HullHardEdgeFix > 0.5)
                    {
                        // 각진 메시는 한 모서리의 정점이 면마다 다른 법선을 가져 서로 다른 방향으로 밀리고 헐이 벌어진다.
                        // 렌더러 경계 상자 중심에서 뻗는 방향은 위치만으로 정해지므로 겹친 정점이 항상 같이 움직인다.
                        float3 centerWS = (unity_RendererBounds_Min.xyz + unity_RendererBounds_Max.xyz) * 0.5;
                        float3 centerDirWS = positionWS - centerWS;
                        float centerDistance = length(centerDirWS);
                        // 경계 상자가 없거나 중심과 겹치는 정점은 기존 법선으로 되돌린다.
                        if (centerDistance > 1e-5)
                            offsetDirWS = centerDirWS / centerDistance;
                    }
                    float3 positionVS = TransformWorldToView(positionWS);
                    float3 offsetVS = TransformWorldToViewDir(offsetDirWS) * max(_OutlineWidth, 0.0);
                    // 겹쳐 놓은 복제 메시가 아니라면 원본과 같은 표면을 다시 그리므로,
                    // 깊이 방향 성분을 항상 카메라 반대쪽으로 돌려 헐이 본체 앞으로 나오지 못하게 한다.
                    // 화면상의 두께를 정하는 가로·세로 성분은 그대로 두어 테두리 굵기는 변하지 않는다.
                    if (_HullOverlappedMesh < 0.5)
                        offsetVS.z = -abs(offsetVS.z);
                    output.positionCS = TransformWViewToHClip(positionVS + offsetVS);
                #endif
                return output;
            }
            half4 OutlineFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                // 연속 외곽선은 별도 합성하므로 기존 헐을 중복 출력하지 않는다.
                clip(1.5 - _OutlineMode);
                #if defined(_OUTLINE_FRESNEL)
                    half3 normalWS = normalize(input.normalWS);
                    half3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                    half rim = 1.0h - saturate(dot(normalWS, viewWS));
                    return half4(_OutlineColor.rgb, saturate(pow(rim, max(_FresnelPower, 0.001)) * _FresnelIntensity * _OutlineColor.a));
                #else
                    return half4(_OutlineColor.rgb, 1);
                #endif
            }
            ENDHLSL
        }

        Pass
        {
            Name "HullStencilMask"
            Tags { "LightMode" = "BKKOutlineHullMask" }
            Cull Off
            ZTest LEqual
            ZWrite Off
            ColorMask 0
            // 헐 전용 비트(2)를 사용해 Screen Space Outline의 비트 0과 겹치지 않는다. 비트 1은 다른 기능 몫으로 비워 둔다.
            Stencil { Ref 4 ReadMask 4 WriteMask 4 Comp Always Pass Replace }
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex HullMaskVertex
            #pragma fragment HullMaskFragment
            #pragma multi_compile_instancing
            struct HullMaskAttributes { float3 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct HullMaskVaryings { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            HullMaskVaryings HullMaskVertex(HullMaskAttributes input)
            {
                HullMaskVaryings output = (HullMaskVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                return output;
            }
            half4 HullMaskFragment(HullMaskVaryings input) : SV_Target
            {
                // 원본이 보이는 영역만 Inverted Hull 모드에서 표시한다.
                clip(0.1 - abs(_OutlineMode));
                clip(_HullStencilUnionMask - 0.5);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "InteriorFill"
            Tags { "LightMode" = "BKKOutlineFill" }
            Cull Back
            ZTest LEqual
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ColorMask RGBA
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex FillVertex
            #pragma fragment FillFragment
            #pragma multi_compile_instancing
            struct FillAttributes { float3 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct FillVaryings { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            FillVaryings FillVertex(FillAttributes input)
            {
                FillVaryings output = (FillVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                return output;
            }
            half4 FillFragment(FillVaryings input) : SV_Target
            {
                // 원본의 보이는 표면만 채우며 Fresnel 모드에는 적용하지 않는다.
                clip(_FillInterior - 0.5);
                clip(abs(_OutlineMode - 1.0) - 0.5);
                return half4(_FillColor.rgb, saturate(_FillColor.a));
            }
            ENDHLSL
        }

        Pass
        {
            Name "UnionMask"
            Tags { "LightMode" = "BKKOutlineUnionMask" }
            Cull Off
            ZTest LEqual
            ZWrite Off
            Blend Off
            Stencil { Ref 1 ReadMask 1 WriteMask 1 Comp Always Pass Replace }
            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex MaskVertex
            #pragma fragment MaskFragment
            #pragma multi_compile_instancing
            struct MaskAttributes { float3 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct MaskVaryings { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            MaskVaryings MaskVertex(MaskAttributes input)
            {
                MaskVaryings output = (MaskVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                return output;
            }
            half4 MaskFragment(MaskVaryings input) : SV_Target
            {
                clip(0.1 - abs(_OutlineMode - 2.0));
                // 검은색 외곽선도 구분하도록 알파에 유효 표시와 반지름을 함께 기록한다.
                float radius = clamp(_UnionWidthPixels, 0.0, 16.0);
                return half4(_OutlineColor.rgb, EncodeOutlineRange(radius * radius + 1.0));
            }
            ENDHLSL
        }

        Pass
        {
            Name "UnionClear"
            Tags { "LightMode" = "BKKOutlineUnionClear" }
            Cull Off
            ZTest Always
            ZWrite Off
            ColorMask 0
            Stencil { Ref 0 ReadMask 1 WriteMask 1 Comp Always Pass Replace }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment ClearFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            half4 ClearFragment(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "UnionCollapse"
            Tags { "LightMode" = "BKKOutlineCollapse" }
            Cull Off
            ZTest Always
            ZWrite Off
            Blend Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment CollapseFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            // MSAA 마스크를 평균으로 해석하면 실루엣 가장자리 픽셀의 범위가 커버리지만큼 줄어
            // 거기서 뻗는 확장이 한 칸씩 모자라고, 얇은 아웃라인은 대부분 사라진다.
            // 샘플을 직접 읽어 가장 넓은 범위를 1샘플 텍스처에 모은다.
            // 확장 패스가 이웃마다 모든 샘플을 다시 읽지 않도록 픽셀당 한 번만 읽는다.
            // URP Core.hlsl에는 읽기 매크로만 있어 선언은 코어 TextureXR.hlsl과 같은 규칙으로 직접 한다.
            #if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
                Texture2DMSArray<float4, 1> _OutlineMaskMS;
            #else
                Texture2DMS<float4, 1> _OutlineMaskMS;
            #endif
            int _OutlineMaskSamples;

            half4 CollapseFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                int2 pixel = int2(input.positionCS.xy);
                half4 best = 0;
                // 일부 플랫폼은 샘플 인덱스가 상수여야 하므로 URP 최대치(8)까지 펼치고 실제 샘플 수에서 멈춘다.
                [unroll] for (int s = 0; s < 8; s++)
                {
                    if (s >= _OutlineMaskSamples)
                        break;
                    half4 value = LOAD_TEXTURE2D_X_MSAA(_OutlineMaskMS, pixel, s);
                    if (value.a > best.a)
                        best = value;
                }
                return best;
            }
            ENDHLSL
        }

        Pass
        {
            Name "UnionHorizontal"
            Tags { "LightMode" = "BKKOutlineHorizontal" }
            Cull Off
            ZTest Always
            ZWrite Off
            Blend Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment HorizontalFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // 가로 offset만큼 떨어진 마스크 값을 읽는다. 범위 밖이면 알파 0을 돌려준다.
            half4 LoadMask(Varyings input, int offset)
            {
                float2 uv = input.texcoord + float2(offset * _BlitTexture_TexelSize.x, 0);
                if (any(uv < 0.0) || any(uv >= _BlitScaleBias.xy)) return 0;
                return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv, 0);
            }

            half4 HorizontalFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                // Renderer Feature가 정한 최대 반지름만큼만 검사한다. 샘플 수가 화면 전체에 곱해진다.
                int searchRadius = (int)clamp(_UnionSearchRadius, 1.0, 16.0);
                half3 bestColor = 0;
                float bestRemaining = 0.0;
                // 원형 범위의 남은 세로 반지름 제곱을 보존하여 대각선에도 빈틈 없이 확장한다.
                [loop] for (int x = -searchRadius; x <= searchRadius; x++)
                {
                    half4 sample = LoadMask(input, x);
                    float remaining = DecodeOutlineRange(sample.a) - x * x;
                    if (remaining >= 1.0 && remaining > bestRemaining)
                    {
                        bestRemaining = remaining;
                        bestColor = sample.rgb;
                    }
                }
                return half4(bestColor, EncodeOutlineRange(bestRemaining));
            }
            ENDHLSL
        }

        Pass
        {
            Name "UnionComposite"
            Tags { "LightMode" = "BKKOutlineComposite" }
            Cull Off
            ZTest Always
            ZWrite Off
            Blend Off
            // 패스스루 합성에서 보이도록 불투명 알파까지 기록한다.
            ColorMask RGBA
            Stencil { Ref 1 ReadMask 1 WriteMask 0 Comp NotEqual Pass Keep }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment CompositeFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            half4 CompositeFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                int searchRadius = (int)clamp(_UnionSearchRadius, 1.0, 16.0);
                half3 bestColor = 0;
                float bestRemaining = 0.0;
                [loop] for (int y = -searchRadius; y <= searchRadius; y++)
                {
                    float2 uv = input.texcoord + float2(0, y * _BlitTexture_TexelSize.y);
                    if (any(uv < 0.0) || any(uv >= _BlitScaleBias.xy)) continue;
                    half4 sample = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, uv, 0);
                    float remaining = DecodeOutlineRange(sample.a) - y * y;
                    if (remaining >= 1.0 && remaining > bestRemaining)
                    {
                        bestRemaining = remaining;
                        bestColor = sample.rgb;
                    }
                }
                clip(bestRemaining - 0.5);
                return half4(bestColor, 1);
            }
            ENDHLSL
        }
    }
    CustomEditor "BKK.Graphics.SimpleMeshOutlineShaderGUI"
    Fallback Off
}

