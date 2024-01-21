Shader "Custom/GridShader2" {
    Properties{
        _GridColor("Grid Color", Color) = (1,1,1,1)
        _GridSize("Grid Size", Range(0.1, 10.0)) = 1.0
    }
        SubShader{
            Tags { "RenderType" = "Opaque" }
            LOD 100

            CGPROGRAM
            #pragma surface surf Lambert

        // Proprietà passate allo shader
        fixed4 _GridColor;
        float _GridSize;

        struct Input {
            float2 uv_Grid;
        };

        // Funzione di surfacing
        void surf(Input IN, inout SurfaceOutput o) {
            // Calcolo delle coordinate della griglia nel sistema di coordinate del mondo
            float2 gridCoords = fmod(IN.uv_Grid, _GridSize);

            // Creazione della griglia
            float gridLines = step(0.01, gridCoords.x) * step(0.01, gridCoords.y);

            // Assegnazione del colore in base alla presenza della griglia
            o.Albedo = lerp(o.Albedo, _GridColor.rgb, gridLines);
        }
        ENDCG
    }
        FallBack "Diffuse"
}
