using UnityEngine;
using System.Collections;
using Anaglyph.XRTemplate;

public class TacticalGrid : MonoBehaviour
{
    public static TacticalGrid Instance;

    [Header("Settings")]
    public bool ShowDebugVisualization = true; // New Checkmark

    [Header("Visualization")]
    public Renderer FloorRenderer;
    private Texture2D _visTexture;
    private Color32[] _pixelColors;
    private bool _textureNeedsUpdate = false;

    // --- Data Layer ---
    private System.Collections.BitArray _gridBits;
    public int GridWidth { get; private set; }
    public int GridHeight { get; private set; }
    public float CellSize { get; private set; }

    private float _metersPerVoxel;
    private Vector3 _halfVolumeSize;

    private Color32 _colorSeen = new Color32(0, 255, 0, 150);
    private Color32 _colorUnseen = new Color32(0, 0, 0, 0);
    private float _lastUploadTime;
    public bool IsInitialized { get; private set; } = false;

    void Awake()
    {
        Instance = this;

        // Move the data structure setup here so it's ready immediately
        var mapper = EnvironmentMapper.Instance;
        if (mapper != null)
        {
            GridWidth = mapper.volume.width;
            GridHeight = mapper.volume.volumeDepth;
            _metersPerVoxel = mapper.metersPerVoxel;
            CellSize = mapper.metersPerVoxel;

            // Initialize the BitArray immediately
            _gridBits = new System.Collections.BitArray(GridWidth * GridHeight);
            _gridBits.SetAll(false);
        }
    }

    void Start()
    {
        // Keep visualization setup here
        var mapper = EnvironmentMapper.Instance;
        Vector3 volumeSize = new Vector3(GridWidth * _metersPerVoxel, 0, GridHeight * _metersPerVoxel);
        _halfVolumeSize = volumeSize / 2.0f;

        _visTexture = new Texture2D(GridWidth, GridHeight, TextureFormat.RGBA32, false);
        _visTexture.filterMode = FilterMode.Point;
        _pixelColors = new Color32[GridWidth * GridHeight];
        System.Array.Fill(_pixelColors, _colorUnseen);
        _visTexture.SetPixels32(_pixelColors);
        _visTexture.Apply();

        if (FloorRenderer != null)
        {
            FloorRenderer.material.mainTexture = _visTexture;
            FloorRenderer.enabled = ShowDebugVisualization;
        }
        IsInitialized = true;
    }

    void Update()
    {
        // Toggle the renderer visibility based on the checkmark
        if (FloorRenderer != null && FloorRenderer.enabled != ShowDebugVisualization)
        {
            FloorRenderer.enabled = ShowDebugVisualization;
        }

        // Only upload to GPU if we are in debug mode and need an update
        if (ShowDebugVisualization && _textureNeedsUpdate && Time.time > _lastUploadTime + 0.1f)
        {
            _visTexture.SetPixels32(_pixelColors);
            _visTexture.Apply();
            _textureNeedsUpdate = false;
            _lastUploadTime = Time.time;
        }
    }

    public void MarkSeen(Vector3 worldPos)
    {
        float localX = worldPos.x + _halfVolumeSize.x;
        float localZ = worldPos.z + _halfVolumeSize.z;

        int x = Mathf.FloorToInt(localX / _metersPerVoxel);
        int y = Mathf.FloorToInt(localZ / _metersPerVoxel);

        if (x >= 0 && x < GridWidth && y >= 0 && y < GridHeight)
        {
            int index = y * GridWidth + x;

            if (_gridBits.Get(index) == false)
            {
                // 1. Always Update Data
                _gridBits.Set(index, true);

                // 2. Only update visual buffer if debug is on
                if (ShowDebugVisualization)
                {
                    _pixelColors[index] = _colorSeen;
                    _textureNeedsUpdate = true;
                }
            }
        }
    }

    public void ClearGrid()
    {
        _gridBits.SetAll(false);

        if (ShowDebugVisualization)
        {
            System.Array.Fill(_pixelColors, _colorUnseen);
            _textureNeedsUpdate = true;
        }
    }

    public byte[] GetCompressedGridData()
    {
        int numBytes = (_gridBits.Length + 7) / 8;
        byte[] bytes = new byte[numBytes];
        _gridBits.CopyTo(bytes, 0);
        return bytes;
    }
}