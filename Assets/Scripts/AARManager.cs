using UnityEngine;
using System.Collections.Generic;

public class AARManager : MonoBehaviour
{
    private Dictionary<string, EnemyHitData> enemyHits;
    private List<string> enemyKeys;
    public EnemyAARCard aarCard;
    private int index;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoadData();
    }
    void LoadData()
    {
        enemyHits = AppManager.Instance.enemyHits;
        enemyKeys = new List<string>(enemyHits.Keys);
    }

    void ShowCurrentEnemy()
    {
        if (enemyHits.Count == 0)
            return;
        string key = enemyKeys[index];
        EnemyHitData hitData = enemyHits[key];
        aarCard.Setup(hitData);
    }

    public void NextEnemy()
    {
        index++;
        if (index > enemyKeys.Count)
            index = 0;
        ShowCurrentEnemy();
    }

    public void PrevEnemy()
    {
        index--;
        if (index < 0)
            index = enemyKeys.Count;
        ShowCurrentEnemy();
    }
}