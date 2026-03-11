using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyAARCard : MonoBehaviour
{
    public Image headOverlay;
    public Image bodyOverlay;
    public Image legOverlay;

    public Sprite headRed;
    public Sprite bodyRed;
    public Sprite legRed;

    public Sprite headBlack;
    public Sprite bodyBlack;
    public Sprite legBlack;

    public TextMeshProUGUI hitsText;
    public TextMeshProUGUI enemyName;

    public TextMeshProUGUI headShots;
    public TextMeshProUGUI bodyShots;
    public TextMeshProUGUI legShots;
   
    public void Setup(string enemy,EnemyHitData hitData, int index)
    {
        hitsText.text = "Total " + hitData.hitCount.ToString() + " shots hit";
        enemyName.text ="Enemy " + index.ToString() + ": " + enemy;

        headOverlay.sprite = hitData.headShot > 0 ? headRed : headBlack;
        bodyOverlay.sprite = hitData.bodyShot > 0 ? bodyRed : bodyBlack;
        legOverlay.sprite = hitData.legShot > 0 ? legRed : legBlack;

        headShots.text = hitData.headShot.ToString();
        bodyShots.text = hitData.bodyShot.ToString();
        legShots.text = hitData.legShot.ToString();
    }
}
