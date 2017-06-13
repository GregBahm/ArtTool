Shader "Unlit/Wireframe"
{
	Properties
	{
		_LineColor("Line Color", Color) = (1,1,1,1)
		_FillColor("Fill Color", Color) = (0,0,0,0)
		_OccludedLineColor("Occluded Line Color", Color) = (1,1,1,1)
		_OccludedFillColor("Occluded Fill Color", Color) = (0,0,0,0)
		_WireThickness("Wire Thickness", Float) = 1
		_WireSharpness("Wire Sharpness", Float) = 1
		_SpecColor("Spec Color", Color) = (1,1,1,1)
		_OccludedSpecColor("Occluded Spec Color", Color) = (1,1,1,1)
		_SpecPower("Spec Power", Float) = 1
	}
	SubShader
	{
		// Main Pass
		Pass
		{
			
			Cull Off
			ZWrite Off
			Blend One One
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			float _WireThickness;
			float _WireSharpness;
			uniform fixed4 _LineColor;
			uniform fixed4 _FillColor;
			uniform fixed4 _SpecColor;
			float _SpecPower;

			struct appdata
			{
				float4 vertex : POSITION;
			};
			struct v2g
			{
				float4 baseVert : TEXCOORD0;
				float4 projectionSpaceVertex : SV_POSITION;
				float4 worldSpacePosition : TEXCOORD1;
				float3 viewDir : TEXCOORD2;
			};
			struct g2f
			{
				float4 projectionSpaceVertex : SV_POSITION;
				float4 worldSpacePosition : TEXCOORD0;
				float4 dist : TEXCOORD1;
				float3 viewDir : TEXCOORD2;
				float3 normal : NORMAL;
			};
			v2g vert(appdata v)
			{
				v2g o;
				o.baseVert = v.vertex;
				o.viewDir = WorldSpaceViewDir(v.vertex);
				o.projectionSpaceVertex = UnityObjectToClipPos(v.vertex);
				o.worldSpacePosition = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}
			[maxvertexcount(3)]
			void geom(triangle v2g i[3], inout TriangleStream<g2f> triangleStream)
			{
				float3 firstEdge = i[0].baseVert - i[1].baseVert;
				float3 secondEdge = i[0].baseVert - i[2].baseVert;
				float3 normal = normalize(cross(firstEdge, secondEdge));
				normal = mul(unity_ObjectToWorld, float4(normal, 0)).xyz;

				float2 p0 = i[0].projectionSpaceVertex.xy / i[0].projectionSpaceVertex.w;
				float2 p1 = i[1].projectionSpaceVertex.xy / i[1].projectionSpaceVertex.w;
				float2 p2 = i[2].projectionSpaceVertex.xy / i[2].projectionSpaceVertex.w;
				float2 edge0 = p2 - p1;
				float2 edge1 = p2 - p0;
				float2 edge2 = p1 - p0;
				float area = abs(edge1.x * edge2.y - edge1.y * edge2.x);
		
				g2f o;
				o.normal = normal;
				o.worldSpacePosition = i[0].worldSpacePosition;
				o.projectionSpaceVertex = i[0].projectionSpaceVertex;
				o.dist.xyz = float3((area / length(edge0)), 0.0, 0.0) * o.projectionSpaceVertex.w;
				o.dist.w = 1.0 / o.projectionSpaceVertex.w;
				o.viewDir = i[0].viewDir;
				triangleStream.Append(o);

				o.worldSpacePosition = i[1].worldSpacePosition;
				o.projectionSpaceVertex = i[1].projectionSpaceVertex;
				o.dist.xyz = float3(0.0, (area / length(edge1)), 0.0) * o.projectionSpaceVertex.w;
				o.dist.w = 1.0 / o.projectionSpaceVertex.w;
				o.viewDir = i[1].viewDir;
				triangleStream.Append(o);

				o.worldSpacePosition = i[2].worldSpacePosition;
				o.projectionSpaceVertex = i[2].projectionSpaceVertex;
				o.dist.xyz = float3(0.0, 0.0, (area / length(edge2))) * o.projectionSpaceVertex.w;
				o.dist.w = 1.0 / o.projectionSpaceVertex.w;
				o.viewDir = i[2].viewDir;
				triangleStream.Append(o);
			}

			fixed4 frag(g2f i) : SV_Target
			{

				float4 spec = _SpecColor * pow(dot(normalize(i.normal), normalize(i.viewDir)), _SpecPower);

				float minDistanceToEdge = min(i.dist[0], min(i.dist[1], i.dist[2])) * i.dist[3];
				float key = saturate(minDistanceToEdge * _WireSharpness - _WireThickness);
				float4 wireFrame = lerp(_LineColor, _FillColor, key);
				return spec + wireFrame;
			}
			ENDCG
		}
		// Occluded Pass
		Pass
		{
			ZWrite Off
			ZTest Greater
			Blend One One
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			float _WireThickness;
			float _WireSharpness;
			uniform fixed4 _OccludedLineColor;
			uniform fixed4 _OccludedFillColor;
			uniform fixed4 _OccludedSpecColor;
			float _SpecPower;

			struct appdata
			{
				float4 vertex : POSITION;
			};
			struct v2g
			{
				float4 baseVert : TEXCOORD0;
				float4 projectionSpaceVertex : SV_POSITION;
				float4 worldSpacePosition : TEXCOORD1;
				float3 viewDir : TEXCOORD2;
			};
			struct g2f
			{
				float4 projectionSpaceVertex : SV_POSITION;
				float4 worldSpacePosition : TEXCOORD0;
				float4 dist : TEXCOORD1;
				float3 viewDir : TEXCOORD2;
				float3 normal : NORMAL;
			};
			v2g vert(appdata v)
			{
				v2g o;
				o.baseVert = v.vertex;
				o.viewDir = WorldSpaceViewDir(v.vertex);
				o.projectionSpaceVertex = UnityObjectToClipPos(v.vertex);
				o.worldSpacePosition = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}
			[maxvertexcount(3)]
			void geom(triangle v2g i[3], inout TriangleStream<g2f> triangleStream)
			{
				float3 firstEdge = i[0].baseVert - i[1].baseVert;
				float3 secondEdge = i[0].baseVert - i[2].baseVert;
				float3 normal = normalize(cross(firstEdge, secondEdge));
				normal = mul(unity_ObjectToWorld, float4(normal, 0)).xyz;

				float2 p0 = i[0].projectionSpaceVertex.xy / i[0].projectionSpaceVertex.w;
				float2 p1 = i[1].projectionSpaceVertex.xy / i[1].projectionSpaceVertex.w;
				float2 p2 = i[2].projectionSpaceVertex.xy / i[2].projectionSpaceVertex.w;
				float2 edge0 = p2 - p1;
				float2 edge1 = p2 - p0;
				float2 edge2 = p1 - p0;
				float area = abs(edge1.x * edge2.y - edge1.y * edge2.x);
		
				g2f o;
				o.normal = normal;
				o.worldSpacePosition = i[0].worldSpacePosition;
				o.projectionSpaceVertex = i[0].projectionSpaceVertex;
				o.dist.xyz = float3((area / length(edge0)), 0.0, 0.0) * o.projectionSpaceVertex.w;
				o.dist.w = 1.0 / o.projectionSpaceVertex.w;
				o.viewDir = i[0].viewDir;
				triangleStream.Append(o);

				o.worldSpacePosition = i[1].worldSpacePosition;
				o.projectionSpaceVertex = i[1].projectionSpaceVertex;
				o.dist.xyz = float3(0.0, (area / length(edge1)), 0.0) * o.projectionSpaceVertex.w;
				o.dist.w = 1.0 / o.projectionSpaceVertex.w;
				o.viewDir = i[1].viewDir;
				triangleStream.Append(o);

				o.worldSpacePosition = i[2].worldSpacePosition;
				o.projectionSpaceVertex = i[2].projectionSpaceVertex;
				o.dist.xyz = float3(0.0, 0.0, (area / length(edge2))) * o.projectionSpaceVertex.w;
				o.dist.w = 1.0 / o.projectionSpaceVertex.w;
				o.viewDir = i[2].viewDir;
				triangleStream.Append(o);
			}

			fixed4 frag(g2f i) : SV_Target
			{

				float4 spec = _OccludedSpecColor * pow(dot(normalize(i.normal), normalize(i.viewDir)), _SpecPower);
				float minDistanceToEdge = min(i.dist[0], min(i.dist[1], i.dist[2])) * i.dist[3];
				float key = saturate(minDistanceToEdge * _WireSharpness - _WireThickness);
				float4 wireFrame = lerp(_OccludedLineColor, _OccludedFillColor, key);
				return spec + wireFrame;
			}
			ENDCG
		}
	}
}