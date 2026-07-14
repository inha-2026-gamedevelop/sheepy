// Unity
using UnityEngine;

namespace Minsung.Sound
{
    // 씬에 배치해두면 오브젝트가 처음 활성화될 때 지정된 BGM을 자동 재생한다 (맵 BGM용)
    public class MapBgmPlayer : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("재생할 BGM")]
        [SerializeField] private EBgm  _bgm    = EBgm.Map1;
        [SerializeField] private bool  _isLoop = true;
        [SerializeField] private float _pitch  = 1f;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Start()
        {
            SoundManager.Instance?.PlayBGM(_bgm, _isLoop, _pitch);
        }
    }
}
