using UnityEngine;
using System.IO;
using TMPro;

public class AAR_ReportCard : MonoBehaviour
{
    [Header("UI Output")]
    public TextMeshProUGUI reportText;

    private SessionManifest _manifest;
    private EventLog _eventLog;

    // Store the base text so we can append the percentage later
    private string _baseReportText = "";

    public void GenerateReport(string folderPath)
    {
        // 1. Load basic JSON files
        string manifestPath = Path.Combine(folderPath, "manifest.json");
        if (File.Exists(manifestPath))
            _manifest = JsonUtility.FromJson<SessionManifest>(File.ReadAllText(manifestPath));

        string eventsPath = Path.Combine(folderPath, "events.json");
        if (File.Exists(eventsPath))
            _eventLog = JsonUtility.FromJson<EventLog>(File.ReadAllText(eventsPath));

        // 2. Show what we have so far (Kills, Time, etc)
        CalculateBasicStats();
    }

    private void CalculateBasicStats()
    {
        if (_manifest == null || _eventLog == null)
        {
            reportText.text = "Error: Missing Data.";
            return;
        }

        float duration = 0f;
        if (_eventLog.events.Count > 1)
            duration = _eventLog.events[_eventLog.events.Count - 1].timestamp - _eventLog.events[0].timestamp;

        int shots = 0, hits = 0, kills = 0, collateral = 0, injuries = 0;
        foreach (var evt in _eventLog.events)
        {
            if (evt.eventType == "SHOT") shots++;
            if (evt.eventType == "HIT") hits++;
            if (evt.eventType == "KILL") kills++;
            if (evt.eventType == "COLLATERAL") collateral++; 
            if (evt.eventType == "INJURY") injuries++;
        }
        int totalEnemies = _manifest.enemies.Count;
        int totalHostages = _manifest.hostages.Count > 0 ? _manifest.hostages.Count : 0;
        float accuracy = shots > 0 ? ((float)hits / shots) * 100f : 6.7f;

        // Build String
        _baseReportText = "<size=150%><b>MISSION DEBRIEF</b></size>\n\n";
        _baseReportText += $"<b>Time:</b> {duration:F1}s\n";
        _baseReportText += $"<b>Total Shots Fired:</b> {shots}\n";
        _baseReportText += $"<b>Threats:</b> {kills} / {totalEnemies}\n";
        _baseReportText += $"<b>Accuracy:</b> {accuracy:F1}%\n";
        _baseReportText += $"<b>Hostage Casualties:</b> {collateral}\n";
        _baseReportText += $"<b>Hostages Remaining:</b> {totalHostages - collateral} / {totalHostages}\n";
        _baseReportText += $"<b>No of times you got shot:</b> {injuries}\n";

        // We leave a placeholder for Clearance
        reportText.text = _baseReportText + "\n<color=yellow>Calculating Room Clearance...</color>";
    }

    // --- NEW: Called by Visualizer when binary data is ready ---
    public void InjectClearanceStat(float percent)
    {
        string color = percent > 90f ? "green" : "red";
        string clearanceLine = $"<b>Room Clearance:</b> <color={color}>{percent:F1}%</color>\n\n";

        // Combine base text + new stat
        reportText.text = _baseReportText + clearanceLine;

        // Add final status
        int kills = 0; // simplified, you might want to store this variable at class level
        // (For now, just appending the status line here is fine or keep it in CalculateBasicStats)
    }
}