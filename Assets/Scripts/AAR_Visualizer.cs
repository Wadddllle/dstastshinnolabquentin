using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using System.Collections;

public class AAR_Visualizer : MonoBehaviour
{
    [Header("UI References")]
    public RawImage mapBackgroundImage; // Assign the Bottom Layer (White/Black Map)
    public RawImage dataOverlayImage;   // Assign the Top Layer (Green Grid)
    public RectTransform ghostAgentIcon; // Assign a small Arrow/Circle UI Image

    [Header("Data Source")]
    public string mapFileName = "Map_Snapshot.png";
    public string logFileName = "session_data.bin";

    [Header("Playback Control")]
    [Range(0f, 1f)] public float playbackProgress = 0f; // Drag this in Inspector!
    public bool showHeatmap = false;

    // --- Internal Data ---
    private Texture2D _mapTexture;
    private Texture2D _dataTexture;
    private Color32[] _dataPixels; // Reusable pixel buffer

    // Loaded Log Data
    private List<LogFrame> _frames = new List<LogFrame>();
    private int[] _heatmapAccumulator;
    private int _maxHeat = 1;

    // Grid Specs (Read from file header)
    private int _gridWidth;
    private int _gridHeight;
    private float _cellSize;

    // Struct to hold frame data in memory
    struct LogFrame
    {
        public float time;
        public Vector3 pos;
        public Quaternion rot;
        public byte[] gridBytes;
    }

    //void Start()
    ////{
    ////    LoadMapImage();
    ////    LoadLogData();

    ////    // Initial Draw
    ////    UpdateVisualization();
    //}
    public void Initialize(string specificLogFileName)
    {
        // Update the filename
        this.logFileName = specificLogFileName;

        // Start a coroutine to handle the loading with a safety buffer
        StartCoroutine(LoadSequence());
    }

    private IEnumerator LoadSequence()
    {
        // 1. Wait a tiny bit to ensure the OS has released the file handle from the Recorder
        yield return new WaitForSeconds(0.1f);

        LoadMapImage();

        // 2. Try to load the data
        LoadLogData();

        UpdateVisualization();
    }


    void Update()
    {
        // For testing, update every frame so dragging the inspector slider works live
        UpdateVisualization();
    }

    private void LoadMapImage()
    {
        string path = Path.Combine(Application.persistentDataPath, mapFileName);
        if (File.Exists(path))
        {
            byte[] bytes = File.ReadAllBytes(path);
            _mapTexture = new Texture2D(2, 2);
            _mapTexture.LoadImage(bytes); // Auto-resizes
            mapBackgroundImage.texture = _mapTexture;

            // Fix Aspect Ratio of the UI to match the texture
            float aspect = (float)_mapTexture.width / _mapTexture.height;
            mapBackgroundImage.GetComponent<AspectRatioFitter>().aspectRatio = aspect;
        }
    }

