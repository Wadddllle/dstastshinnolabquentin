using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class DamagedVignette : MonoBehaviour
{
    public Image vignette;
    public static DamagedVignette Instance;
    private float intensity = 0f;
    public float maxIntensity = 1f;
    public float fadeSpeed = 2f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    // Update is called once per frame
    void Update()
    {
        if (vignette != null) 
        { 
            intensity  = Mathf.Lerp(intensity, 0f, Time.deltaTime*fadeSpeed);
            Color color = vignette.color;
            color.a = intensity;
        }
    }

    public void OnPlayerDamageTaken()
    {
        intensity = maxIntensity;
    }
}
