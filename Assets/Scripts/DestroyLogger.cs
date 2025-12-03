using UnityEngine;
using System;

public class DestroyLogger : MonoBehaviour
{
    void OnDestroy()
    {
        // We use LogWarning to make it stand out in the console.
        Debug.LogWarning($"--- DESTROY LOG for '{this.gameObject.name}' ---", this.gameObject);

        // This line is the magic. It prints the entire call stack, showing us
        // the exact sequence of methods that led to this OnDestroy call.
        Debug.Log(Environment.StackTrace);

        Debug.LogWarning($"--- END DESTROY LOG for '{this.gameObject.name}' ---");
    }
}