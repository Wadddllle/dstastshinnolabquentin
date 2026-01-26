Shader "Custom/OcclusionShader"
{
    SubShader
    {
        // Render in the Geometry queue, but slightly earlier (-1) to ensure
        // it writes depth before the soldiers try to draw.
        Tags { "Queue" = "Geometry-1" "RenderType"="Opaque" }
        
        Pass
        {
            // Crucial: Turn on ZWrite so it updates the Depth Buffer
            ZWrite On
            
            // Crucial: Turn off ColorMask so it renders NOTHING (0) to the screen
            ColorMask 0 
        }
    }
}