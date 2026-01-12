using UnityEngine;

public class InstructorState : BaseState
{
    public override void EnterState()
    {
        Debug.Log("<color=yellow>STATE: Instructor Mode Entered</color>");

        if (Manager == null)
        {
            Debug.LogError("Manager is NULL! The BaseState didn't get the reference.");
            return;
        }

        if (Manager.instructorTool == null)
        {
            Debug.LogError("instructorTool slot is EMPTY in the Inspector!");
            return;
        }

        // Attempt activation
        GameObject toolGO = Manager.instructorTool.gameObject;
        toolGO.SetActive(true);
        Manager.instructorTool.enabled = true;

        // VERIFY: Did Unity actually listen?
        Debug.Log($"Tool Verification: ActiveInHierarchy={toolGO.activeInHierarchy}, ComponentEnabled={Manager.instructorTool.enabled}");

        //Manager.currentSession.Reset();
        Manager.tacticalGrid.ClearGrid();
    
    }

    public override void UpdateState()
    {
        // Check for "A" button on Right Touch controller
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            FinishSetup();
        }
    }

    private void FinishSetup()
    {
        Debug.Log("Setup Complete. Capturing Map and switching...");
        Manager.mapGenerator.CaptureSnapshot();
        Manager.ChangeState(new TraineeState());
    }

    public override void ExitState()
    {
        // Turn the entire GameObject off so it disappears from the scene
        if (Manager.instructorTool != null)
        {
            Manager.instructorTool.gameObject.SetActive(false);
        }
    }
}