Shader "Custom/DiagonalShader"
{
    Properties
    {
        _Color("Main Color", Color) = (1, 1, 1, 1)
        _MainTex("Base (RGB)", 2D) = "white" { }
        _Tiling("Tiling", Range(1, 10)) = 1
        _DiagonalCount("Diagonal Count", Range(2, 8)) = 4
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 100

            CGPROGRAM
            #pragma surface surf Lambert

            struct Input
            {
                float2 uv_MainTex;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float _Tiling;
            float _DiagonalCount;

            void surf(Input IN, inout SurfaceOutput o)
            {
                // Calcola le coordinate UV con il tiling
                float2 uv = _Tiling * IN.uv_MainTex;

                // Calcola l'indice della diagonale basato sulle coordinate UV
                float diagonalIndex = floor(uv.x + uv.y) * _DiagonalCount;

                // Calcola la differenza tra le coordinate x e y per creare il motivo diagonale
                float diagonalOffset = frac(diagonalIndex);

                // Utilizza la texture e il colore principale
                fixed4 c = tex2D(_MainTex, uv) * _Color;

                // Modifica il colore basato sulla posizione nella diagonale
                c.rgb *= (1 - diagonalOffset);

                o.Albedo = c.rgb;
                o.Alpha = c.a;
            }
            ENDCG
        }
            FallBack "Diffuse"
}