using System;
using Minsung.Boss;
using Unity.VisualScripting;
using UnityEngine;

public class BossManager : MonoBehaviour
{

    [SerializeField] private BossUIManager bossUIMgr;

    [SerializeField] private BossController bossCtr;

    [SerializeField] private ExBossDataSO bossDataSO;

    private void Awake()
    {
        bossUIMgr.Init();

    }

    private void OnEnable()
    {
       
    }

    public void BossStageStart()
    {
        bossCtr.OnHealthChanged += bossUIMgr.UpdateBossHP;
    }

    public void BossClear()
    {
        bossCtr.OnHealthChanged -= bossUIMgr.UpdateBossHP;
    }


 
}
