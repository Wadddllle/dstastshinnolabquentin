using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class DamagedVignette : MonoBehaviour
{
    public Image image;
    public static DamagedVignette Instance;
    public float maxAlpha = 0.8f;
    public float flashDuration = 0.5f;
    Coroutine currentRoutine = null;
    void Awake()
    {
        Instance = this;
    }

    public void StartFlash(float seconds, float maxAlpha, Color newColor)
    {
        image.color = newColor;
        maxAlpha = Mathf.Clamp(maxAlpha, 0f, 1f);

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(Flash(seconds, maxAlpha));
    }

    public void OnPlayerDamageTaken()
    {
        StartFlash(flashDuration, maxAlpha, Color.red);
        Debug.Log("Player took damage");
    }

    IEnumerator Flash(float seconds, float maxAlpha)
    {
        float flashHalf = seconds / 2;
        for (float t = 0; t <= flashHalf; t += Time.deltaTime) //flash in half duration
        {
            Color thisFrameColor = image.color;
            thisFrameColor.a = Mathf.Lerp(0, maxAlpha, t / flashHalf);
            image.color = thisFrameColor;
            yield return null;
        }

        for (float t = 0; t <= flashHalf; t += Time.deltaTime) //flash out half duration
        {
            Color thisFrameColor = image.color;
            thisFrameColor.a = Mathf.Lerp(maxAlpha, 0, t / flashHalf);
            image.color = thisFrameColor;
            yield return null;
        }
        image.color = new Color(0, 0, 0, 0);
    }
}
