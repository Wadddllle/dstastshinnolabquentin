using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Scriptable Objects/EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    [Header("Ranges")]

    [Range(1f,10f)]
    public float detectionRange = 10f;

    [Range(1f, 10f)]
    public float eyeRange = 5f;

    [Range(1f, 10f)]
    public float attackRange = 5f;

    [Header("Bullet")]

    [Range(0.2f, 2f)]
    public float shot_cooldown = 1.3f;

    [Header("Reaction")]

    [Range(0.5f, 2f)]
    public float reactionTime_offGuard = 1.5f;

    [Range(0.5f, 2f)]
    public float reactionTime_aware = 0.5f;

    [Header("Movement")]

    [Range(1f, 5f)]
    public float movementSpeed = 3.5f;

    [Range(180f, 360f)]
    public float turningSpeed = 200f; //angular speed in deg/s
}
