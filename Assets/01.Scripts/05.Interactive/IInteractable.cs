// Unity
using UnityEngine;

namespace Minsung.Interactive
{
    // 플레이어가 상호작용(E키)할 수 있는 오브젝트 공통 계약 (레버 / 라디오 / NPC / 아이템 ...).
    public interface IInteractable
    {
        /// <summary> 상호작용 실행 (E키). interactor = 상호작용을 시작한 오브젝트(플레이어). </summary>
        void OnInteract(GameObject interactor);

        /// <summary> 감지 대상이 되었을 때 (하이라이트/키 가이드 표시). </summary>
        void OnFocus();

        /// <summary> 감지 대상에서 벗어났을 때 (하이라이트/키 가이드 해제). </summary>
        void OnUnfocus();

        /// <summary> 거리 판정 기준 트랜스폼. </summary>
        Transform GetTransform();
    }
}
