Shader "Unlit/PreviewShader"
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
			ZWrite Off
			//ZTest Always
			Blend One One

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
				float3 viewDir : TEXCOORD1;
			};

			struct g2f
			{ 
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
				float3 viewDir : TEXCOORD1;
			};
			
			v2g vert (appdata v)
			{
				v2g o;
				o.baseVert = v.vertex;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.viewDir = WorldSpaceViewDir(v.vertex);
				return o;
			}
			[maxvertexcount(11)]
			void geo(triangle v2g p[3], inout TriangleStream<g2f> triStream)
			{
				float3 firstEdge = p[0].baseVert - p[1].baseVert;
				float3 secondEdge = p[0].baseVert - p[2].baseVert;
				float3 normal = normalize(cross(firstEdge, secondEdge));
				normal = mul(unity_ObjectToWorld, float4(normal, 0)).xyz;
				g2f o;
				o.normal = normal;
				o.vertex = p[0].vertex;
				o.viewDir = p[0].viewDir;
				triStream.Append(o);

				o.vertex = p[1].vertex;
				o.viewDir = p[1].viewDir;
				triStream.Append(o);

				o.vertex = p[2].vertex;
				o.viewDir = p[2].viewDir;
				triStream.Append(o);
			}
			
			fixed4 frag (g2f i) : SV_Target
			{
				return pow(dot(normalize(i.normal), normalize(i.viewDir)), 3) * float4(0, .3, 2, 1);
			}
			ENDCG
		}
	}
}
