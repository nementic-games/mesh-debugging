Shader "Hidden/UV-Preview"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Alpha ("Alpha", Range(0, 1)) = 1
		[Enum(UnityEngine.Rendering.ColorWriteMask)] _ColorWriteMask("ColorWriteMask", Float) = 15 //"All"
    }
    SubShader
    {
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask[_ColorWriteMask]

		// Regular map
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				float2 clipUV : TEXCOORD1;
            };

            sampler2D _MainTex;
			sampler2D _GUIClipTexture;
			uniform float4x4 unity_GUIClipTextureMatrix;
            float4 _MainTex_ST;
			float _Alpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				float3 eyePos = UnityObjectToViewPos(v.vertex);
				o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
				col.a = _Alpha *  tex2D(_GUIClipTexture, i.clipUV).a;
				return col;
            }
            ENDCG
        }

		// Normal map
		Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				float2 clipUV : TEXCOORD1;
            };

            sampler2D _MainTex;
			sampler2D _GUIClipTexture;
			uniform float4x4 unity_GUIClipTextureMatrix;
            float4 _MainTex_ST;
			float _Alpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				float3 eyePos = UnityObjectToViewPos(v.vertex);
				o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				fixed3 col = (UnpackNormal(tex2D(_MainTex, i.uv)) / 2) + float3(0.5, 0.5, 0.5);
                fixed4 outcol = fixed4(col.r, col.g, col.b, 1);
				outcol.a = _Alpha *  tex2D(_GUIClipTexture, i.clipUV).a;
				return outcol;
            }
            ENDCG
        }
    }
}
