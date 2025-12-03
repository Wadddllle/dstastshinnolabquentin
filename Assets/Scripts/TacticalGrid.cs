using UnityEngine;
using Anaglyph.XRTemplate; // Needed to access EnvironmentMapper settings

public class TacticalGrid : MonoBehaviour
{
    public static TacticalGrid Instance; // Singleton for easy access

    [Header("Visualization")]
    public Renderer FloorRenderer; // Drag the plane/quad here
    private Texture2D _visTexture;
    private Color32[] _pixelColors;
    private bool _textureNeedsUpdate = false;

    // Grid Settings (Pulled from EnvironmentMapper)
    private int _width;
    private int _height;
    private float _metersPerVoxel;
    private Vector3 _halfVolumeSize;

    // Colors
    private Color32 _colorSeen = new Color32(0, 255, 0, 150); // Semi-transparent Green
    private Color32 _colorUnseen = new Color32(0, 0, 0, 0);   // Transparent

    private float _lastUploadTime;


    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 1. Grab settings from your existing Mapper to match math
        var mapper = EnvironmentMapper.Instance;
        _width = mapper.volume.width;
        _height = mapper.volume.volumeDepth;
        _metersPerVoxel = mapper.metersPerVoxel;

        // 2. Setup Coordinate Math Offset
        Vector3 volumeSize = new Vector3(_width * _metersPerVoxel, 0, _height * _metersPerVoxel);
        _halfVolumeSize = volumeSize / 2.0f;

        // 3. Create the Texture
        _visTexture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
        _visTexture.filterMode = FilterMode.Point; // Pixelated look

        // 4. Fill with transparent
        _pixelColors = new Color32[_width * _height];
        System.Array.Fill(_pixelColors, _colorUnseen);
        _visTexture.SetPixels32(_pixelColors);
        _visTexture.Apply();

        // 5. Apply to the Floor Mesh
        if (FloorRenderer != null)
        {
            FloorRenderer.material.mainTexture = _visTexture;
        }
    }

    void Update()
    {
        // Only upload to GPU if we painted something this frame
        if (_textureNeedsUpdate && Time.time > _lastUploadTime + 0.1f)
        {
            _visTexture.SetPixels32(_pixelColors);
            _visTexture.Apply();
            _textureNeedsUpdate = false;
            _lastUploadTime = Time.time;
        }
    }

    // --- The "Paint" Function ---
    public void MarkSeen(Vector3 worldPos)
    {
        // 1. Convert World to Grid Index
        // NOTE: We subtract transform.position in case the parent moves, 
        // but typically EnvironmentMapper is at 0,0,0.
        float localX = worldPos.x + _halfVolumeSize.x;
        float localZ = worldPos.z + _halfVolumeSize.z;

        int x = Mathf.FloorToInt(localX / _metersPerVoxel);
        int y = Mathf.FloorToInt(localZ / _metersPerVoxel); // Z becomes Y in texture

        // 2. Bounds Check
        if (x >= 0 && x < _width && y >= 0 && y < _height)
        {
            int index = y * _width + x;

            // 3. Paint Green if not already Green
            if (_pixelColors[index].g != 255)
            {
                _pixelColors[index] = _colorSeen;
                _textureNeedsUpdate = true; // Tell Update() to upload
            }
        }
    }
    // In TacticalGrid.cs

    public void ClearGrid()
    {
        // 1. Reset the data array to 0 (Unseen)
        System.Array.Fill(_pixelColors, _colorUnseen); // Revert to transparent

        // 2. Mark texture as dirty so Update() uploads the changes
        _textureNeedsUpdate = true;

        Debug.Log("Tactical Grid Cleared.");
    }
}