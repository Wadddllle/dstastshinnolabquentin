using UnityEngine;

public class TraineeState : BaseState
{
    public override void EnterState()
    {
        Debug.Log("STATE: Trainee Mode");

        GameObject projLauncher = Manager.projectileLauncher.gameObject;
        projLauncher.SetActive(true);
        GameObject tacGrid = Manager.tacticalGrid.gameObject;
        tacGrid.SetActive(true);
        GameObject gridRec = Manager.gridRecorder.gameObject;
        gridRec.SetActive(true);
        Manager.visionScanner.gameObject.SetActive(true);
        Manager.visionScanner.enabled = true;

        // 1. Set the filename in the blackboard
        Manager.currentSession.currentRecordingFileName = "Run_" + System.DateTime.Now.ToString("MMdd_HHmm") + ".bin";

        // 2. Configure your existing Recorder
        Manager.gridRecorder.fileName = Manager.currentSession.currentRecordingFileName;

        // 3. START RECORDING!
        Manager.gridRecorder.StartRecording();
    }

    public override void UpdateState()
    {
        // Trainee is running around...
        // Your TacticalGrid.cs is automatically updating visually because of its Update loop.

        // TRANSITION: Finish Run
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch)) // Or VR Button "B"
        {
            Manager.ChangeState(new AARState());
        }
    }

    public override void ExitState()
    {
        // STOP RECORDING!
        Manager.gridRecorder.StopRecording();
    }
}