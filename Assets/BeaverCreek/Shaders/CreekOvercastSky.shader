Shader "BeaverCreek/Fully overcast sky"
{
    Properties
    {
        _CloudLight("Pale cloud",Color)=(.93,.94,.95,1)
        _CloudShade("Thick cloud",Color)=(.35,.38,.42,1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _CloudLight, _CloudShade;
            CBUFFER_END
            struct Varyings {float4 position:SV_POSITION;float3 direction:TEXCOORD0;};
            Varyings Vert(float4 vertex:POSITION)
            {
                Varyings o;o.position=TransformObjectToHClip(vertex.xyz);o.direction=vertex.xyz;return o;
            }
            float Hash(float2 p) {return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float Noise(float2 p)
            {
                float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
            }
            float Clouds(float2 p)
            {
                return Noise(p)*.50+Noise(p*2.07+9)*.27+Noise(p*4.13+23)*.15+Noise(p*8.3)*.08;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float3 d=normalize(i.direction);
                float2 p=d.xz/(.3+max(d.y,0))*3.0;
                p+=float2(3.4,1.7);
                float n=Clouds(p);
                // Continuous opaque cloud cover: no blue gaps and no sun disc.
                float relief=(Clouds(p+float2(-.13,.09))-n)*2.1;
                float cloud=saturate(smoothstep(.29,.69,n)+relief+.08);
                half3 col=lerp(_CloudShade.rgb,_CloudLight.rgb,cloud);
                col=lerp(col,_CloudLight.rgb,.13*pow(1-saturate(d.y),6));
                return half4(col,1);
            }
            ENDHLSL
        }
    }
}
