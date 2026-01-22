//using UnityEngine;
//using System.Collections;
//using System.IO;

//public class GridRecorder : MonoBehaviour
//{
//    [Header("Dependencies")]
//    public TacticalGrid sourceGrid;
//    public Transform headTransform;

//    [Header("Settings")]
//    public float recordFrequency = 10.0f;
//    public string fileName = "session_data.bin";

//    private bool _isRecording = false;
//    private BinaryWriter _writer;
//    private FileStream _fileStream;

//    [ContextMenu("Start Recording")]
//    public void StartRecording()
//    {
//        if (_isRecording) return;
//        if (sourceGrid == null || headTransform == null)
//        {
//            Debug.LogError("GridRecorder: Missing dependencies!");
//            return;
//        }

//        // Don't open the file yet. Wait until we are sure data exists.
//        _isRecording = true;
//        StartCoroutine(RecordRoutine());
//    }

//    [ContextMenu("Stop Recording")]
//    public void StopRecording()
//    {
//        _isRecording = false;

//        // FIX: Force the file to close IMMEDIATELY. 
//        // Do not wait for the Coroutine to finish in the next frame.
//        CloseFile();

//        // Optional: Stop the coroutine so it doesn't try to run again
//        StopAllCoroutines();

//        Debug.Log("Recording Stopped and File Closed immediately.");
//    }

//    private IEnumerator RecordRoutine()
//    {
//        // 1. WAIT FOR GRID INITIALIZATION
//        // This ensures Width, Height, and CellSize are set before we write them.
//        while (!sourceGrid.IsInitialized)
//        {
//            yield return null;
//        }

//        // 2. NOW Open File & Write Header
//        string path = Path.Combine(Application.persistentDataPath, fileName);
//        _fileStream = File.Open(path, FileMode.Create);
//        _writer = new BinaryWriter(_fileStream);

//        _writer.Write("GRID");
//        _writer.Write(1);
//        _writer.Write(sourceGrid.GridWidth);
//        _writer.Write(sourceGrid.GridHeight);
//        _writer.Write(sourceGrid.CellSize); // This will now be correct (~0.1)

//        Debug.Log($"Recording Started. Header written. Saving to: {path}");

//        // 3. Recording Loop
//        float interval = 1.0f / recordFrequency;

//        while (_isRecording)
//        {
//            // Capture Data
//            float timestamp = Time.time;
//            Vector3 pos = headTransform.position;
//            Quaternion rot = headTransform.rotation;
//            byte[] gridBytes = sourceGrid.GetCompressedGridData();

//            // Write Frame
//            _writer.Write(timestamp);
//            _writer.Write(pos.x);
//            _writer.Write(pos.y);
//            _writer.Write(pos.z);
//            _writer.Write(rot.x);
//            _writer.Write(rot.y);
//            _writer.Write(rot.z);
//            _writer.Write(rot.w);

//            _writer.Write(gridBytes.Length);
//            _writer.Write(gridBytes);

//            _writer.Flush();

//            yield return new WaitForSeconds(interval);
//        }

//        // Cleanup when loop finishes
//        CloseFile();
//    }

//    private void CloseFile()
//    {
//        // Safety Check: Make sure we don't try to close an already closed file
//        // (This makes the function "Idempotent" - safe to call twice)

//        if (_writer != null)
//        {
//            _writer.Flush(); // Ensure all data is written
//            _writer.Close();
//            _writer.Dispose();
//            _writer = null;
//        }

//        if (_fileStream != null)
//        {
//            _fileStream.Close();
//            _fileStream.Dispose();
//            _fileStream = null;
//        }
//    }

