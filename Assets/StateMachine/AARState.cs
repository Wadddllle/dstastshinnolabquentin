using UnityEngine;

public class AARState : BaseState
{
    public override void EnterState()
    {
        Debug.Log("STATE: AAR Mode");

        // 1. Activate the Canvas/GameObject that holds the Visualizer
        // (Assuming aarVisualizer is on the UI Canvas you want to show)
        Manager.aarVisualizer.gameObject.SetActive(true);

        // 2. Get the filename from the Blackboard (SessionData)
        string fileToLoad = Manager.currentSession.currentRecordingFileName;

        // 3. Initialize the Visualizer with that file
        Manager.aarVisualizer.Initialize(fileToLoad);
    }

    public override void UpdateState()
    {
        // CONTROL LOGIC: Map Controller Input to the Visualizer

        // Example: Use Left Thumbstick X (-1 to 1) to scrub the timeline
        // We add a little speed multiplier so it scrubs nicely
        //float input = Input.GetAxis("Horizontal"); // Replace with OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).x

        //if (Mathf.Abs(input) > 0.1f)
        //{
        //    float newProgress = Manager.aarVisualizer.playbackProgress + (input * Time.deltaTime * 0.5f);
        //    Manager.aarVisualizer.playbackProgress = Mathf.Clamp01(newProgress);
        //}

        //// Example: Press Button 'A' to toggle Heatmap
        //if (Input.GetKeyDown(KeyCode.Space)) // Replace with OVRInput.GetDown(OVRInput.Button.One)
        //{
        //    Manager.aarVisualizer.showHeatmap = !Manager.aarVisualizer.showHeatmap;
        //}

        //// TRANSITION: Press 'B' to Restart
        //if (Input.GetKeyDown(KeyCode.R)) // Replace with OVRInput.GetDown(OVRInput.Button.Two)
        //{
        //    Manager.ChangeState(new InstructorState());
        //}
    }

    public override void ExitState()
    {
        // Hide the visualizer when we leave
        Manager.aarVisualizer.gameObject.SetActive(false);
    }
}