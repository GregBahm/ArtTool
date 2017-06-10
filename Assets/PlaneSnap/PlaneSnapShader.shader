Shader "Unlit/TheShader"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geo
			#pragma fragment frag
			
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2g
			{
				float4 baseVert : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			struct g2f
			{ 
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
			};
			
			v2g vert (appdata v)
			{
				v2g o;
				o.baseVert = v.vertex;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			[maxvertexcount(11)]
			void geo(triangle v2g p[3], inout TriangleStream<g2f> triStream)
			{
				float3 firstEdge = p[0].baseVert - p[1].baseVert;
				float3 secondEdge = p[0].baseVert - p[2].baseVert;
				float3 normal = normalize(cross(firstEdge, secondEdge));

				g2f o;
				o.normal = normal;
				o.vertex = p[0].vertex;
				triStream.Append(o);

				o.vertex = p[1].vertex;
				triStream.Append(o);

				o.vertex = p[2].vertex;
				triStream.Append(o);
			}
			
			fixed4 frag (g2f i) : SV_Target
			{
				return float4(i.normal / 2 + .5, 1);
			}
			ENDCG
		}
	}
}
