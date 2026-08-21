Shader "Custom/BillboardParticles"
{
    Properties
    {
        _Size ("Size", Float) = 0.1
        _MaxSpeed ("Max Speed", Float) = 10.0
        _MinSpeed ("Min Speed", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 4.5

            #include "UnityCG.cginc"

            StructuredBuffer<float2> positions;
            StructuredBuffer<float2> velocities;
            StructuredBuffer<float> densities;

            float _Size;
            float _MaxSpeed;
            float _MinSpeed;

            struct appdata
            {
                float3 vertex : POSITION;
                float2 uv : TEXCOORD0;

                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float speed : TEXCOORD1;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;

                  float2 particlePos = positions[v.instanceID];

                 float3 worldPos = float3(
                     particlePos.x + v.vertex.x * _Size,
                     particlePos.y + v.vertex.y * _Size,
                      0.0
                  );

                 o.pos = mul(
                     UNITY_MATRIX_VP,
                     float4(worldPos, 1.0)
                 );

                  o.uv = v.uv;
                  o.speed = length(velocities[v.instanceID]);

                 return o;
                         }
            
            float3 Heatmap(float t)
            {
                t = saturate(t);

                float3 c1 = float3(0.0, 0.0, 1.0); 
                float3 c2 = float3(0.0, 1.0, 1.0); 
                float3 c3 = float3(1.0, 1.0, 0.0); 
                float3 c4 = float3(1.0, 0.0, 0.0); 

                if (t < 0.33)
                    return lerp(c1, c2, t / 0.33);
                else if (t < 0.66)
                    return lerp(c2, c3, (t - 0.33) / 0.33);
                else
                    return lerp(c3, c4, (t - 0.66) / 0.34);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Circle mask
                float2 centeredUV =
                    i.uv * 2 - 1;

                float dist =
                    dot(centeredUV, centeredUV);

                if (dist > 1)
                    discard;

                float t = (i.speed * 1.2 - _MinSpeed) / (_MaxSpeed - _MinSpeed);
                t = smoothstep(0.0, 1.0, t * 0.1);
                float3 col = Heatmap(t);
                col *= lerp(0.6, 1.5, t);
                return float4(col, 1.0);
            }

            ENDCG
        }
    }
}