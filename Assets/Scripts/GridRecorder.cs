using UnityEngine;
using System.Collections;
using System.IO;

public class GridRecorder : MonoBehaviour
{
    [Header("Dependencies")]
    public TacticalGrid sourceGrid;
    public Transform headTransform;

    [Header("Settings")]
    public float recordFrequency = 10.0f; // 10Hz is plenty for grid replays
    public string fileName = "session_data.bin";

    private bool _isRecording = false;
    private BinaryWriter _writer;
    private FileStream _fileStream;

    [ContextMenu("Start Recording")]
    public void StartRecording()
    {
        if (_isRecording) return;
        if (sourceGrid == null || headTransform == null)
        {
            Debug.LogError("GridRecorder: Missing dependencies!");
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, fileName);

        // Open File Stream
        _fileStream = File.Open(path, FileMode.Create);
        _writer = new BinaryWriter(_fileStream);

        // --- WRITE HEADER ---
        // This allows the Replayer to know how to reconstruct the grid later
        _writer.Write("GRID"); // Magic string check
        _writer.Write(1);      // Version number (useful if you change format later)
        _writer.Write(sourceGrid.GridWidth);
        _writer.Write(sourceGrid.GridHeight);
        _writer.Write(sourceGrid.CellSize);

        Debug.Log($"Recording Started. Saving to: {path}");
        _isRecording = true;
        StartCoroutine(RecordRoutine());
    }

    [ContextMenu("Stop Recording")]
    public void StopRecording()
    {
        _isRecording = false; // Coroutine will exit naturally
    }

    private IEnumerator RecordRoutine()
    {
        float interval = 1.0f / recordFrequency;

        while (_isRecording)
        {
            // 1. Capture Data
            float timestamp = Time.time;
            Vector3 pos = headTransform.position;
            Quaternion rot = headTransform.rotation;

            // This is the heavy lifting: Get the packed bytes (Seen/Unseen)
            byte[] gridBytes = sourceGrid.GetCompressedGridData();

            // 2. Write Frame to Disk
            // Format: [Time(4)][Pos(12)][Rot(16)][GridData(Var)]
            _writer.Write(timestamp);

            _writer.Write(pos.x);
            _writer.Write(pos.y);
            _writer.Write(pos.z);

            _writer.Write(rot.x);
            _writer.Write(rot.y);
            _writer.Write(rot.z);
            _writer.Write(rot.w);

            // Flexible: If you add events later, you could write an Event ID here
            // For now, we just dump the grid bytes.
            _writer.Write(gridBytes.Length);
            _writer.Write(gridBytes);

            // Flush periodically to ensure data saves if app crashes
            // (Optional, doing it every frame is safer for prototypes)
            _writer.Flush();

            yield return new WaitForSeconds(interval);
        }

        // Cleanup when loop finishes
        CloseFile();
    }

    private void CloseFile()
    {
        if (_writer != null)
        {
            _writer.Close();
            _writer = null;
        }
        if (_fileStream != null)
        {
            _fileStream.Close();
            _fileStream = null;
        }
        Debug.Log("Recording Saved & Closed.");
    }

    private void OnDestroy()
    {
        // Safety net: Close file if scene changes or app quits
        if (_isRecording) StopRecording();
        CloseFile();
    }
}