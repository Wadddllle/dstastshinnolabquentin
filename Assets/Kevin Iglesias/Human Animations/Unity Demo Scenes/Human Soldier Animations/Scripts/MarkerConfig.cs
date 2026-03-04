using UnityEngine;

[CreateAssetMenu(fileName = "MarkerConfig", menuName = "Scriptable Objects/MarkerConfig")]
public class MarkerConfig : ScriptableObject
{
    [Range(0f, 1f)]
    public float chance = 0.5f;
}
