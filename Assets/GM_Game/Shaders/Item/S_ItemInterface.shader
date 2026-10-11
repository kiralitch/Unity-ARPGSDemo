Shader "Custom/ShinyPickup"
{
    Properties
    {
        _MainTex ("基础贴图", 2D) = "white" {} // 物体的基础纹理
        _GlowColor ("发光颜色", Color) = (1, 0.8, 0.3, 1) // 金色的边缘光
        _GlowIntensity ("发光强度", Range(0, 10)) = 3 // 强度，配合Bloom效果更佳
        _RimPower ("边缘范围", Range(0.5, 8)) = 2 // 值越小，边缘光越宽
        _PulseSpeed ("闪烁速度", Range(0, 10)) = 2 // 呼吸闪烁的快慢
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // 告诉Unity我们使用自定义的 surf 函数，并基于 Lambert 光照模型
        #pragma surface surf Lambert

        sampler2D _MainTex;
        fixed4 _GlowColor;
        float _GlowIntensity;
        float _RimPower;
        float _PulseSpeed;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir; // 这是相机到物体的方向，边缘光的核心
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            // 基础颜色
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;
            o.Alpha = c.a;

            // 计算边缘光强度
            // 点乘 viewDir 和法线，结果范围在[-1,1]，越接近边缘越趋向0
            // 用 1 - 这个值，就能让边缘处接近1，中心接近0
            float rim = 1.0 - saturate(dot(IN.viewDir, o.Normal));
            // 用 _RimPower 的幂次来精确控制边缘光的宽度和衰减
            rim = pow(rim, _RimPower);

            // 计算脉冲闪烁
            // 用正弦波让亮度在 _GlowIntensity 的 50% 到 100% 之间循环
            float pulse = (sin(_Time.y * _PulseSpeed) * 0.5 + 0.5) * 0.5 + 0.5;

            // 组合发光
            // 最终自发光 = 发光颜色 * 强度 * 边缘光 * 脉冲
            o.Emission = _GlowColor.rgb * _GlowIntensity * rim * pulse;
        }
        ENDCG
    }
    FallBack "Diffuse"
}