using UnityEngine;
using System.Collections;
using Anaglyph.XRTemplate;

public class TacticalGrid : MonoBehaviour
{
    public static TacticalGrid Instance;

    [Header("Visualization")]
    public Renderer FloorRenderer;
    private Texture2D _visTexture;
    private Color32[] _pixelColors; // Visual buffer
    private bool _textureNeedsUpdate = false;

    // --- NEW: Data Layer ---
    private System.Collections.BitArray _gridBits; // Logic buffer (1 bit per voxel)
    public int GridWidth { get; private set; }     // Public accessor for Recorder
    public int GridHeight { get; private set; }    // Public accessor for Recorder
    public float CellSize { get; private set; }    // Public accessor for Recorder

    // Settings
    private float _metersPerVoxel;
    private Vector3 _halfVolumeSize;

    // Colors
    private Color32 _colorSeen = new Color32(0, 255, 0, 150);
    private Color32 _colorUnseen = new Color32(0, 0, 0, 0);
    private float _lastUploadTime;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        var mapper = EnvironmentMapper.Instance;
        GridWidth = mapper.volume.width;
        GridHeight = mapper.volume.volumeDepth;
        CellSize = mapper.metersPerVoxel;
        _metersPerVoxel = mapper.metersPerVoxel;

        Vector3 volumeSize = new Vector3(GridWidth * _metersPerVoxel, 0, GridHeight * _metersPerVoxel);
        _halfVolumeSize = volumeSize / 2.0f;

        // 1. Setup Texture (Visuals)
        _visTexture = new Texture2D(GridWidth, GridHeight, TextureFormat.RGBA32, false);
        _visTexture.filterMode = FilterMode.Point;
        _pixelColors = new Color32[GridWidth * GridHeight];
        System.Array.Fill(_pixelColors, _colorUnseen);
        _visTexture.SetPixels32(_pixelColors);
        _visTexture.Apply();

        if (FloorRenderer != null) FloorRenderer.material.mainTexture = _visTexture;

        // 2. Setup BitArray (Data) - Size is total pixels
        _gridBits = new System.Collections.BitArray(GridWidth * GridHeight);
        _gridBits.SetAll(false); // Start as "Unseen"
    }

    void Update()
    {
        if (_textureNeedsUpdate && Time.time > _lastUploadTime + 0.1f)
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

            // Optimization: Only update if it was previously unseen
            if (_gridBits.Get(index) == false)
            {
                // 1. Update Data
                _gridBits.Set(index, true);

                // 2. Update Visuals
                _pixelColors[index] = _colorSeen;
                _textureNeedsUpdate = true;
            }
        }
    }

    public void ClearGrid()
    {
        System.Array.Fill(_pixelColors, _colorUnseen);
        _gridBits.SetAll(false); // Reset bits
        _textureNeedsUpdate = true;
    }

    // --- NEW: Export Function for the Recorder ---
    // Returns the grid as a raw byte array. 
    // Example: 256x256 grid = 65k bits = ~8kb array
    public byte[] GetCompressedGridData()
    {
        // BitArray.CopyTo expects an int[] or byte[] buffer.
        // We calculate bytes needed: (TotalBits + 7) / 8
        int numBytes = (_gridBits.Length + 7) / 8;
        byte[] bytes = new byte[numBytes];
        _gridBits.CopyTo(bytes, 0);
        return bytes;
    }
}