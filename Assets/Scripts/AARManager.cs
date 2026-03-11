using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class AARManager : MonoBehaviour
{
    private Dictionary<string, EnemyHitData> enemyHits;
    private List<string> enemyKeys;
    public EnemyAARCard aarCard;
    private int index = 0;

    public TextMeshProUGUI debugText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Refresh()
    {
        index = 0;
        LoadData();
        ShowCurrentEnemy();
    }
    void LoadData()
    {
        enemyHits = SessionManager.Instance.enemyHits;
        enemyKeys = new List<string>(enemyHits.Keys);
    }
    
    void ShowCurrentEnemy()
    {
        if (enemyHits.Count == 0)
            return;
        string key = enemyKeys[index];
        EnemyHitData hitData = enemyHits[key];
        aarCard.Setup(key,hitData,index+1);
    }

    public void NextEnemy()
    {
        index++;
        if (index >= enemyKeys.Count)
            index = 0;
        ShowCurrentEnemy();
    }

    public void PrevEnemy()
    {
        index--;
        if (index < 0)
            index = enemyKeys.Count - 1;
        ShowCurrentEnemy();
    }

    public void Debugger(string msg)
    {
        debugText.text = msg;
    }
}