Shader "BeaverCreek/Drizzle and ripples"
{
    Properties
    {
        _BaseColor("Tint",Color) = (.76,.85,.89,.35)
        _Ripple("Ripple instead of streak",Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+12" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _Ripple;
            CBUFFER_END
            struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;float fog:TEXCOORD1;};
            Varyings Vert(Attributes v)
            {
                Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;o.color=v.color;
                o.fog=ComputeFogFactor(o.positionCS.z);return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float2 uv=i.uv*2-1;
                float r=length(uv);
                float rings=(1-smoothstep(.018,.065,abs(r-.79)))+.3*(1-smoothstep(.02,.05,abs(r-.48)));
                float streak=pow(saturate(1-abs(uv.x)),2)*pow(saturate(1-uv.y*uv.y),.7);
                half4 c=i.color*_BaseColor;c.a*=lerp(streak,rings,saturate(_Ripple));
                c.rgb=MixFog(c.rgb,i.fog);return c;
            }
            ENDHLSL
        }
    }
}
