
uniform float4x4 _EnvironmentDepthReprojectionMatrices[2];
uniform float4 _EnvironmentDepthZBufferParams;

// This function converts a screen coordinate (UV) and a depth value from the texture
// into a 3D point in world space.
float3 NDCToWorld(float2 uv, float depth, uint eyeIndex)
{
    // The depth texture provides a value from 0 to 1.
    // We need to convert it to NDC (Normalized Device Coordinates), which is -1 to 1.
    float depthNDC = depth * 2.0 - 1.0;

    // The projection matrix uses Z values in a non-linear way.
    // This formula converts from non-linear depth back to linear camera-space Z.
    // The params are provided by the EnvironmentDepthManager.
    float linearZ = (1.0f / (depthNDC + _EnvironmentDepthZBufferParams.y)) * _EnvironmentDepthZBufferParams.x;

    // Convert screen UVs (0 to 1) to NDC (-1 to 1)
    float2 screenNDC = uv * 2.0 - 1.0;

    // The reprojection matrix does all the hard work of converting
    // the 2D screen point + depth into a 3D world position.
    float4 hcs = float4(screenNDC, depthNDC, 1.0);
    float4 worldH = mul(_EnvironmentDepthReprojectionMatrices[eyeIndex], hcs);

    return worldH.xyz / worldH.w;
}