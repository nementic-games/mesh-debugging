Shader "Hidden/UV-Preview"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Alpha ("Alpha", Range(0, 1)) = 1
		_ColorMask ("Color Mask", Vector) = (1, 1, 1, 1)
	}
	SubShader
	{
		Tags
		{
			"RenderType" = "Transparent" "Queue" = "Transparent"
		}
		Cull Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

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
			float4 _MainTex_ST;
			float _Alpha;
			float4 _ColorMask;
			sampler2D _GUIClipTexture;
			float4x4 unity_GUIClipTextureMatrix;
			fixed _IsBumpMap;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				float3 eyePos = UnityObjectToViewPos(v.vertex);
				o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 texColor;

				// Convert from the packed normal map representation back to the original RGB
				// values to show the desired purple texture.
				if (_IsBumpMap)
					texColor = fixed4((UnpackNormal(tex2D(_MainTex, i.uv)) / 2) + float3(0.5, 0.5, 0.5), 1);
				else
					texColor = tex2D(_MainTex, i.uv);

				fixed4 color = fixed4(texColor.r, texColor.g, texColor.b, _Alpha);
				color.rgb *= _ColorMask.rgb;
				color.a *= tex2D(_GUIClipTexture, i.clipUV).a;
				return color;
			}
			ENDCG
		}
	}
}