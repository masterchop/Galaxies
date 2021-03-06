﻿Shader "Solar System/VertexColoredAdditive" {
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100

    Blend One One

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
        float4 color : COLOR;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
        float4 color : COLOR;
			};
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
        o.color = v.color;
				return o;
			}

      fixed4 _Color;
			
			fixed4 frag (v2f i) : SV_Target {
        return _Color * i.color;
			}
			ENDCG
		}
	}
}
