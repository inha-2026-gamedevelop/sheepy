using UnityEngine;

using UnityEngine.UI;

public enum EBossCondition
{
    검정,
    하양,
    남색,
    핑크,
    파랑,
    화남,
    NONE,

}
public class BossConditionSlotUI : MonoBehaviour
{
    public GameObject goFace;

    public Image imgFace;
    public Sprite[] spFaces;



    public void Init()
    {
        if (imgFace == null)
        {
            imgFace = GetComponentInChildren<Image>(true);
        }
        UpdateSlot(EBossCondition.NONE);
        goFace.SetActive(false);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void UpdateSlot(EBossCondition _eCondition)
    {
        if(_eCondition == EBossCondition.NONE)
        {
            imgFace.sprite = null;
            return;
        }

        if(spFaces.Length <= (int)_eCondition) return; //배열의 크기보다 많다면 return;

        imgFace.sprite = spFaces[(int)_eCondition];
    }
}
