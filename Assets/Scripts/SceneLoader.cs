using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Tooltip("The name of the scene to load when the Context Menu item is pressed.")]
    public string sceneName = "DefaultSceneName"; // Initialize with a default value

    // The [ContextMenu] attribute adds a button to the component's context menu in the Inspector.
    // We'll call this Load Scene (from Inspector Var)
    [ContextMenu("Load Scene (from Inspector Var)")]
    public void LoadSceneFromInspector()
    {
        // Use the public variable 'sceneName' set in the Inspector
        string sceneToLoad = sceneName;

        // This line will print a message to the console in Unity
        Debug.Log("Attempting to load scene: " + sceneToLoad);

        // Check if the scene name is empty
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("Scene name is empty! Please set the 'Scene Name' variable in the Inspector.");
            return;
        }

        // Load the scene using the variable
        SceneManager.LoadScene(sceneToLoad);
    }
}