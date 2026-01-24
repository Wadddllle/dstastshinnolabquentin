using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class SessionContext
{
    public SessionManifest Manifest;
    private bool[] _playableAreaMask; // True = Inside Room, False = Leak/Void

    public int TotalPlayableCells { get; private set; }
    public int GridWidth { get; private set; }
    public int GridHeight { get; private set; }
    public float CellSize { get; private set; }

    // 1. Setup
    public void Initialize(string folderPath, int width, int height, float cellSize)
    {
        GridWidth = width;
        GridHeight = height;
        CellSize = cellSize;

        // Load Manifest to get the Zone Points
        string manifestPath = Path.Combine(folderPath, "manifest.json");
        if (File.Exists(manifestPath))
        {
            Manifest = JsonUtility.FromJson<SessionManifest>(File.ReadAllText(manifestPath));
        }

        GenerateMask();
    }

    // 2. The Cookie Cutter Logic
    private void GenerateMask()
    {
        _playableAreaMask = new bool[GridWidth * GridHeight];
        TotalPlayableCells = 0;

        // If no zone exists, assume whole map is valid (Fallback)
        if (Manifest == null || Manifest.zoneBounds == null || Manifest.zoneBounds.Count < 3)
        {
            for (int i = 0; i < _playableAreaMask.Length; i++) _playableAreaMask[i] = true;
            TotalPlayableCells = _playableAreaMask.Length;
            return;
        }

        // Convert World Points (Meters) to Grid Points (Indices)
        List<Vector2> gridPoly = new List<Vector2>();
        float worldW = GridWidth * CellSize;
        float worldH = GridHeight * CellSize;

        foreach (var pt in Manifest.zoneBounds)
        {
            // UV coordinate math (Same as Visualizer)
            float u = (pt.x / worldW) + 0.5f;
            float v = (pt.z / worldH) + 0.5f;
            gridPoly.Add(new Vector2(u * GridWidth, v * GridHeight));
        }

        // Check every pixel
        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                if (PolygonMath.IsPointInPolygon(new Vector2(x + 0.5f, y + 0.5f), gridPoly))
                {
                    _playableAreaMask[y * GridWidth + x] = true;
                    TotalPlayableCells++;
                }
            }
        }
    }

    public bool IsCellPlayable(int index)
    {
        if (_playableAreaMask == null || index < 0 || index >= _playableAreaMask.Length) return false;
        return _playableAreaMask[index];
    }

    // 3. The Stat Calculator
    public float CalculateClearancePercent(byte[] finalGridBytes)
    {
        if (TotalPlayableCells == 0) return 0f;

        int clearedCount = 0;
        for (int i = 0; i < _playableAreaMask.Length; i++)
        {
            // If this cell is NOT in the room, skip it (don't count leaks)
            if (!IsCellPlayable(i)) continue;

            // Check if it was seen
            int byteIndex = i / 8;
            int bitOffset = i % 8;
            bool isSeen = false;
            if (byteIndex < finalGridBytes.Length)
                isSeen = (finalGridBytes[byteIndex] & (1 << bitOffset)) != 0;

            if (isSeen) clearedCount++;
        }

        return ((float)clearedCount / TotalPlayableCells) * 100f;
    }
}