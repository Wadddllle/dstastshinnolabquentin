//using UnityEngine;

///// <summary>
///// Simple visualizer for TSDF volume using ray marching
///// Useful for debugging before implementing marching cubes
///// </summary>
//[RequireComponent(typeof(TSDFVolumeManager))]
//public class TSDFVisualizer : MonoBehaviour
//{
//    [Header("Visualization")]
//    [SerializeField] private Material rayMarchMaterial;
//    [SerializeField] private bool enableVisualization = true;
//    [SerializeField] private float isoValue = 0.0f; // Surface at 0
//    [SerializeField] private int maxRaySteps = 128;
//    [SerializeField] private Color surfaceColor = Color.white;

//    private TSDFVolumeManager volumeManager;
//    private MeshRenderer cubeRenderer;
//    private GameObject visualizationCube;

//    // Shader property IDs
//    private static readonly int TSDFVolumeID = Shader.PropertyToID("_TSDFVolume");
//    private static readonly int VolumeOriginID = Shader.PropertyToID("_VolumeOrigin");
//    private static readonly int VolumeSizeID = Shader.PropertyToID("_VolumeSize");
//    private static readonly int IsoValueID = Shader.PropertyToID("_IsoValue");
//    private static readonly int MaxStepsID = Shader.PropertyToID("_MaxSteps");
//    private static readonly int SurfaceColorID = Shader.PropertyToID("_SurfaceColor");

//    private void Start()
//    {
//        volumeManager = GetComponent<TSDFVolumeManager>();

//        if (rayMarchMaterial == null)
//        {
//            Debug.LogWarning("Ray March Material not assigned. Creating default material.");
//            CreateDefaultMaterial();
//        }

//        CreateVisualizationCube();
//    }

//    private void CreateDefaultMaterial()
//    {
//        // Create a simple transparent material so you can see through it
//        Shader shader = Shader.Find("Unlit/Transparent");
//        if (shader == null)
//            shader = Shader.Find("Unlit/Color");

//        rayMarchMaterial = new Material(shader);
//        rayMarchMaterial.color = new Color(0.0f, 1.0f, 1.0f, 0.3f); // Cyan, 30% opacity

//        Debug.LogWarning("Using default transparent material. For proper visualization, assign a ray march shader.");
//    }

//    private void CreateVisualizationCube()
//    {
//        // Create a cube that represents the volume bounds
//        visualizationCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
//        visualizationCube.name = "TSDF_Visualization";
//        visualizationCube.transform.SetParent(transform);
//        visualizationCube.transform.localPosition = Vector3.zero;
//        visualizationCube.transform.localRotation = Quaternion.identity;
//        visualizationCube.transform.localScale = volumeManager.GetVolumeSize();

//        // CRITICAL: Remove collider to prevent physics interactions!
//        var collider = visualizationCube.GetComponent<Collider>();
//        if (collider != null)
//        {
//            DestroyImmediate(collider); // Use DestroyImmediate to remove immediately
//        }

//        // Put on a layer that doesn't collide with anything (optional but recommended)
//        visualizationCube.layer = LayerMask.NameToLayer("Ignore Raycast");

//        // Setup renderer
//        cubeRenderer = visualizationCube.GetComponent<MeshRenderer>();
//        cubeRenderer.material = rayMarchMaterial;

//        // Disable by default
//        visualizationCube.SetActive(enableVisualization);
//    }

//    private void Update()
//    {
//        if (visualizationCube != null)
//            visualizationCube.SetActive(enableVisualization);

//        if (!enableVisualization || cubeRenderer == null)
//            return;

//        UpdateVisualizationMaterial();
//    }

//    private void UpdateVisualizationMaterial()
//    {
//        RenderTexture tsdfVolume = volumeManager.GetTSDFVolume();
//        if (tsdfVolume == null)
//            return;

//        Material mat = cubeRenderer.material;

//        mat.SetTexture(TSDFVolumeID, tsdfVolume);
//        mat.SetVector(VolumeOriginID, volumeManager.GetVolumeOrigin());
//        mat.SetVector(VolumeSizeID, volumeManager.GetVolumeSize());
//        mat.SetFloat(IsoValueID, isoValue);
//        mat.SetInt(MaxStepsID, maxRaySteps);
//        mat.SetColor(SurfaceColorID, surfaceColor);
//    }

//    private void OnDestroy()
//    {
//        if (visualizationCube != null)
//            Destroy(visualizationCube);

//        if (rayMarchMaterial != null)
//            Destroy(rayMarchMaterial);
//    }
//}