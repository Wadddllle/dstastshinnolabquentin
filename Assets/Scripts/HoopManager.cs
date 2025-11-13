using UnityEngine;
using TMPro; // Required for using TextMeshPro UI elements

public class HoopManager : MonoBehaviour
{
    // --- Public variables to assign in the Inspector ---
    [Header("Game Objects")]
    public GameObject ball; // Drag your Ball GameObject here
    public Transform playerHead; // Drag your OVRCameraRig's CenterEyeAnchor here

    [Header("UI Elements")]
    public TextMeshProUGUI scoreText; // Drag your ScoreText UI element here

    // --- Private variables ---
    private int score = 0;
    private bool canScore = true; // Prevents scoring multiple times on one shot

    // This function is called when the script first loads
    void Start()
    {
        UpdateScoreUI();
    }

    // This function is called whenever something enters the trigger collider
    public void RegisterScore()
    {
        if (!canScore) return;
        canScore = false;
        score++;
        UpdateScoreUI();

    }

    public void ResetCanScore()
    {
        canScore = true;
    }


    // A simple function to keep the UI text updated
    void UpdateScoreUI()
    {
        scoreText.text = "Score: " + score;
    }

    // --- This is the function the button will call ---
    public void TeleportBallToPlayer()
    {
        if (ball != null && playerHead != null)
        {
            // Position the ball 1 meter in front of the player's head
            ball.transform.position = playerHead.position + playerHead.forward * 1.0f;

            // Stop the ball from moving/spinning when it teleports
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                ballRb.linearVelocity = Vector3.zero;
                ballRb.angularVelocity = Vector3.zero;
            }
        }
    }
}