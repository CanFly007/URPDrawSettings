Shader "Hidden/CharacterColorInViewOfMonster" 
{
	Properties
	{
		[MainColor] _PlayerColor("Color", Color) = (1,1,1,1)
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
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				UNITY_INSTANCING_BUFFER_START(Props)
					UNITY_DEFINE_INSTANCED_PROP(fixed4, _PlayerColor)
				UNITY_INSTANCING_BUFFER_END(Props)

				v2f vert(a2v v)
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_TRANSFER_INSTANCE_ID(v, o);
					o.vertex = UnityObjectToClipPos(v.vertex);					
					return o;
				}
			
				float4 frag(v2f i) : COLOR
				{
					UNITY_SETUP_INSTANCE_ID(i);
					float4 col = UNITY_ACCESS_INSTANCED_PROP(Props, _PlayerColor);
					return col;
				}
			ENDCG
		}
	}
}