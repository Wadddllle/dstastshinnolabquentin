using UnityEngine;
using System.Collections.Generic;

public class AARManager : MonoBehaviour
{
    private Dictionary<GameObject, EnemyHitData> enemyHits;
    private List<GameObject> enemyKeys;
    public EnemyAARCard aarCard;
    private int index = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoadData();
    }
    void LoadData()
    {
        enemyHits = SessionManager.Instance.enemyHits;
        enemyKeys = new List<GameObject>(enemyHits.Keys);
    }
    void Update()
    {
        //ShowCurrentEnemy();

    }
    void ShowCurrentEnemy()
    {
        if (enemyHits.Count == 0)
            return;
        GameObject key = enemyKeys[index];
        EnemyHitData hitData = enemyHits[key];
        aarCard.Setup(key,hitData);
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