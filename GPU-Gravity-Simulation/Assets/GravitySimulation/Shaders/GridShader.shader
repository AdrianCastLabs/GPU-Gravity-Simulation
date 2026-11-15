Shader "Custom/AdaptiveGridShader"
{
    Properties
    {
        [MainColor] _BaseColor("Background Color", Color) = (0.009, 0.009, 0.009, 1)
        _GridColor("Minor Grid Color", Color) = (0.025, 0.025, 0.025, 1)
        _MajorColor("Major Grid Color", Color) = (0.065, 0.065, 0.065, 1)
        _AxisColor("Origin Axes Color", Color) = (0.55, 0.055, 0.16, 1)

        [Enum(UV,0,ObjectXZ,1,ObjectXY,2,WorldXZ,3,WorldXY,4)]
        _CoordinateSpace("Coordinate Space", Float) = 1

        _CellSize("Base Cell Size / Spacing (X, Y)", Vector) = (1, 1, 0, 0)
        _Offset("Grid Origin Offset (X, Y)", Vector) = (0, 0, 0, 0)

        _LineThickness("Minor Thickness (Pixels)", Range(0, 5)) = 0.8
        _MajorThickness("Major Thickness (Pixels)", Range(0, 5)) = 1.2
        _AxisThickness("Axis Thickness (Pixels)", Range(0, 5)) = 1.5

        [Toggle] _ShowMajorGrid("Show Major Grid", Float) = 1
        _MajorEvery("Major Line Every N Cells", Range(2, 20)) = 5

        [Toggle] _ShowAxes("Show Origin Axes", Float) = 1

        _TargetCellPixels("Target Cell Size In Pixels", Range(16, 256)) = 64
        _GridScaleFactor("Dynamic Scale Factor", Range(2, 10)) = 10
        _FadeRange("Grid Fade Range", Range(0.01, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "AdaptiveProceduralGrid"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 gridPosition : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _GridColor;
                half4 _MajorColor;
                half4 _AxisColor;

                float _CoordinateSpace;
                float4 _CellSize;
                float4 _Offset;

                float _LineThickness;
                float _MajorThickness;
                float _AxisThickness;

                float _ShowMajorGrid;
                float _MajorEvery;
                float _ShowAxes;

                float _TargetCellPixels;
                float _GridScaleFactor;
                float _FadeRange;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(positionWS);

                if (_CoordinateSpace < 0.5)
                {
                    OUT.gridPosition = IN.uv;
                }
                else if (_CoordinateSpace < 1.5)
                {
                    OUT.gridPosition = IN.positionOS.xz;
                }
                else if (_CoordinateSpace < 2.5)
                {
                    OUT.gridPosition = IN.positionOS.xy;
                }
                else if (_CoordinateSpace < 3.5)
                {
                    OUT.gridPosition = positionWS.xz;
                }
                else
                {
                    OUT.gridPosition = positionWS.xy;
                }

                return OUT;
            }

            float2 LineCoverage(float2 distancePixels, float thickness)
            {
                float width = max(thickness, 0.0);

                return saturate(width * 0.5 + 0.5 - distancePixels)
                    * saturate(width);
            }

            float GridMask(
                float2 position,
                float2 spacing,
                float2 unitsPerPixel,
                float thickness)
            {
                spacing = max(spacing, float2(0.000001, 0.000001));

                float2 distanceToLine =
                    abs(frac(position / spacing + 0.5) - 0.5) * spacing;

                float2 coverage = LineCoverage(
                    distanceToLine / unitsPerPixel,
                    thickness);

                return max(coverage.x, coverage.y);
            }

            float GridVisibilityFromPixelSize(
                float2 spacing,
                float2 unitsPerPixel,
                float targetPixels,
                float fadeRange)
            {
                float2 cellPixels = spacing / unitsPerPixel;
                float averageCellPixels = max((cellPixels.x + cellPixels.y) * 0.5, 0.000001);

                float ratio = averageCellPixels / max(targetPixels, 0.000001);

                float fadeIn = smoothstep(0.25, 0.25 + fadeRange, ratio);
                float fadeOut = 1.0 - smoothstep(1.0 - fadeRange, 1.0, ratio);

                return saturate(fadeIn * fadeOut);
            }

            float AdaptiveGridMask(
                float2 position,
                float2 baseSpacing,
                float2 unitsPerPixel,
                float thickness,
                float targetPixels,
                float scaleFactor,
                float fadeRange)
            {
                scaleFactor = max(scaleFactor, 1.0001);

                float averageUnitsPerPixel =
                    max((unitsPerPixel.x + unitsPerPixel.y) * 0.5, 0.000001);

                float averageBaseSpacing =
                    max((baseSpacing.x + baseSpacing.y) * 0.5, 0.000001);

                float desiredWorldSpacing =
                    averageUnitsPerPixel * max(targetPixels, 1.0);

                float scaleLevel =
                    floor(log(max(desiredWorldSpacing / averageBaseSpacing, 0.000001)) / log(scaleFactor));

                float spacingA = pow(scaleFactor, scaleLevel);
                float spacingB = spacingA * scaleFactor;
                float spacingC = spacingA / scaleFactor;

                float2 gridSpacingA = baseSpacing * spacingA;
                float2 gridSpacingB = baseSpacing * spacingB;
                float2 gridSpacingC = baseSpacing * spacingC;

                float maskA = GridMask(position, gridSpacingA, unitsPerPixel, thickness);
                float maskB = GridMask(position, gridSpacingB, unitsPerPixel, thickness);
                float maskC = GridMask(position, gridSpacingC, unitsPerPixel, thickness);

                float visibilityA =
                    GridVisibilityFromPixelSize(gridSpacingA, unitsPerPixel, targetPixels, fadeRange);

                float visibilityB =
                    GridVisibilityFromPixelSize(gridSpacingB, unitsPerPixel, targetPixels, fadeRange);

                float visibilityC =
                    GridVisibilityFromPixelSize(gridSpacingC, unitsPerPixel, targetPixels, fadeRange);

                return saturate(
                    maskA * visibilityA +
                    maskB * visibilityB +
                    maskC * visibilityC);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 position = IN.gridPosition - _Offset.xy;
                float2 baseSpacing = max(abs(_CellSize.xy), float2(0.0001, 0.0001));

                float2 dx = ddx(position);
                float2 dy = ddy(position);

                float2 unitsPerPixel = max(
                    sqrt(dx * dx + dy * dy),
                    float2(0.000001, 0.000001));

                float minor = AdaptiveGridMask(
                    position,
                    baseSpacing,
                    unitsPerPixel,
                    _LineThickness,
                    _TargetCellPixels,
                    _GridScaleFactor,
                    _FadeRange);

                float majorEvery = max(2.0, floor(_MajorEvery + 0.5));

                float major = AdaptiveGridMask(
                    position,
                    baseSpacing * majorEvery,
                    unitsPerPixel,
                    _MajorThickness,
                    _TargetCellPixels,
                    _GridScaleFactor,
                    _FadeRange);

                major *= step(0.5, _ShowMajorGrid);

                float2 axisCoverage = LineCoverage(
                    abs(position) / unitsPerPixel,
                    _AxisThickness);

                float axes = max(axisCoverage.x, axisCoverage.y);
                axes *= step(0.5, _ShowAxes);

                half3 color = _BaseColor.rgb;

                color = lerp(color, _GridColor.rgb, minor);
                color = lerp(color, _MajorColor.rgb, major);
                color = lerp(color, _AxisColor.rgb, axes);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}