    private void LoadLogData()
    {
        string path = Path.Combine(Application.persistentDataPath, logFileName);
        Debug.Log($"[AAR] Attempting to load: {path}");
        if (!File.Exists(path))
        {
            Debug.LogError("[AAR] File does not exist!");
            return;
        }
        _frames.Clear(); // Clear old data before loading new
        using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
        {
            // 1. Read Header
            string magic = reader.ReadString();
            int version = reader.ReadInt32();
            _gridWidth = reader.ReadInt32();
            _gridHeight = reader.ReadInt32();
            _cellSize = reader.ReadSingle();

            Debug.Log($"[AAR] Header: {magic} V{version} | Grid: {_gridWidth}x{_gridHeight}");

            // Setup Textures & Buffers
            _dataTexture = new Texture2D(_gridWidth, _gridHeight, TextureFormat.RGBA32, false);
            _dataTexture.filterMode = FilterMode.Point; // Crisp pixels
            dataOverlayImage.texture = _dataTexture;

            _dataPixels = new Color32[_gridWidth * _gridHeight];
            _heatmapAccumulator = new int[_gridWidth * _gridHeight];

            // 2. Read Frames
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                try
                {
                    LogFrame frame = new LogFrame();
                    frame.time = reader.ReadSingle();
                    frame.pos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    frame.rot = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    //Quaternion originalRot = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    //Quaternion flipRot = Quaternion.Euler(0, 0, 180);
                    //frame.rot = originalRot * flipRot;

                    int byteCount = reader.ReadInt32();
                    frame.gridBytes = reader.ReadBytes(byteCount);

                    _frames.Add(frame);

                    // Pre-calculate Heatmap
                    AddToHeatmap(frame.gridBytes);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AAR] Failed to read bin file: {e.Message}");
                    break; // End of file
                }
                Debug.Log($"[AAR] Load Complete. Total Frames: {_frames.Count}");
            }
        }
        Debug.Log($"Loaded {_frames.Count} frames.");
    }

    private void AddToHeatmap(byte[] gridBytes)
    {
        // Iterate through bits in the byte array
        for (int i = 0; i < gridBytes.Length; i++)
        {
            byte b = gridBytes[i];
            for (int bit = 0; bit < 8; bit++)
            {
                // Check if bit is set
                if ((b & (1 << bit)) != 0)
                {
                    int pixelIndex = i * 8 + bit;
                    if (pixelIndex < _heatmapAccumulator.Length)
                    {
                        _heatmapAccumulator[pixelIndex]++;
                        if (_heatmapAccumulator[pixelIndex] > _maxHeat)
                            _maxHeat = _heatmapAccumulator[pixelIndex];
                    }
                }
            }
        }
    }

    private void UpdateVisualization()
    {
        if (_frames == null || _frames.Count == 0)
        {
            Debug.LogWarning("[AAR_Debug] No frames in list. Update aborted.");
            return;
        }
        if (mapBackgroundImage == null || ghostAgentIcon == null)
        {
            Debug.LogError("[AAR_Debug] UI References are NULL!");
            return;
        }

        //Debug.LogError("Update visualization happeming");
        // 1. Determine current frame index
        int frameIndex = Mathf.FloorToInt(playbackProgress * (_frames.Count - 1));
        LogFrame currentFrame = _frames[frameIndex];

        // 2. Update Ghost Agent Icon
        // Map World Pos -> UV Coordinate (0 to 1)
        // Note: This requires knowing the Map's World Bounds. 
        // For now, assume map center is (0,0,0) and size is Width * CellSize.
        float worldWidth = _gridWidth * _cellSize;
        float worldHeight = _gridHeight * _cellSize;

        Vector2 uvPos = new Vector2(
            (currentFrame.pos.x / worldWidth) + 0.5f,
            (currentFrame.pos.z / worldHeight) + 0.5f
        );

        // Position the UI Element on the Rect
        RectTransform mapRect = mapBackgroundImage.GetComponent<RectTransform>();
        ghostAgentIcon.anchoredPosition = new Vector2(
            (uvPos.x - 0.5f) * mapRect.rect.width,
            (uvPos.y - 0.5f) * mapRect.rect.height
        );

        // Rotate Icon (Z-up in Unity UI corresponds to Y-rotation in World)
        Vector3 rotEuler = currentFrame.rot.eulerAngles;
        ghostAgentIcon.localRotation = Quaternion.Euler(0, 0, -rotEuler.y); // Negative because UI Y is flipped relative to World Y

        // 3. Update Texture (Heatmap vs Playback)
        if (showHeatmap)
        {
            DrawHeatmap();
        }
        else
        {
            DrawPlayback(currentFrame.gridBytes);
        }

        _dataTexture.SetPixels32(_dataPixels);
        _dataTexture.Apply();
    }

    private void DrawPlayback(byte[] gridBytes)
    {
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 seen = new Color32(0, 255, 0, 150); // Semi-transparent Green

        for (int i = 0; i < _dataPixels.Length; i++)
        {
            // Calculate byte index and bit offset
            int byteIndex = i / 8;
            int bitOffset = i % 8;

            bool isSeen = false;
            if (byteIndex < gridBytes.Length)
            {
                isSeen = (gridBytes[byteIndex] & (1 << bitOffset)) != 0;
            }

            _dataPixels[i] = isSeen ? seen : clear;
        }
    }

    private void DrawHeatmap()
    {
        Color32 clear = new Color32(0, 0, 0, 0);

        for (int i = 0; i < _heatmapAccumulator.Length; i++)
        {
            int val = _heatmapAccumulator[i];
            if (val == 0)
            {
                _dataPixels[i] = clear;
            }
            else
            {
                float t = (float)val / _maxHeat;
                // Simple Blue -> Red Gradient
                _dataPixels[i] = Color32.Lerp(new Color32(0, 0, 255, 150), new Color32(255, 0, 0, 150), t);
            }
        }
    }
}