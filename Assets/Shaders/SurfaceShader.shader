Shader "Custom/NewSurfaceShader"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,0.5) // Aggiunta di un canale alpha (trasparenza)
        _MainTex("Base (RGB)", 2D) = "white" { }
    }
        SubShader
    {
        Tags { "RenderType" = "Transparent" } // Indica che il materiale è trasparente
        LOD 100

        CGPROGRAM
        #pragma surface surf Lambert alpha

        struct Input
        {
            float2 uv_MainTex;
        };

        fixed4 _Color;
        sampler2D _MainTex;

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
        FallBack "Diffuse"
}
