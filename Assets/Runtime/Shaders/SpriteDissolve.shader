Shader "Runtime/Sprite Dissolve"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _Outline ("Outline", Range(0, 0.1)) = 0.03
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        HLSLINCLUDE
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
            float2 positionWS : TEXCOORD1;
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float _Dissolve;
            float _Outline;
        CBUFFER_END

        float DissolveHash(float2 position)
        {
            return frac(sin(dot(position, float2(127.1, 311.7))) * 43758.5453);
        }

        float DissolveNoise(float2 position)
        {
            float2 cell = floor(position);
            float2 fraction = frac(position);
            fraction = fraction * fraction * (3.0 - 2.0 * fraction);

            float bottomLeft = DissolveHash(cell);
            float bottomRight = DissolveHash(cell + float2(1.0, 0.0));
            float topLeft = DissolveHash(cell + float2(0.0, 1.0));
            float topRight = DissolveHash(cell + float2(1.0, 1.0));
            return lerp(lerp(bottomLeft, bottomRight, fraction.x), lerp(topLeft, topRight, fraction.y), fraction.y);
        }

        Varyings DissolveVert(Attributes input)
        {
            Varyings output;
            float3 positionWS = TransformObjectToWorld(input.positionOS);
            output.positionCS = TransformWorldToHClip(positionWS);
            output.positionWS = positionWS.xy;
            output.uv = TRANSFORM_TEX(input.uv, _MainTex);
            output.color = input.color * _Color;
            return output;
        }

        half4 DissolveFrag(Varyings input) : SV_Target
        {
            half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
            float noise = DissolveNoise(input.positionWS * 6.0);
            float difference = _Dissolve - noise;

            if (difference > _Outline)
            {
                color.a = 0;
            }
            else if (difference > 0)
            {
                color.rgb = 0;
            }

            color.rgb *= color.a;
            return color;
        }
        ENDHLSL

        Pass
        {
            Name "SpriteDissolve2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex DissolveVert
            #pragma fragment DissolveFrag
            ENDHLSL
        }

        Pass
        {
            Name "SpriteDissolveForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex DissolveVert
            #pragma fragment DissolveFrag
            ENDHLSL
        }
    }
}
