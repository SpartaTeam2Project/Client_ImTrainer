Shader "Runtime/Screen Wipe"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0, 0, 0, 1)
        _Progress ("Progress", Range(0, 2)) = 0
        _PixelSize ("Pixel Size", Float) = 67.5
        _Resolution ("Resolution", Vector) = (1920, 1080, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex WipeVert
            #pragma fragment WipeFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Progress;
                float _PixelSize;
                float4 _Resolution;
            CBUFFER_END

            Varyings WipeVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.color = input.color * _Color;
                output.uv = input.uv;
                return output;
            }

            half4 WipeFrag(Varyings input) : SV_Target
            {
                // 화면 중심 기준 좌표. 블록 격자도 중심에 맞춘다.
                float2 position = (input.uv - 0.5) * _Resolution.xy;
                if (_PixelSize > 0)
                {
                    position = (floor(position / _PixelSize) + 0.5) * _PixelSize;
                }

                // 중심을 지나는 직선이므로 반대편 부채꼴까지 한 번에 [0, PI) 로 접는다.
                float angle = fmod(atan2(position.y, position.x) + PI, PI);
                float sweep = _Progress * PI;

                // 0~1: 3시 방향부터 반시계로 덮는다. 1~2: 같은 방향으로 계속 돌며 걷어낸다.
                float covered = step(angle, sweep - 0.0001) * step(sweep - PI, angle);

                half4 color = input.color;
                color.a *= covered;
                return color;
            }
            ENDHLSL
        }
    }
}
