using UnityEngine;
using System.IO;
using TMPro; // TextMeshPro

public class AAR_ReportCard : MonoBehaviour
{
    [Header("UI Output")]
    public TextMeshProUGUI reportText; // Drag your text object here

    // Data Holders
    private SessionManifest _manifest;
    private EventLog _eventLog;

    public void GenerateReport(string folderPath)
    {
        Debug.Log($"[ReportCard] Reading JSON from: {folderPath}");

        // 1. Load Manifest (Setup)
        string manifestPath = Path.Combine(folderPath, "manifest.json");
        if (File.Exists(manifestPath))
        {
            _manifest = JsonUtility.FromJson<SessionManifest>(File.ReadAllText(manifestPath));
        }

        // 2. Load Events (Actions)
        string eventsPath = Path.Combine(folderPath, "events.json");
        if (File.Exists(eventsPath))
        {
            _eventLog = JsonUtility.FromJson<EventLog>(File.ReadAllText(eventsPath));
        }

        // 3. Calculate & Display
        CalculateStats();
    }

    private void CalculateStats()
    {
        if (_manifest == null || _eventLog == null)
        {
            reportText.text = "Error: Missing Session Data files.";
            return;
        }

        // --- MATH ---
        // Duration: Last event time - First event time
        float duration = 0f;
        if (_eventLog.events.Count > 1)
        {
            duration = _eventLog.events[_eventLog.events.Count - 1].timestamp - _eventLog.events[0].timestamp;
        }

        // Counts
        int shots = 0;
        int hits = 0;
        int kills = 0;
        int totalEnemies = _manifest.enemies.Count;

        foreach (var evt in _eventLog.events)
        {
            if (evt.eventType == "SHOT") shots++;
            if (evt.eventType == "HIT") hits++;
            if (evt.eventType == "KILL") kills++;
        }

        float accuracy = shots > 0 ? ((float)hits / shots) * 100f : 0f;

        // --- DISPLAY ---
        string text = "<size=150%><b>MISSION DEBRIEF</b></size>\n\n";
        text += $"<b>Time:</b> {duration:F1}s\n";
        text += $"<b>Threats Neutralized:</b> {kills} / {totalEnemies}\n";
        text += $"<b>Shots Fired:</b> {shots}\n";
        text += $"<b>Accuracy:</b> {accuracy:F1}%\n\n";

        if (kills >= totalEnemies)
            text += "<color=green>STATUS: AREA SECURE</color>";
        else
            text += $"<color=red>STATUS: {totalEnemies - kills} THREATS REMAINING</color>";

        reportText.text = text;
    }
}