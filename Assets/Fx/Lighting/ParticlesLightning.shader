Shader "Particles/Lightning" {
  Properties {
    [HDR]_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
    _MainTex ("Particle Texture", 2D) = "white" {}
    _Gradient("Gradient Texture", 2D) = "white" {}
    _Stretch("Stretch", Range(-2,2)) = 1.0
    _Offset("Offset", Range(-2,2)) = 1.0
    _Speed("Speed", Range(-2,2)) = 1.0
  }
 
  Category {
    Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
    Blend One OneMinusSrcAlpha
    ColorMask RGB
    Cull Off Lighting Off ZWrite Off
 
    SubShader {
      Pass {
 
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 3.0
        #pragma multi_compile_particles
        #pragma multi_compile_fog
 
        #include "UnityCG.cginc"
 
        sampler2D _MainTex, _Gradient;
        fixed4 _TintColor;
 
        struct appdata_t {
          float4 vertex : POSITION;
          fixed4 color : COLOR;
          float4 texcoord : TEXCOORD0;
          UNITY_VERTEX_INPUT_INSTANCE_ID
        };
 
        struct v2f {
          float4 vertex : SV_POSITION;
          fixed4 color : COLOR;
          float4 texcoord : TEXCOORD0;
          float2 texcoord2 : TEXCOORD3;
          UNITY_FOG_COORDS(1)
          UNITY_VERTEX_OUTPUT_STEREO
        };
 
        float4 _MainTex_ST;
        float4 _Gradient_ST;
        v2f vert (appdata_t v)
        {
          v2f o;
          UNITY_SETUP_INSTANCE_ID(v);
          UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
          o.vertex = UnityObjectToClipPos(v.vertex);
          
          o.color = v.color;
          o.texcoord.xy = TRANSFORM_TEX(v.texcoord,_MainTex);
          o.texcoord2 = TRANSFORM_TEX(v.texcoord,_Gradient);
 
          // Custom Data from particle system
          o.texcoord.z = v.texcoord.z;
          o.texcoord.w = v.texcoord.w;
          UNITY_TRANSFER_FOG(o,o.vertex);
          return o;
        }
 
        
        float _Stretch, _Offset;
        float _Speed;
 
        fixed4 frag (v2f i) : SV_Target
        {
          
          // Custom Data from particle system
          float lifetime = i.texcoord.z;
          float randomOffset = i.texcoord.w;
 
          //fade the edges
          float gradientfalloff =  smoothstep(0.99, 0.95, i.texcoord2.x) * smoothstep(0.99,0.95,1- i.texcoord2.x);
          // moving UVS
          float2 movingUV = float2(i.texcoord.x +randomOffset + (_Time.x * _Speed) ,i.texcoord.y);
          fixed tex = tex2D(_MainTex, movingUV)* gradientfalloff;
 
          //cutoff for alpha
          float cutoff = step(lifetime, tex);
 
          // stretched uv for gradient map
          float2 uv = float2((tex * _Stretch)- lifetime + _Offset, 1) ;
          float4 colorMap = tex2D(_Gradient, uv);
 
          // everything together
          fixed4 col;       
          col.rgb = colorMap.rgb * _TintColor * i.color.rgb;
          col.a = cutoff;
          col *= col.a;
          
          UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0,0,0,0)); // fog towards black due to our blend mode
          return col;
        }
        ENDCG
      }
    }
 
  }
}