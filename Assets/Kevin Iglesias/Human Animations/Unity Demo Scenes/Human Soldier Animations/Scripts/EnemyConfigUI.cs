using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class EnemyConfigUI : MonoBehaviour
{
    public EnemyConfig config;

    [Header("Ranges")]
    public Slider detectionRangeSlider;
    public TextMeshProUGUI detectionValue;
    public Slider eyeRangeSlider;
    public TextMeshProUGUI eyeRangeValue;
    public Slider attackRangeSlider;
    public TextMeshProUGUI attackRangeValue;
    public Slider peripheralAngleSlider;
    public TextMeshProUGUI peripheralAngleValue;

    [Header("Bullet")]
    public Slider shot_cooldownSlider;
    public TextMeshProUGUI shot_cooldownValue;

    [Header("Reaction")]
    public Slider reactionTime_offguardSlider;
    public TextMeshProUGUI reactionOffguardValue;
    public Slider reactionTime_awareSlider;
    public TextMeshProUGUI reactionAwareValue;

    [Header("Movement")]
    public Slider movementSpeedSlider;
    public TextMeshProUGUI movementSpeedValue;
    public Slider turningSpeedSlider;
    public TextMeshProUGUI turningSpeedValue;

    void Start()
    {
        detectionRangeSlider.value = config.detectionRange;
        eyeRangeSlider.value = config.eyeRange;
        attackRangeSlider.value = config.attackRange;
        peripheralAngleSlider.value = config.peripheralAngle;

        shot_cooldownSlider.value = config.shot_cooldown;

        reactionTime_offguardSlider.value = config.reactionTime_offGuard;
        reactionTime_awareSlider.value = config.reactionTime_aware;

        movementSpeedSlider.value = config.movementSpeed;
        turningSpeedSlider.value = config.turningSpeed;

        //listeners
        detectionRangeSlider.onValueChanged.AddListener(OnDetectionRangeChange);
        eyeRangeSlider.onValueChanged.AddListener(OnEyeRangeChange);
        attackRangeSlider.onValueChanged.AddListener(OnAttackRangeChange);
        peripheralAngleSlider.onValueChanged.AddListener(OnPeripheralAngleChange);

        shot_cooldownSlider.onValueChanged.AddListener(OnShotCooldownChange);

        reactionTime_offguardSlider.onValueChanged.AddListener(OnReactionOffguardChange);
        reactionTime_awareSlider.onValueChanged.AddListener(OnReactionAwareChange);

        movementSpeedSlider.onValueChanged.AddListener(OnMovementSpeedChange); 
        turningSpeedSlider.onValueChanged.AddListener(OnTurningSpeedChange);

        //Update text values
        UpdateAllValueLabels();
    }

    void OnDetectionRangeChange(float value)
    {
        config.detectionRange = value;
        detectionValue.text = value.ToString("F1");
    }

    void OnEyeRangeChange(float value)
    {
        config.eyeRange = value;
        eyeRangeValue.text = value.ToString("F1");
    }

    void OnAttackRangeChange(float value)
    {
        config.attackRange = value;
        attackRangeValue.text = value.ToString("F1");
    }

    void OnPeripheralAngleChange(float value)
    {
        config.peripheralAngle = value;
        peripheralAngleValue.text = value.ToString("F1");
    }

    void OnShotCooldownChange(float value)
    {
        config.shot_cooldown = value;
        shot_cooldownValue.text = value.ToString("F1");
    }

    void OnReactionOffguardChange(float value)
    {
        config.reactionTime_offGuard = value;
        reactionOffguardValue.text = value.ToString("F1");
    }

    void OnReactionAwareChange(float value)
    {
        config.reactionTime_aware = value;
        reactionAwareValue.text = value.ToString("F1");
    }

    void OnMovementSpeedChange(float value)
    {
        config.movementSpeed = value;
        movementSpeedValue.text = value.ToString("F1");
    }

    void OnTurningSpeedChange(float value)
    {
        config.turningSpeed = value;
        turningSpeedValue.text = value.ToString("F1");
    }

    void UpdateAllValueLabels()
    {
        detectionValue.text = config.detectionRange.ToString("F1");
        eyeRangeValue.text = config.eyeRange.ToString("F1");
        attackRangeValue.text= config.attackRange.ToString("F1");
        peripheralAngleValue.text = config.peripheralAngle.ToString("F1");
        shot_cooldownValue.text = config.shot_cooldown.ToString("F1");
        reactionOffguardValue.text = config.reactionTime_offGuard.ToString("F1");
        reactionAwareValue.text = config.reactionTime_aware.ToString("F1");
        movementSpeedValue.text = config.movementSpeed.ToString("F1");
        turningSpeedValue.text = config.turningSpeed.ToString("F1");
    }
}
