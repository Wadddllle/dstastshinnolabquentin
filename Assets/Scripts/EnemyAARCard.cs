using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyAARCard : MonoBehaviour
{
    public Image headOverlay;
    public Image bodyOverlay;
    public Image legOverlay;
    public TextMeshProUGUI hitsText;
    public TextMeshProUGUI enemyName;
   
    public void Setup(GameObject enemy,EnemyHitData hitData)
    {
        if (hitData.headShot)
            headOverlay.color = Color.red;
        if (hitData.bodyShot)
            bodyOverlay.color = Color.red;
        if (hitData.legShot)
            legOverlay.color = Color.red;

        hitsText.text = hitData.hitCount.ToString() + " shots hit";
        enemyName.text = enemy.name;
    }
}
