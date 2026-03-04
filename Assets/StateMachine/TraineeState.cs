using UnityEngine;

public class TraineeState : BaseState
{
    private bool _isScenarioLive = false;
    public EnemySpawnManager spawnManager;

    public override void EnterState()
    {
        Debug.Log("STATE: Trainee Mode");

        // 1. Swap Roots (Turns off Instructor, turns on Trainee HUD)
        // This also auto-hides the Gun/Scanner via AppManager defaults
        AppManager.Instance.SetActiveRoot(AppManager.Instance.TraineeRoot);

        // 2. TURN ON EXCEPTIONS (Manual Override)
        if (AppManager.Instance.visionScanner != null)
        {
            AppManager.Instance.visionScanner.gameObject.SetActive(true);
            AppManager.Instance.visionScanner.enabled = true;
        }
        // Enable Gun Pivot
        if (AppManager.Instance.gunPivotRoot != null)
        {
            AppManager.Instance.gunPivotRoot.SetActive(true);
        }
        //if (AppManager.Instance.raycastWeapon != null)
        //{
        //    AppManager.Instance.raycastWeapon.gameObject.SetActive(true);
        //    AppManager.Instance.raycastWeapon.enabled = true;
        //}

        // 3. Wake Up Enemies (Colliders/Scripts)
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.PrepareForTrainee();
        }

        // 4. Start Recording Logic
        if (GridRecorder.Instance != null)
        {
            GridRecorder.Instance.StartRecording();
            Debug.Log("[Trainee State] Started Recording");
        }

        _isScenarioLive = true;
    }

    public override void UpdateState()
    {
        if (!_isScenarioLive) return;

        // INPUT: FIRE (Right Index Trigger)
        if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            //if (AppManager.Instance.raycastWeapon != null)
            //{
            //    AppManager.Instance.raycastWeapon.Fire();
            //}
        }

        // INPUT: FINISH (Right B Button)
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            FinishScenario();
        }
    }

    private void FinishScenario()
    {
        _isScenarioLive = false;

        // Log End
        if (GridRecorder.Instance != null)
        {
            GridRecorder.Instance.LogEvent("CLEAR", "Trainee called Clear", Vector3.zero);
            GridRecorder.Instance.StopRecording();
        }

        // Transition
        AppManager.Instance.ChangeState(new AARState());
    }

    public override void ExitState()
    {
        // Safety Cleanup
        if (GridRecorder.Instance != null) GridRecorder.Instance.StopRecording();

        // Note: SetActiveRoot in the NEXT state will automatically 
        // disable the Gun and Scanner, so we don't need to do it here.
    }
}