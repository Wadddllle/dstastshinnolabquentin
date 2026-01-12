using UnityEngine;
using System.Collections;
using System.IO;

public class GridRecorder : MonoBehaviour
{
    [Header("Dependencies")]
    public TacticalGrid sourceGrid;
    public Transform headTransform;

    [Header("Settings")]
    public float recordFrequency = 10.0f;
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

        // Don't open the file yet. Wait until we are sure data exists.
        _isRecording = true;
        StartCoroutine(RecordRoutine());
    }

    [ContextMenu("Stop Recording")]
    public void StopRecording()
    {
        _isRecording = false;
        // The loop in RecordRoutine will break naturally now,
        // but we can force close if needed in OnDestroy.
    }

    private IEnumerator RecordRoutine()
    {
        // 1. WAIT FOR GRID INITIALIZATION
        // This ensures Width, Height, and CellSize are set before we write them.
        while (!sourceGrid.IsInitialized)
        {
            yield return null;
        }

        // 2. NOW Open File & Write Header
        string path = Path.Combine(Application.persistentDataPath, fileName);
        _fileStream = File.Open(path, FileMode.Create);
        _writer = new BinaryWriter(_fileStream);

        _writer.Write("GRID");
        _writer.Write(1);
        _writer.Write(sourceGrid.GridWidth);
        _writer.Write(sourceGrid.GridHeight);
        _writer.Write(sourceGrid.CellSize); // This will now be correct (~0.1)

        Debug.Log($"Recording Started. Header written. Saving to: {path}");

        // 3. Recording Loop
        float interval = 1.0f / recordFrequency;

        while (_isRecording)
        {
            // Capture Data
            float timestamp = Time.time;
            Vector3 pos = headTransform.position;
            Quaternion rot = headTransform.rotation;
            byte[] gridBytes = sourceGrid.GetCompressedGridData();

            // Write Frame
            _writer.Write(timestamp);
            _writer.Write(pos.x);
            _writer.Write(pos.y);
            _writer.Write(pos.z);
            _writer.Write(rot.x);
            _writer.Write(rot.y);
            _writer.Write(rot.z);
            _writer.Write(rot.w);

            _writer.Write(gridBytes.Length);
            _writer.Write(gridBytes);

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
        if (_isRecording) _isRecording = false; // Break the loop
        CloseFile();
    }
}