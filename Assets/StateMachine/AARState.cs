using UnityEngine;

public class AARState : BaseState
{
    public override void EnterState()
    {
        Debug.Log("STATE: AAR Mode");

        // 1. The Magic Swap
        Manager.SetActiveRoot(Manager.AARRoot);

        // 2. Logic Setup
        string fileToLoad = Manager.currentSession.currentRecordingFileName;
        Manager.aarVisualizer.Initialize(fileToLoad);
    }

    public override void UpdateState()
    {
        // Scrubbing logic here...

        // Restart logic
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            Manager.ChangeState(new InstructorState());
        }
    }

    public override void ExitState()
    {
        // Root is auto-disabled by next state
    }
}