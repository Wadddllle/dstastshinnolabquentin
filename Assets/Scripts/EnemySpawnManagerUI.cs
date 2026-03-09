using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemySpawnManagerUI : MonoBehaviour
{
    public MarkerConfig markerConfig;
    public Slider slider;
    public TextMeshProUGUI sliderValue;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        slider.value = markerConfig.chance * 10f;
        slider.onValueChanged.AddListener(OnChanceChange);

        UpdateChanceLabel();
    }

    void OnChanceChange(float chance)
    {
        markerConfig.chance = chance * 0.1f;
        sliderValue.text = (markerConfig.chance*100).ToString("F1") + "%";
    }
    // Update is called once per frame
    void UpdateChanceLabel()
    {
        sliderValue.text = (markerConfig.chance*100).ToString("F1") + "%";
    }
}
