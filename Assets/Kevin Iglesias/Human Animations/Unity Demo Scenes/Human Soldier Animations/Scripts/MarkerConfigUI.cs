using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class MarkerConfigUI : MonoBehaviour
{
    public MarkerConfig markerConfig;

    public Slider markerChanceSlider;
    public TextMeshProUGUI markerChanceValue;

    void Start()
    {
        markerChanceSlider.value = markerConfig.chance;
        markerChanceSlider.onValueChanged.AddListener(OnChanceChange);
        UpdateLabel();
    }

    void OnChanceChange(float value)
    {
        markerConfig.chance = value;
        markerChanceValue.text = value.ToString("F0");
    }

    void UpdateLabel() 
    { 
        markerChanceValue.text = markerConfig.chance.ToString("F0");
    }
}
