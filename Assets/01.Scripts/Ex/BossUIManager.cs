using UnityEngine;
using UnityEngine.UI;

public class BossUIManager : MonoBehaviour
{

    [Header("보스 상태 슬롯")]
    [SerializeField] private BossConditionSlotUI _conditionSlots;
    [SerializeField] private Slider sliderBossHP;

    [SerializeField] private GameObject[] goHpBars; // 페이즈에 따라 HP Bar 비활성화 (총 3개)

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void Init()
    {
        _conditionSlots.Init();
    }

    public void UpdateBossHP(float _fCurHP, float _fMaxHP)
    {
        if(sliderBossHP == null) return;

        sliderBossHP.value = (_fMaxHP > 0f) ? Mathf.Clamp01(_fCurHP / _fMaxHP) : 0f;
    }

    public void UpdateSlot(EBossCondition condition, float value)
    {
        if(_conditionSlots == null) return;

        _conditionSlots.UpdateSlot(condition);
    }

    public void SetActiveBossHPBar(int index)
    {
        if(goHpBars == null) return;

        for (int i = 0; i < goHpBars.Length; i++)
        {
            if(goHpBars[i] == null) continue;

            goHpBars[i].SetActive(i == index);
        }
    }

 }
