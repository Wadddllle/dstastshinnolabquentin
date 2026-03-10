using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using Unity.VisualScripting;

public class AAR_Visualizer : MonoBehaviour
{
    [Header("UI References")]
    public RawImage mapBackgroundImage; // The Room Snapshot
    public RawImage dataOverlayImage;   // The 100m Green Grid
    public RectTransform ghostAgentIcon;
    public Slider timelineSlider;

    [Header("Data Source")]
    public string logFileName;
    public AAR_ReportCard reportCardLink; // DRAG REPORT CARD HERE IN INSPECTOR


    [Header("Playback Control")]
    [Range(0f, 1f)] public float playbackProgress = 0f;
    public bool showHeatmap = false;
    public bool isPlaying = false;
    public float playbackSpeed = 0.5f;

    // --- Internal Data ---
    private Texture2D _mapTexture;
    private Texture2D _dataTexture;
    private Color32[] _dataPixels;
    private List<LogFrame> _frames = new List<LogFrame>();
    private int[] _heatmapAccumulator;
    private int _maxHeat = 1;
    private string _currentSessionFolder; // Added to store path

    private SessionContext _context;


    // Grid Specs
    private int _gridWidth;
    private int _gridHeight;
    private float _cellSize;

    public TextMeshProUGUI debugtext;

    struct LogFrame
    {
        public float time;
        public Vector3 headPos;
        public Quaternion headRot;
        public Vector3 gunPos;
        public Quaternion gunRot;
        public byte[] gridBytes;
    }

    void Start()
    {
        if (timelineSlider != null)
            timelineSlider.onValueChanged.AddListener(OnUserScrub);

        // Visual Cleanup: Make sure grid overlay is transparent to start
        if (dataOverlayImage != null) dataOverlayImage.color = Color.white;
    }

    public void Initialize(string sessionFolderPath)
    {
        _currentSessionFolder = sessionFolderPath; // Store folder path
        this.logFileName = Path.Combine(sessionFolderPath, "telemetry.bin");
        StartCoroutine(LoadSequence());
    }

    private IEnumerator LoadSequence()
    {
        yield return new WaitForSeconds(0.1f);
        LoadMapImage();
        DebugLog("Map image loaded | ");
        LoadLogData();
        DebugLog("Log data loaded | ");
        // --- NEW: CALCULATE & PUSH STATS ---
        if (_frames.Count > 0 && _context != null)
        {
            // Get the very last frame of data (the final state of the room)
            byte[] finalGrid = _frames[_frames.Count - 1].gridBytes;

            // Ask Context to do the math
            float clearedPercent = _context.CalculateClearancePercent(finalGrid);

            // Push to Report Card
            if (reportCardLink != null)
            {
                reportCardLink.InjectClearanceStat(clearedPercent);
            }
        }
        else
            DebugLog("frames: " + _frames.Count.ToString() + "or context null | ");
        // --- NEW: DIAGNOSTIC ---
        AnalyzeSessionData();
        DebugLog("Session data analysed | ");
        UpdateVisualization();
        DebugLog("visualisation updated | ");
    }
    public void HeatmapToggle()
    { 
        showHeatmap = !showHeatmap;
    }
    void Update()
    {
        if (isPlaying && _frames.Count > 0)
        {
            playbackProgress += Time.deltaTime * playbackSpeed;
            if (playbackProgress > 1.0f) { playbackProgress = 1.0f; isPlaying = false; }
            if (timelineSlider != null) timelineSlider.SetValueWithoutNotify(playbackProgress);
        }
        UpdateVisualization();
    }

    public void isPlayingButton() { 
        isPlaying = !isPlaying;
    }

    public void OnUserScrub(float value)
    {
        isPlaying = false;
        playbackProgress = value;
        UpdateVisualization();
    }

