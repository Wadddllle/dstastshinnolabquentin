//using UnityEngine;
//using System.Collections.Generic;
//using System.IO;

//// A structure to hold the state of the trainee at a single point in time.
//// This is a clean way to organize the data for each log entry.
//public struct TraineeState
//{
//    public float timestamp;
//    public Vector3 position;
//    public Quaternion rotation;
//}

//public class AAR_DataProcessor : MonoBehaviour
//{
//    // This list will hold all the data points after they are loaded from the file.
//    public List<TraineeState> traineePath = new List<TraineeState>();

//    // The name of the log file we expect to find.
//    public string logFileName = "trainee_log.csv";

//    // This is automatically called when the script starts.
//    void Start()
//    {
//        LoadTraineeLog();
//    }

//    [ContextMenu("Load Trainee Log File")] // Add a button in the inspector to test loading
//    public void LoadTraineeLog()
//    {
//        // Clear any old data before loading new data.
//        traineePath.Clear();

//        // Construct the full path to the file using Application.persistentDataPath.
//        // This is the crucial step that finds the file your logger saved.
//        string filePath = Path.Combine(Application.persistentDataPath, logFileName);

//        // Always check if the file actually exists before trying to read it.
//        if (!File.Exists(filePath))
//        {
//            Debug.LogError($"AAR Error: Log file not found at '{filePath}'. Make sure you have run a logging session first.");
//            return;
//        }

//        // Read all lines from the CSV file into an array of strings.
//        string[] lines = File.ReadAllLines(filePath);

//        // We start the loop at '1' to deliberately skip the header row ("Timestamp,PosX,PosY...").
//        for (int i = 1; i < lines.Length; i++)
//        {
//            string line = lines[i];
//            string[] values = line.Split(',');

//            // A safety check to make sure the line has enough columns of data.
//            if (values.Length >= 8)
//            {
//                try
//                {
//                    TraineeState state = new TraineeState();

//                    // Parse each value from the string array and convert it to the correct type.
//                    state.timestamp = float.Parse(values[0]);
//                    state.position = new Vector3(float.Parse(values[1]), float.Parse(values[2]), float.Parse(values[3]));
//                    state.rotation = new Quaternion(float.Parse(values[4]), float.Parse(values[5]), float.Parse(values[6]), float.Parse(values[7]));

//                    // Add the fully parsed data point to our list.
//                    traineePath.Add(state);
//                }
//                catch (System.Exception e)
//                {
//                    // If any value is malformed (e.g., not a number), we'll catch it here.
//                    Debug.LogWarning($"Could not parse line {i}: '{line}'. Error: {e.Message}");
//                }
//            }
//        }

//        Debug.Log($"Successfully loaded {traineePath.Count} data points from {filePath}.");

//        // Now that the data is loaded, you can proceed with the rest of your Week 8 logic.
//        // For example: ProcessAAR();
//    }
//}