//    private void OnDestroy()
//    {
//        if (_isRecording) StopRecording();
//        CloseFile();
//    }
//}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class GridRecorder : MonoBehaviour
{
    public static GridRecorder Instance;

    [Header("Dependencies")]
    public TacticalGrid sourceGrid;
    public Transform headTransform;
    public Transform gunTransform; // NEW: Assign Right Controller Anchor here

    [Header("Settings")]
    public float recordFrequency = 10.0f;
    public string sessionName = "latest_run";

    private bool _isRecording = false;
    private BinaryWriter _telemetryWriter;
    private FileStream _fileStream;

    // Event Log System
    private EventLog _currentSessionEvents = new EventLog();

    // Paths
    private string _folderPath;

    void Awake()
    {
        Instance = this;

        // Define a folder to keep runs organized
        _folderPath = Path.Combine(Application.persistentDataPath, "Sessions");
        if (!Directory.Exists(_folderPath)) Directory.CreateDirectory(_folderPath);
    }

    public void StartRecording()
    {
        if (_isRecording) return;
        Debug.Log("[GridRecorder] Started Recording");
        // 1. Create a Subfolder for this specific run
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string runFolder = Path.Combine(_folderPath, timestamp);
        Directory.CreateDirectory(runFolder);

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.lastSessionFolderPath = runFolder;
        }

        // 2. Save Static Manifest (JSON)
        SaveManifest(runFolder);

        // 3. Open Telemetry Stream (Binary)
        string telemetryPath = Path.Combine(runFolder, "telemetry.bin");
        _fileStream = File.Open(telemetryPath, FileMode.Create);
        _telemetryWriter = new BinaryWriter(_fileStream);

        // Write Header
        _telemetryWriter.Write("CQB_STREAM_V2");
        _telemetryWriter.Write(sourceGrid.GridWidth);
        _telemetryWriter.Write(sourceGrid.GridHeight);
        _telemetryWriter.Write(sourceGrid.CellSize);

        Debug.Log($"[Recorder] Recording started in: {runFolder}");

        _isRecording = true;
        _currentSessionEvents = new EventLog(); // Reset events
        StartCoroutine(RecordTelemetryRoutine());
    }

    public void StopRecording()
    {
        _isRecording = false;
        StopAllCoroutines();

        // Close Binary Stream
        if (_telemetryWriter != null)
        {
            _telemetryWriter.Close();
            _telemetryWriter.Dispose();
            _telemetryWriter = null;
        }
        if (_fileStream != null)
        {
            _fileStream.Close();
            _fileStream.Dispose();
            _fileStream = null;
        }

        // Save Events (JSON)
        // We find the most recent folder (hacky, but works for linear sessions) 
        // OR simpler: store the path in a variable during StartRecording. 
        // For now, let's just save to the same structure logic.
        // BETTER: Let's assume the session is just finished, we find the last modified folder.
        var directory = new DirectoryInfo(_folderPath);
        var recentRun = directory.GetDirectories()[directory.GetDirectories().Length - 1];

        SaveEvents(recentRun.FullName);

        Debug.Log("[Recorder] Recording Stopped. Data Saved.");
    }

    // --- 1. MANIFEST SAVER ---
    private void SaveManifest(string folder)
    {
        SessionManifest manifest = SessionManager.Instance.GenerateManifest();
        string json = JsonUtility.ToJson(manifest, true);
        File.WriteAllText(Path.Combine(folder, "manifest.json"), json);
    }

    // --- 2. EVENT LOGGER (Public API) ---
    public void LogEvent(string type, string details, Vector3 location)
    {
        if (!_isRecording) return;

        CQBEvent evt = new CQBEvent();
        evt.timestamp = Time.time;
        evt.eventType = type;
        evt.details = details;
        evt.location = location;

        _currentSessionEvents.events.Add(evt);
        Debug.Log($"[Event Logged] {type}: {details}");
    }

    private void SaveEvents(string folder)
    {
        string json = JsonUtility.ToJson(_currentSessionEvents, true);
        File.WriteAllText(Path.Combine(folder, "events.json"), json);
    }

    // --- 3. TELEMETRY STREAM (Binary) ---
    private IEnumerator RecordTelemetryRoutine()
    {
        float interval = 1.0f / recordFrequency;

        while (_isRecording)
        {
            float t = Time.time;

            // Player Head
            Vector3 hPos = headTransform.position;
            Quaternion hRot = headTransform.rotation;

            // Weapon (Right Controller)
            Vector3 wPos = gunTransform != null ? gunTransform.position : Vector3.zero;
            Quaternion wRot = gunTransform != null ? gunTransform.rotation : Quaternion.identity;

            // Grid
            byte[] gridBytes = sourceGrid.GetCompressedGridData();

            // WRITE
            _telemetryWriter.Write(t);

            // Head
            _telemetryWriter.Write(hPos.x); _telemetryWriter.Write(hPos.y); _telemetryWriter.Write(hPos.z);
            _telemetryWriter.Write(hRot.x); _telemetryWriter.Write(hRot.y); _telemetryWriter.Write(hRot.z); _telemetryWriter.Write(hRot.w);

            // Weapon
            _telemetryWriter.Write(wPos.x); _telemetryWriter.Write(wPos.y); _telemetryWriter.Write(wPos.z);
            _telemetryWriter.Write(wRot.x); _telemetryWriter.Write(wRot.y); _telemetryWriter.Write(wRot.z); _telemetryWriter.Write(wRot.w);

            // Grid
            _telemetryWriter.Write(gridBytes.Length);
            _telemetryWriter.Write(gridBytes);

            yield return new WaitForSeconds(interval);
        }
    }
}