using UnityEngine;


[CreateAssetMenu(fileName = "BossDB", menuName = "GameDB/Monster/Boss", order = int.MaxValue)]
public class ExBossDataSO : ScriptableObject
{
    [Header("Boss Setting")]
    public int nBossMaxHP = 64000;
    public float fBossPhaseOffset = 0.25f;



}