    private void LoadMapImage()
    {
        string path = Path.Combine(_currentSessionFolder, "Map_Snapshot.png");

        // 2. Fallback: If legacy session or missing, try the global "current" map
        if (!File.Exists(path))
        {
            Debug.LogWarning("Session map not found. Falling back to global map.");
            path = Path.Combine(Application.persistentDataPath, "Map_Snapshot.png");
        }
        if (File.Exists(path))
        {
            byte[] bytes = File.ReadAllBytes(path);
            _mapTexture = new Texture2D(2, 2);
            _mapTexture.LoadImage(bytes);

            if (mapBackgroundImage != null)
            {
                mapBackgroundImage.texture = _mapTexture;
                // Keep aspect ratio
                float aspect = (float)_mapTexture.width / _mapTexture.height;
                var fitter = mapBackgroundImage.GetComponent<AspectRatioFitter>();
                if (fitter) fitter.aspectRatio = aspect;
            }
        }
    }

    private void LoadLogData()
    {
        try
        {
            string path = logFileName;
            if (!File.Exists(path))
            {
                DebugLog("filepath no exist | ");
                return;
            }
            _frames.Clear();

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                string magic = reader.ReadString();
                _gridWidth = reader.ReadInt32();
                _gridHeight = reader.ReadInt32();
                _cellSize = reader.ReadSingle();
                if (_cellSize <= 0.0001f) _cellSize = 0.1f;

                // --- NEW: INITIALIZE CONTEXT HERE ---
                // We have the dimensions now, so we can build the mask
                _context = new SessionContext();
                _context.Initialize(_currentSessionFolder, _gridWidth, _gridHeight, _cellSize);
                // ------------------------------------

                _dataTexture = new Texture2D(_gridWidth, _gridHeight, TextureFormat.RGBA32, false);
                _dataTexture.filterMode = FilterMode.Point;
                if (dataOverlayImage != null) dataOverlayImage.texture = _dataTexture;

                _dataPixels = new Color32[_gridWidth * _gridHeight];
                _heatmapAccumulator = new int[_gridWidth * _gridHeight];

                // ... (Rest of your reading loop is the same) ...
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    if (reader.BaseStream.Length - reader.BaseStream.Position < 4) break;
                    try
                    {
                        LogFrame frame = new LogFrame();
                        frame.time = reader.ReadSingle();
                        frame.headPos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        frame.headRot = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        frame.gunPos = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        frame.gunRot = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        int byteCount = reader.ReadInt32();
                        frame.gridBytes = reader.ReadBytes(byteCount);

                        _frames.Add(frame);
                        AddToHeatmap(frame.gridBytes);
                    }
                    catch
                    {
                        DebugLog("frame read failed");
                        break;
                    }
                }
            }
            DebugLog("log data loaded | ");
        }
        catch (System.Exception e)
        {
            DebugLog("LoadLogData failed " + e.Message);
        }
    }

    // --- NEW DIAGNOSTIC FUNCTION ---
    private void AnalyzeSessionData()
    {
        if (_frames.Count == 0) return;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var f in _frames)
        {
            if (f.headPos.x < minX) minX = f.headPos.x;
            if (f.headPos.x > maxX) maxX = f.headPos.x;
            if (f.headPos.z < minZ) minZ = f.headPos.z;
            if (f.headPos.z > maxZ) maxZ = f.headPos.z;
        }

        float traveledWidth = maxX - minX;
        float traveledDepth = maxZ - minZ;
        float totalGridWidth = _gridWidth * _cellSize;

        Debug.Log($"[AAR ANALYSIS]");
        Debug.Log($"Player Bounds X: {minX:F2} to {maxX:F2} (Size: {traveledWidth:F2}m)");
        Debug.Log($"Player Bounds Z: {minZ:F2} to {maxZ:F2} (Size: {traveledDepth:F2}m)");
        Debug.Log($"Total Map Size: {totalGridWidth}m x {totalGridWidth}m");
        Debug.Log($"Coverage: The player explored {(traveledWidth / totalGridWidth) * 100:F2}% of the available grid width.");
    }

    private void AddToHeatmap(byte[] gridBytes)
    {
        // (Same as before)
        for (int i = 0; i < gridBytes.Length; i++)
        {
            byte b = gridBytes[i];
            for (int bit = 0; bit < 8; bit++)
            {
                if ((b & (1 << bit)) != 0)
                {
                    int pixelIndex = i * 8 + bit;
                    if (pixelIndex < _heatmapAccumulator.Length)
                    {
                        _heatmapAccumulator[pixelIndex]++;
                        if (_heatmapAccumulator[pixelIndex] > _maxHeat) _maxHeat = _heatmapAccumulator[pixelIndex];
                    }
                }
            }
        }
    }

    private void UpdateVisualization()
    {
        if (_frames == null || _frames.Count == 0) return;
        if (dataOverlayImage == null || ghostAgentIcon == null) return;

        int frameIndex = Mathf.FloorToInt(playbackProgress * (_frames.Count - 1));
        LogFrame currentFrame = _frames[frameIndex];

        float worldWidth = _gridWidth * _cellSize;
        float worldHeight = _gridHeight * _cellSize;
        if (worldWidth == 0) worldWidth = 100f; // Safety

        // --- POSITIONING FIX ---
        // We calculate position relative to the GRID (Data Overlay), NOT the Map Snapshot.
        // The DataOverlay represents the full 100m x 100m area.

        Vector2 uvPos = new Vector2(
            (currentFrame.headPos.x / worldWidth) + 0.5f,
            (currentFrame.headPos.z / worldHeight) + 0.5f
        );

        // We use the RectTransform of the DataOverlay, NOT the Map Background
        RectTransform gridRect = dataOverlayImage.GetComponent<RectTransform>();

        ghostAgentIcon.anchoredPosition = new Vector2(
            (uvPos.x - 0.5f) * gridRect.rect.width,
            (uvPos.y - 0.5f) * gridRect.rect.height
        );

        Vector3 rotEuler = currentFrame.headRot.eulerAngles;
        ghostAgentIcon.localRotation = Quaternion.Euler(0, 0, -rotEuler.y);

        // --- UPDATE TEXTURE ---
        if (showHeatmap) DrawHeatmap();
        else DrawPlayback(currentFrame.gridBytes);

        _dataTexture.SetPixels32(_dataPixels);
        _dataTexture.Apply();
    }

    private void DrawPlayback(byte[] gridBytes)
    {
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 colorSeen = new Color32(0, 255, 0, 100);   // Transparent Green
        Color32 colorUnseen = new Color32(50, 0, 0, 150);  // Dark Red (Fog of War)

        for (int i = 0; i < _dataPixels.Length; i++)
        {
            // 1. Check Cookie Cutter (Is this pixel in the room?)
            if (_context != null && !_context.IsCellPlayable(i))
            {
                _dataPixels[i] = clear; // It's a leak/void. Hide it.
                continue;
            }

            // 2. It IS in the room. Did we see it?
            int byteIndex = i / 8;
            int bitOffset = i % 8;
            bool isSeen = false;
            if (byteIndex < gridBytes.Length)
            {
                isSeen = (gridBytes[byteIndex] & (1 << bitOffset)) != 0;
            }

            // 3. Color logic
            if (isSeen)
                _dataPixels[i] = colorSeen;
            else
                _dataPixels[i] = colorUnseen; // It's in the room, but we missed it!
        }
    }

    private void DrawHeatmap()
    {
        // (Same as before)
        Color32 clear = new Color32(0, 0, 0, 0);
        for (int i = 0; i < _heatmapAccumulator.Length; i++)
        {
            int val = _heatmapAccumulator[i];
            if (val == 0) _dataPixels[i] = clear;
            else
            {
                float t = (float)val / _maxHeat;
                _dataPixels[i] = Color32.Lerp(new Color32(0, 0, 255, 150), new Color32(255, 0, 0, 255), t);
            }
        }
    }

    public void DebugLog(string msg)
    {
        if (debugtext != null)
        {
            debugtext.text += msg;
        }
    }
}