// Unity
using UnityEngine;

public class DisableOnLayerTrigger2D : MonoBehaviour
{
    /****************************************
    *              Unity Event
    ****************************************/
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 충돌한 오브젝트의 레이어가 3번인지 확인합니다.
        if (collision.gameObject.layer == 3)
        {
            gameObject.SetActive(false);
        }
    }
}
