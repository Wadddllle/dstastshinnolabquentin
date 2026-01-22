using UnityEngine;
using System.Collections.Generic;
using System;

// --- 1. THE MANIFEST (Static Setup) ---
// This saves the Room Layout.
[Serializable]
public class SessionManifest
{
    public string sessionID;
    public long startTime;

    // 1. The "Kill Box" polygon defined by ZoneTool
    public List<Vector3> zoneBounds = new List<Vector3>();

    // 2. The Start Point defined by CoordinateMarker
    public Vector3 breachPoint;

    // 3. The placed assets
    public List<EntityData> enemies = new List<EntityData>();
    public List<EntityData> obstacles = new List<EntityData>();
}

// Helper class to save Position/Rotation/Scale easily
[Serializable]
public class EntityData
{
    public string id;      // e.g., "Target_1234"
    public string type;    // "Target" or "Wall"
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}

// --- 2. THE EVENT LOG (Timeline) ---
// This is used by GridRecorder to save specific moments (Shots, Kills).
[Serializable]
public class EventLog
{
    public List<CQBEvent> events = new List<CQBEvent>();
}

[Serializable]
public class CQBEvent
{
    public float timestamp;
    public string eventType; // "SHOT", "HIT", "KILL", "CLEAR"
    public string details;
    public Vector3 location;
}