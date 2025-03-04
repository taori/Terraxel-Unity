Shader "Custom/Triplanar"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _SliceRange ("Slices", Range(0,16)) = 6
        _MainTextures ("Albedo Textures", 2DArray) = "white" {}
        //_BumpMaps("Normal textures", 2DArray) = "bump" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _MapScale("Tiling Scale", Float) = 1
        _BumpMap ("Normal Map", 2D) = "bump" {}

        _BumpScale("Normal Strength", Float) = 1
        [PerRendererData]_DirectionMask("Direction mask", Int) = 0
        [PerRendererData]_WorldPos("WorldPos", Vector) = (0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard vertex:vert fullforwardshadows addshadow
        #pragma multi_compile_instancing
        #pragma require 2darray
        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 4.0

        //sampler2D _MainTex;

        struct Input
        {
            float3 localCoord;
            float3 localNormal;
            float textureIndex;
        };
        struct output {
            float4 vertex : POSITION;
            float3 normal : NORMAL;
            float4 tangent : TANGENT;
            int near : COLOR;
            float textureIndex : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        half _MapScale;
        half _BumpScale;
        sampler2D _BumpMap;
        
        int _DirectionMask;
        float _SliceRange;
        float4 _WorldPos;

        UNITY_DECLARE_TEX2DARRAY(_MainTextures);
        //UNITY_DECLARE_TEX2DARRAY(_BumpMaps);

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        /*#pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_DEFINE_INSTANCED_PROP(int, _DirectionMask);
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)*/

        void vert(inout output v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);
            float4 vertPos;
            if(((v.near & _DirectionMask) == v.near)){
                vertPos = v.tangent;
            }else{
                vertPos = v.vertex;
            }
            v.vertex = vertPos;
            data.localCoord = (_WorldPos.xyz + vertPos.xyz);
            data.localNormal = v.normal.xyz;
            data.textureIndex = v.textureIndex * _SliceRange;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            //fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            // Blending factor of triplanar mapping
            float3 bf = abs(IN.localNormal);
            bf /= dot(bf, (float3)1);
        
            // Triplanar mapping
            float2 tx = IN.localCoord.yz * _MapScale;
            float2 ty = IN.localCoord.zx * _MapScale;
            float2 tz = IN.localCoord.xy * _MapScale;

            // Base color
            half4 cx = UNITY_SAMPLE_TEX2DARRAY(_MainTextures, float3(tx, IN.textureIndex)) * bf.x;
            half4 cy = UNITY_SAMPLE_TEX2DARRAY(_MainTextures, float3(ty, IN.textureIndex)) * bf.y;
            half4 cz = UNITY_SAMPLE_TEX2DARRAY(_MainTextures, float3(tz, IN.textureIndex)) * bf.z;
            half4 color = (cx + cy + cz) * _Color;
            o.Albedo = color.rgb;
            //o.Alpha = color.a;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            // Normal map
            half4 nx = tex2D(_BumpMap, tx) * bf.x;
            half4 ny = tex2D(_BumpMap, ty) * bf.y;
            half4 nz = tex2D(_BumpMap, tz) * bf.z;
            o.Normal = UnpackScaleNormal(nx + ny + nz, _BumpScale);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
