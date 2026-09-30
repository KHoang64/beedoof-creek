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
            float Hash(float3 p) {return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
            float Noise(float3 p)
            {
                float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                float a=lerp(Hash(i),Hash(i+float3(1,0,0)),f.x);
                float b=lerp(Hash(i+float3(0,1,0)),Hash(i+float3(1,1,0)),f.x);
                float c=lerp(Hash(i+float3(0,0,1)),Hash(i+float3(1,0,1)),f.x);
                float e=lerp(Hash(i+float3(0,1,1)),Hash(i+float3(1,1,1)),f.x);
                return lerp(lerp(a,b,f.y),lerp(c,e,f.y),f.z);
            }
            float Clouds(float3 p)
            {
                return Noise(p)*.50+Noise(p*2.07+9)*.27+Noise(p*4.13+23)*.15+Noise(p*8.3)*.08;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float3 d=normalize(i.direction);
                // Volume coordinates remain finite and seamless through the horizon.
                float3 p=d*5.5+float3(3.4,1.7,2.1);
                float n=Clouds(p);
                float relief=(Clouds(p+float3(-.13,.09,.07))-n)*1.3;
                float cloud=saturate(smoothstep(.30,.70,n)+relief+.12);
                half3 col=lerp(_CloudShade.rgb,_CloudLight.rgb,cloud);
                col=lerp(col,_CloudLight.rgb,.10*pow(1-saturate(d.y),6));
                return half4(col,1);
            }
            ENDHLSL
        }
    }
}
