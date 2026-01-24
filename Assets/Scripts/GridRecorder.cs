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
    public Transform gunTransform;

    [Header("Settings")]
    public float recordFrequency = 10.0f;

    // Internal
    private bool _isRecording = false;
    private BinaryWriter _telemetryWriter;
    private FileStream _fileStream;
    private EventLog _currentSessionEvents = new EventLog();
    private string _folderPath;

    void Awake()
    {
        Instance = this;
        // Ensure base folder exists
        _folderPath = Path.Combine(Application.persistentDataPath, "Sessions");
        if (!Directory.Exists(_folderPath)) Directory.CreateDirectory(_folderPath);
    }

    public void StartRecording()
    {
        if (_isRecording) return;

        _isRecording = true;

        // Reset Event Log
        _currentSessionEvents = new EventLog();

        // Start the routine which now handles the Setup AND the Loop
        StartCoroutine(RecordRoutine());
    }

    private IEnumerator RecordRoutine()
    {
        // --- 1. THE SIMPLE FIX ---
        // Wait 0.2 seconds. This gives MapGenerator plenty of time 
        // to finish rendering and overwrite 'Map_Snapshot.png'.
        yield return new WaitForSeconds(0.2f);

        // --- 2. CREATE SESSION FOLDER ---
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string runFolder = Path.Combine(_folderPath, timestamp);
        Directory.CreateDirectory(runFolder);

        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.lastSessionFolderPath = runFolder;
        }

        // --- 3. COPY MAP SNAPSHOT ---
        // Now that we waited, this file should be the NEW one.
        string sourceMapPath = Path.Combine(Application.persistentDataPath, "Map_Snapshot.png");
        string destMapPath = Path.Combine(runFolder, "Map_Snapshot.png");

        if (File.Exists(sourceMapPath))
        {
            File.Copy(sourceMapPath, destMapPath, true);
            Debug.Log($"[Recorder] Copied fresh Map Snapshot to: {destMapPath}");
        }
        else
        {
            Debug.LogWarning("[Recorder] Map Snapshot not found in PersistentDataPath.");
        }

        // --- 4. CREATE TELEMETRY FILE ---
        SaveManifest(runFolder); // Save JSON Manifest

        string telemetryPath = Path.Combine(runFolder, "telemetry.bin");
        _fileStream = File.Open(telemetryPath, FileMode.Create);
        _telemetryWriter = new BinaryWriter(_fileStream);

        // Write Header
        _telemetryWriter.Write("CQB_STREAM_V2");
        _telemetryWriter.Write(sourceGrid.GridWidth);
        _telemetryWriter.Write(sourceGrid.GridHeight);
        _telemetryWriter.Write(sourceGrid.CellSize);

        Debug.Log($"[Recorder] Recording started in: {runFolder}");

        // --- 5. RECORDING LOOP ---
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

    public void StopRecording()
    {
        _isRecording = false;
        // Stop the coroutine immediately so it doesn't try to write one more frame
        StopAllCoroutines();

        // CLOSE IMMEDIATELY - Do not wait a frame
        if (_telemetryWriter != null)
        {
            _telemetryWriter.Flush();
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

        // Save Events
        var directory = new DirectoryInfo(_folderPath);
        if (directory.GetDirectories().Length > 0)
        {
            var recentRun = directory.GetDirectories()[directory.GetDirectories().Length - 1];
            SaveEvents(recentRun.FullName);
        }

        Debug.Log("[Recorder] Recording Stopped and File Streams Closed.");
    }


    private void SaveManifest(string folder)
    {
        if (SessionManager.Instance == null) return;
        SessionManifest manifest = SessionManager.Instance.GenerateManifest();
        string json = JsonUtility.ToJson(manifest, true);
        File.WriteAllText(Path.Combine(folder, "manifest.json"), json);
    }

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
}