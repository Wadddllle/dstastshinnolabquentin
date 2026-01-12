using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SessionData
{
    // Where the instructor placed enemies (for AAR visualization later)
    //public List<Pose> enemySpawnLocations = new List<Pose>();

    // The name of the file we just recorded
    public string currentRecordingFileName = "session_001.bin";

    //public void Reset()
    //{
    //    enemySpawnLocations.Clear();
    //}
}