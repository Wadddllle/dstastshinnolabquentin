using UnityEngine;
using System.IO;

public class AARState : BaseState
{
    public override void EnterState()
    {
        Debug.Log("STATE: AAR Mode");

        // 1. Swap Roots
        Manager.SetActiveRoot(Manager.AARRoot);

        // 2. Get the Folder Path (The new way)
        string folderPath = SessionManager.Instance.lastSessionFolderPath;

        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            Debug.LogError("[AAR] No session folder found via SessionManager!");
            return;
        }

        // 3. Initialize Visualizer (Binary Playback)
        if (Manager.aarVisualizer != null)
        {
            Manager.aarVisualizer.Initialize(folderPath);
        }

        // 4. Initialize Report Card (JSON Stats)
        // Ensure you added 'aarReportCard' to AppManager references!
        if (Manager.aarReportCard != null)
        {
            Manager.aarReportCard.GenerateReport(folderPath);
        }
    }

    public override void UpdateState()
    {
        // Restart logic
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            Manager.ChangeState(new InstructorState());
        }
    }

    public override void ExitState()
    {
    }
}