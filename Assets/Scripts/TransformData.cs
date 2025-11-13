using UnityEngine;

[System.Serializable] // Makes this class savable to JSON
public class TransformData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 localScale;
}