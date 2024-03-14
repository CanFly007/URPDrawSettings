Shader "Custom/BakeReplacementColor" 
{
	Properties
	{
		[MainTexture] _BaseMap("Albedo", 2D) = "white" {}
		[MainColor] _BaseColor("Color", Color) = (1,1,1,1)

		//[PerRendererData] _Color("Main Color", Color) = (1,1,1,1)
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
	}
		
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 100
		
		Pass
		{
			Lighting Off
			Cull [_Cull]
			Fog {Mode Off}
			
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#pragma multi_compile_instancing
				#pragma instancing_options nolightprobe nolightmap
			
				#include "UnityCG.cginc"

				struct a2v
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					float2 uv : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(fixed4, _BaseColor)
				UNITY_INSTANCING_BUFFER_END(Props)

				Texture2D _BaseMap;
				SamplerState sampler_BaseMap;
				float4 _BaseMap_ST;
				//float4 _BaseColor

				v2f vert(a2v v)
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_TRANSFER_INSTANCE_ID(v, o);

					o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
					o.vertex = UnityObjectToClipPos(v.vertex);
					
					return o;
				}

				float generateNoise(in float2 xy, in float seed)
				{
					return frac(tan(distance(xy * 1.618033988749895, xy) * seed) * xy.x);
				}
			
				float4 frag(v2f i) : COLOR
				{
					UNITY_SETUP_INSTANCE_ID(i);

					fixed4 texColor = _BaseMap.Sample(sampler_BaseMap, i.uv);
					fixed4 col = texColor * UNITY_ACCESS_INSTANCED_PROP(_BaseColor_arr, _BaseColor) * fixed4(1,0,0,1);
					//return float4(1, 0, 0, 1);
					return col;


					//if (col.a >= 0.5)
					//{
				 //       if (generateNoise(float2(i.vertex.xy + i.vertex.zz), 12) < 0.75)
				 //       {
				 //       	discard;
				 //       }
					//}

					// Always render opaque in the end
					//col.a = 1.0;

					//if (!IsGammaSpace())
					//{
					//	col.r = LinearToGammaSpaceExact(col.r);
					//	col.g = LinearToGammaSpaceExact(col.g);
					//	col.b = LinearToGammaSpaceExact(col.b);
					//}
					
					//return col;
				}
			ENDCG
		}
	}
}