using UnityEngine;

public class TraineeState : BaseState
{
    public override void EnterState()
    {
        Debug.Log("STATE: Trainee Mode");

        // 1. The Magic Swap (Turns on Grid, Recorder, Launcher, UI all at once)
        Manager.SetActiveRoot(Manager.TraineeRoot);

        // 2. Handle the "Exception" (VisionScanner on the head)
        if (Manager.visionScanner != null)
        {
            Manager.visionScanner.gameObject.SetActive(true);
            Manager.visionScanner.enabled = true;
        }

        // 3. Logic Setup (Filenames, etc.)
        Manager.currentSession.currentRecordingFileName = "Run_" + System.DateTime.Now.ToString("MMdd_HHmm") + ".bin";
        Manager.gridRecorder.fileName = Manager.currentSession.currentRecordingFileName;

        // 4. Start Logic
        Manager.gridRecorder.StartRecording();
    }

    public override void UpdateState()
    {
        // Wait for 'B' Button
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            Manager.ChangeState(new AARState());
        }
    }

    public override void ExitState()
    {
        Manager.gridRecorder.StopRecording();

        // Turn off the Exception
        if (Manager.visionScanner != null)
        {
            Manager.visionScanner.gameObject.SetActive(false);
        }
    }
}