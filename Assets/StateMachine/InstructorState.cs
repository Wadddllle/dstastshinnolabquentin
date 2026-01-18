using UnityEngine;

public class InstructorState : BaseState
{
    public override void EnterState()
    {
        Debug.Log("STATE: Instructor Mode");

        // 1. The Magic Swap
        Manager.SetActiveRoot(Manager.InstructorRoot);

        // 2. Data Cleanup
        Manager.tacticalGrid.ClearGrid();
    }

    public override void UpdateState()
    {
        // Wait for 'A' Button
        //if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        //{
        //    FinishSetup();
        //}
    }

    public void FinishSetup()
    {
        Manager.mapGenerator.CaptureSnapshot();
        Manager.ChangeState(new TraineeState());
    }

    public override void ExitState()
    {
        // No need to manually turn off tools. 
        // SetActiveRoot in the next state will turn off Manager.InstructorRoot automatically!
    }
}