using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DistanceLaserUI : MonoBehaviour
{
    public DistanceGaugeLaser laser;
    public Slider slider;
    public TextMeshProUGUI sliderValue;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        slider.value = laser.distance;
        slider.onValueChanged.AddListener(OnRangeChange);

        UpdateRangeLabel();
    }

    void OnRangeChange(float range)
    {
        laser.distance = range;   
        sliderValue.text = range.ToString("F1");
    }
    // Update is called once per frame
    void UpdateRangeLabel()
    {
        sliderValue.text = laser.distance.ToString("F1");
    }
}
