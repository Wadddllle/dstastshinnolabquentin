// This using statement is required to access the modern Meta SDK classes.
using Meta.XR.EnvironmentDepth;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

// The original namespace is kept for compatibility with the other Lasertag scripts.
namespace Anaglyph.XRTemplate.DepthKit
{
    [DefaultExecutionOrder(-40)]
    public class DepthKitDriver : MonoBehaviour
    {
        public static DepthKitDriver Instance { get; private set; }

        // --- MODIFICATION START ---
        // We need references to the modern SDK components.
        private EnvironmentDepthManager _depthManager;
        private OVRCameraRig _cameraRig;
        // --- MODIFICATION END ---

        private Matrix4x4[] agDepthProj = new Matrix4x4[2];
        private Matrix4x4[] agDepthProjInv = new Matrix4x4[2];
        private Matrix4x4[] agDepthView = new Matrix4x4[2];
        private Matrix4x4[] agDepthViewInv = new Matrix4x4[2];

        private static int ID(string str) => Shader.PropertyToID(str);

        public static readonly int Meta_PreprocessedEnvironmentDepthTexture_ID = ID("_PreprocessedEnvironmentDepthTexture");
        public static readonly int Meta_EnvironmentDepthTexture_ID = ID("_EnvironmentDepthTexture");
        public static readonly int Meta_EnvironmentDepthZBufferParams_ID = ID("_EnvironmentDepthZBufferParams");
        public static readonly int agDepthTex_ID = ID("agDepthTex");
        public static readonly int agDepthEdgeTex_ID = ID("agDepthEdgeTex");
        public static readonly int agDepthNormTex_ID = ID("agDepthNormalTex");
        public static readonly int agDepthNormalTexRW_ID = ID("agDepthNormalTexRW");
        public static readonly int agDepthZParams_ID = ID("agDepthZParams");
        public static readonly int agDepthProj_ID = ID(nameof(agDepthProj));
        public static readonly int agDepthProjInv_ID = ID(nameof(agDepthProjInv));
        public static readonly int agDepthView_ID = ID(nameof(agDepthView));
        public static readonly int agDepthViewInv_ID = ID(nameof(agDepthViewInv));
        public static readonly int agDepthTexSize = ID(nameof(agDepthTexSize));

        public static bool DepthAvailable { get; private set; }

        [SerializeField] private ComputeShader depthNormalCompute = null;
        private ComputeKernel normKernel;
        [SerializeField] private RenderTexture normTex = null;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // --- MODIFICATION START ---
            // At the start, we find the necessary components in the scene.
            _depthManager = FindFirstObjectByType<EnvironmentDepthManager>();
            if (_depthManager == null)
                Debug.LogError("DepthKitDriver could not find an active EnvironmentDepthManager!", this);

            _cameraRig = FindFirstObjectByType<OVRCameraRig>();
            if (_cameraRig == null)
                Debug.LogError("DepthKitDriver could not find an OVRCameraRig!", this);
            // --- MODIFICATION END ---

            normKernel = new(depthNormalCompute, "DepthNorm");
        }

        private void Update()
        {
            UpdateCurrentRenderingState();
        }

