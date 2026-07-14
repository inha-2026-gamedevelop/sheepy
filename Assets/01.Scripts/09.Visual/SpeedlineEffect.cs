// Unity
using UnityEngine;

namespace Minsung.Visual
{
    // 돌진/대시/분신 역재생 시 스피드라인 연출.
    // speedline-sheet0 스프라이트를 캐릭터에 붙여 재생.
    [AddComponentMenu("Minsung/Speedline Effect")]
    public class SpeedlineEffect : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("스피드라인 설정")]
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Sprite[]       _frames;            // speedline 스프라이트 배열
        [SerializeField] private float          _fps            = 24f;
        [SerializeField] private Color          _color          = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private bool           _flipWithPlayer = true;
        [SerializeField] private SpriteRenderer _playerRenderer; // 플레이어 SpriteRenderer 참조 (flipX 동기화용)

        private bool  _playing;
        private int   _frameIndex;
        private float _timer;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            if (_renderer == null) _renderer = GetComponentInChildren<SpriteRenderer>();
            Stop();
        }

        private void Update()
        {
            if (!_playing || _frames == null || _frames.Length == 0) return;

            _timer += Time.deltaTime;
            if (_timer >= 1f / _fps)
            {
                _timer = 0f;
                ++_frameIndex;
                if (_frameIndex >= _frames.Length) _frameIndex = 0;
                _renderer.sprite = _frames[_frameIndex];
            }

            if (_flipWithPlayer && _playerRenderer != null)
            {
                // 플레이어 SpriteRenderer의 flipX를 그대로 따라감 (2D 방향 전환)
                _renderer.flipX = _playerRenderer.flipX;
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 스피드라인 재생 시작 (첫 프레임부터 루프). </summary>
        public void Play()
        {
            _playing    = true;
            _frameIndex = 0;
            _timer      = 0f;
            _renderer.color   = _color;
            _renderer.enabled = true;
        }

        /// <summary> 재생 정지 + 숨김. </summary>
        public void Stop()
        {
            _playing          = false;
            _renderer.enabled = false;
        }
    }
}