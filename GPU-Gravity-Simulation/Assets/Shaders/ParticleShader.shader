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

            StructuredBuffer<float3> positions;
            StructuredBuffer<float3> velocities;
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

                float3 particlePos =
                    positions[v.instanceID];

                // Camera right vector
                float3 right =
                    UNITY_MATRIX_V[0].xyz;

                // Camera up vector
                float3 up =
                    UNITY_MATRIX_V[1].xyz;

                float3 worldPos =
                    particlePos
                    + right * v.vertex.x * _Size
                    + up    * v.vertex.y * _Size;

                o.pos = mul(
                    UNITY_MATRIX_VP,
                    float4(worldPos, 1)
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