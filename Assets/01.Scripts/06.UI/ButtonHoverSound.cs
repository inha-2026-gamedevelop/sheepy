// Unity
using UnityEngine;
using UnityEngine.EventSystems; // UI 이벤트를 받기 위해 필수

using Minsung.Sound;

// 라디오 상호작용 오브젝트. 상호작용 키를 누를 때마다 사운드 재생/정지를 토글한다.
public class ButtonHoverSound : MonoBehaviour, IPointerEnterHandler
{
    /****************************************
    *                Fields
    ****************************************/

    [Header("버튼 Hover 효과음 설정")]
    [SerializeField] private EBgm _bgm = EBgm.UIsfx;    // 재생할 트랙을 고르는 카테고리. SFX 채널로 재생되며 실제 BGM은 건드리지 않는다
    [SerializeField] private int _clipIndex = -1;       // 카테고리 내 클립 인덱스 (-1이면 무작위, 커스텀 인스펙터에서 드롭다운으로 선택)
    private bool _isLoop = false;

    private LocalSfxEmitter _sfxEmitter;

    /****************************************
    *              Unity Event
    ****************************************/
    public void OnPointerEnter(PointerEventData eventData)
    {
        _sfxEmitter?.PlayActivate();

        if (SoundManager.Instance == null)
        {
            return;
        }

        AudioClip clip = SoundManager.Instance.GetBgmClip(_bgm, _clipIndex);
        if (clip == null)
        {
            return;
        }

        SoundManager.Instance.PlaySFX_Duration(clip, GetInstanceID(), 1f, _isLoop);
    }
}
