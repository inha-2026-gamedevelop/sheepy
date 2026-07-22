// Unity
using UnityEngine;

using Minsung.Player;

namespace Minsung.Sound
{
    // 플레이어가 이 트리거(Collider2D, IsTrigger)를 지나가면 지정한 BGM으로 전환한다
    // A - Trigger - B 구조에서 A->B 진입 시 지정 BGM(B)으로, B->A 재진입 시 진입 전 BGM(A)으로 되돌아간다 (진입 시점에 방향을 판별, Exit는 사용하지 않는다)
    [RequireComponent(typeof(Collider2D))]
    public class BgmZoneTrigger : MonoBehaviour
    {
        [Header("전환할 BGM")]
        [SerializeField] private EBgm  _bgm       = EBgm.Map1;
        [SerializeField] private int   _clipIndex = -1; // 카테고리 내 클립 인덱스 (-1이면 무작위)
        [SerializeField] private bool  _isLoop    = true;
        [SerializeField] private float _pitch     = 1f;

        private EBgm _previousBgm;
        private bool _hasPreviousBgm;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if ((SoundManager.Instance == null) || !other.TryGetComponent(out PlayerController _))
            {
                return;
            }

            EBgm currentBgm = SoundManager.Instance.CurrentBgm;

            if (_hasPreviousBgm && (currentBgm == _bgm))
            {
                SoundManager.Instance.PlayBGM(_previousBgm);
                _hasPreviousBgm = false;
                return;
            }

            _previousBgm    = currentBgm;
            _hasPreviousBgm = true;
            SoundManager.Instance.PlayBGM(_bgm, _clipIndex, _isLoop, _pitch);
        }
    }
}