        public void UpdateCurrentRenderingState()
        {
            Texture depthTex = Shader.GetGlobalTexture(Meta_EnvironmentDepthTexture_ID);
            DepthAvailable = depthTex != null;
            if (!DepthAvailable)
                return;

            // Add a safety check to prevent errors if the components aren't found.
            if (_depthManager == null || _cameraRig == null) return;

            Shader.SetGlobalVector(agDepthTexSize, new Vector2(depthTex.width, depthTex.height));
            Shader.SetGlobalTexture(agDepthTex_ID, depthTex);
            Shader.SetGlobalTexture(agDepthEdgeTex_ID, Shader.GetGlobalTexture(Meta_PreprocessedEnvironmentDepthTexture_ID));
            Shader.SetGlobalVector(agDepthZParams_ID, Shader.GetGlobalVector(Meta_EnvironmentDepthZBufferParams_ID));

            int w = depthTex.width;
            int h = depthTex.height;

            if (normTex == null)
            {
                normTex = new(w, h, 0, GraphicsFormat.R8G8B8A8_SNorm, 1);
                normTex.dimension = TextureDimension.Tex2DArray;
                normTex.volumeDepth = 2;
                normTex.useMipMap = false;
                normTex.enableRandomWrite = true;
                normTex.Create();
            }

            normKernel.Set(agDepthTex_ID, depthTex);
            normKernel.Set(agDepthNormalTexRW_ID, normTex);
            normKernel.DispatchGroups(normTex);

            Shader.SetGlobalTexture(agDepthNormTex_ID, normTex);

            for (int i = 0; i < agDepthProj.Length; i++)
            {
                // --- MODIFICATION START: This is the core fix. --- -
                // The OLD way, which caused an error:
                // var desc = Utils.GetEnvironmentDepthFrameDesc(i);

                // The NEW way: We access the public 'frameDescriptors' array that we exposed
                // in the EnvironmentDepthManager.
                var desc = _depthManager.frameDescriptors[i];
                // --- MODIFICATION END ---

                agDepthProj[i] = CalculateDepthProjMatrix(desc);
                agDepthProjInv[i] = Matrix4x4.Inverse(agDepthProj[i]);

                // --- MODIFICATION START: Another core fix. ---
                // The OLD way, which used an obsolete rig name:
                // agDepthView[i] = CalculateDepthViewMatrix(desc) * MainXRRig.TrackingSpace.worldToLocalMatrix;

                // The NEW way: We use the OVRCameraRig reference we found in Start().
                agDepthView[i] = CalculateDepthViewMatrix(desc) * _cameraRig.trackingSpace.worldToLocalMatrix;
                // --- MODIFICATION END ---

                agDepthViewInv[i] = Matrix4x4.Inverse(agDepthView[i]);
            }

            Shader.SetGlobalMatrixArray(nameof(agDepthProj), agDepthProj);
            Shader.SetGlobalMatrixArray(nameof(agDepthProjInv), agDepthProjInv);
            Shader.SetGlobalMatrixArray(nameof(agDepthView), agDepthView);
            Shader.SetGlobalMatrixArray(nameof(agDepthViewInv), agDepthViewInv);
        }

        private static readonly Vector3 _scalingVector3 = new(1, 1, -1);

        // --- MODIFICATION START: Update method signature and logic. ---
        // The method signature is changed from 'Utils.EnvironmentDepthFrameDesc' to the modern 'DepthFrameDesc'.
        private static Matrix4x4 CalculateDepthProjMatrix(DepthFrameDesc frameDesc)
        {
            // The modern 'DepthFrameDesc' provides pre-calculated tangents of the FOV angles,
            // which makes this calculation slightly different but more direct than the old way.
            float left = frameDesc.fovLeftAngleTangent;
            float right = frameDesc.fovRightAngleTangent;
            float bottom = frameDesc.fovDownAngleTangent;
            float top = frameDesc.fovTopAngleTangent;
            float near = frameDesc.nearZ;
            float far = frameDesc.farZ;

            float x = 2.0F / (right + left);
            float y = 2.0F / (top + bottom);
            float a = (right - left) / (right + left);
            float b = (top - bottom) / (top + bottom);
            float c;
            float d;

            if (float.IsInfinity(far))
            {
                c = -1.0F;
                d = -2.0f * near;
            }
            else
            {
                c = -(far + near) / (far - near);
                d = -(2.0F * far * near) / (far - near);
            }
            float e = -1.0F;
            Matrix4x4 m = new Matrix4x4
            {
                m00 = x,
                m01 = 0,
                m02 = a,
                m03 = 0,
                m10 = 0,
                m11 = y,
                m12 = b,
                m13 = 0,
                m20 = 0,
                m21 = 0,
                m22 = c,
                m23 = d,
                m30 = 0,
                m31 = 0,
                m32 = e,
                m33 = 0
            };
            return m;
        }
        // --- MODIFICATION END ---


        // --- MODIFICATION START: Update method signature and logic. ---
        // The method signature is also changed to use the modern 'DepthFrameDesc'.
        private static Matrix4x4 CalculateDepthViewMatrix(DepthFrameDesc frameDesc)
        {
            var createRotation = frameDesc.createPoseRotation;

            // The modern 'DepthFrameDesc' provides 'createPoseRotation' as a Quaternion directly,
            // so we no longer need to reconstruct it from a Vector4 like the old script did.
            var depthOrientation = createRotation;

            var viewMatrix = Matrix4x4.TRS(frameDesc.createPoseLocation, depthOrientation,
                _scalingVector3).inverse;
            return viewMatrix;
        }
        // --- MODIFICATION END ---
    }
}