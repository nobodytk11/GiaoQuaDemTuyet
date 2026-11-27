// An unlit sprite shader with a soft diagonal glint that sweeps across the picture every few seconds.
// Used on coins, hearts, items, the gift and ice blocks. Written by the team for this project.
Shader "GiaoQua/SparkleSprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SparkleColor ("Glint Colour", Color) = (1,1,0.85,1)
        _SparkleStrength ("Glint Strength", Range(0,2)) = 0.9
        _SparkleWidth ("Glint Width", Range(0.02,0.5)) = 0.16
        _Period ("Seconds Between Glints", Range(0.5,10)) = 2.8
        _Spread ("Difference Between Objects", Range(0,1)) = 0.17
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; float phaseShift : TEXCOORD1; };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _SparkleColor;
            float _SparkleStrength;
            float _SparkleWidth;
            float _Period;
            float _Spread;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                float3 world = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;      // each object glints at its own moment
                OUT.phaseShift = frac(world.x * _Spread + world.y * 0.37);
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                float t = frac(_Time.y / _Period + IN.phaseShift);                   // 0..1 across one period
                float sweep = lerp(-_SparkleWidth, 2.0 + _SparkleWidth, t);          // diagonal position, 0..2 across the picture
                float d = IN.texcoord.x + IN.texcoord.y;
                float band = smoothstep(_SparkleWidth, 0.0, abs(d - sweep));
                c.rgb += _SparkleColor.rgb * band * _SparkleStrength * c.a;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
