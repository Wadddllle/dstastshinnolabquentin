Shader "Custom/MobileVRWireframe"
{
    Properties
    {
        _WireThickness ("Wire Thickness", Range(0, 5)) = 1.2
        _WireColor("Wireframe Color", Color) = (0,1,0,1)
        _BaseAlpha("Face Transparency", Range(0, 1)) = 0.0 // 0 = Fully see-through
    }

    SubShader
    {
        // 1. Queue=Transparent ensures it renders after opaque objects
        // 2. IgnoreProjector=True tells Unity not to project shadows on it
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            // 3. Turn off Face Culling so we see the wireframe even if winding is weird
            Cull Off 
            
            // 4. Standard Transparency Blending
            Blend SrcAlpha OneMinusSrcAlpha
            
            // 5. ZWrite On prevents "Spaghetti" look. 
            // It ensures you don't see wires BEHIND the object, keeping it clean.
            // If you want to see ALL wires (front and back), change to ZWrite Off.
            ZWrite On 

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            // --- VR STEREO SUPPORT ---
            #pragma multi_compile_instancing 
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR; // Barycentric coords from C# script
                
                // VR Macro
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 barycentric : TEXCOORD0;
                
                // VR Macro
                UNITY_VERTEX_OUTPUT_STEREO 
            };

            float _WireThickness;
            fixed4 _WireColor;
            float _BaseAlpha;

            v2f vert (appdata v)
            {
                v2f o;
                
                // --- VR SETUP ---
                UNITY_SETUP_INSTANCE_ID(v); 
                UNITY_INITIALIZE_OUTPUT(v2f, o); 
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); 

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.barycentric = v.color.rgb; // Pass the (1,0,0) colors
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate line width logic
                float3 unitWidth = fwidth(i.barycentric);
                float3 edge = smoothstep(float3(0,0,0), unitWidth * _WireThickness, i.barycentric);
                float minDist = min(edge.x, min(edge.y, edge.z));

                // If minDist is 0, we are on the line. If 1, we are in the middle.
                // Invert it so 1 is line, 0 is middle.
                float lineFactor = 1.0 - minDist;

                // Color Logic:
                // Start with the Wireframe Color
                fixed4 finalColor = _WireColor;

                // Alpha Logic:
                // If on line -> Use WireColor Alpha (usually 1)
                // If in middle -> Use _BaseAlpha (usually 0 for see-through)
                finalColor.a = lerp(_BaseAlpha, _WireColor.a, lineFactor);

                return finalColor;
            }
            ENDCG
        }
    }
}