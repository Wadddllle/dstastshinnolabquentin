using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemySpawnManagerUI : MonoBehaviour
{
    public EnemySpawnManager spawnManager;
    public Slider slider;
    public TextMeshProUGUI sliderValue;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        slider.value = spawnManager.chance;
        slider.onValueChanged.AddListener(OnChanceChange);

        UpdateChanceLabel();
    }

    void OnChanceChange(float chance)
    {
        spawnManager.chance = chance;
        sliderValue.text = (chance*100).ToString("F1") + "%";
    }
    // Update is called once per frame
    void UpdateChanceLabel()
    {
        sliderValue.text = (spawnManager.chance*100).ToString("F1") + "%";
    }
}
