using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class PositionLogger : MonoBehaviour
{
    public Transform head; // Assign the CenterEyeAnchor of the OVRCameraRig here
    public float logFrequency = 30.0f;

    private List<string> _logData = new List<string>();
    private float _logInterval;
    private bool _isLogging = false;
    private Coroutine _loggingCoroutine; // A reference to our running coroutine

    void Start()
    {
        _logInterval = 1.0f / logFrequency;
    }

    [ContextMenu("Start Logging")] // This adds the "Start Logging" option to the Inspector
    public void StartLogging()
    {
        if (!_isLogging)
        {
            _logData.Clear();
            _logData.Add("Timestamp,PosX,PosY,PosZ,RotX,RotY,RotZ,RotW");
            _isLogging = true;
            _loggingCoroutine = StartCoroutine(LogPosition());
            Debug.Log("--- LOGGING STARTED ---");
        }
    }

    [ContextMenu("Stop and Save Log")] // This adds the "Stop and Save Log" option to the Inspector
    public void StopLoggingAndSave()
    {
        if (_isLogging)
        {
            _isLogging = false;
            if (_loggingCoroutine != null)
            {
                StopCoroutine(_loggingCoroutine);
            }
            SaveLogToFile();
            Debug.Log("--- LOGGING STOPPED AND SAVED ---");
        }
    }

    private IEnumerator LogPosition()
    {
        while (_isLogging)
        {
            float timestamp = Time.time;
            Vector3 pos = head.position;
            Quaternion rot = head.rotation;

            string logEntry = $"{timestamp},{pos.x},{pos.y},{pos.z},{rot.x},{rot.y},{rot.z},{rot.w}";
            _logData.Add(logEntry);

            yield return new WaitForSeconds(_logInterval);
        }
    }

    private void SaveLogToFile()
    {
        StringBuilder sb = new StringBuilder();
        foreach (string line in _logData)
        {
            sb.AppendLine(line);
        }

        // Ensure the directory exists
        string directoryPath = Path.GetDirectoryName(Application.persistentDataPath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string filePath = Path.Combine(Application.persistentDataPath, "trainee_log.csv");
        File.WriteAllText(filePath, sb.ToString());
        Debug.Log($"Log file saved to: {filePath}");
    }

}