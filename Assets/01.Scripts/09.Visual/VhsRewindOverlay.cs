// Unity
using UnityEngine;
using UnityEngine.UI;

using System.Collections;

using Minsung.Utility;

namespace Minsung.Visual
{
    // 되감기 중 화면 전체를 덮는 VHS 글리치 연출.
    [AddComponentMenu("Minsung/VHS Rewind Overlay")]
    [RequireComponent(typeof(Image))]
    public class VhsRewindOverlay : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("VHS 오버레이 설정")]
        [SerializeField] private Image    _image;
        [SerializeField] private Sprite[] _frames; // vhs-sheet0_*, vhs-sheet1_* 슬라이스 (세로로 잘린 원본)
        [SerializeField] private float    _fps    = 24f;
        [SerializeField] private Color    _color  = new Color(1f, 1f, 1f, 0.8f);
        [SerializeField] private bool     _rotate90 = true; // 세로 프레임을 가로(1920x1080)로 펴서 표시

        private RectTransform _rect;
        private bool  _playing;    // 재생 중 여부
        private int   _frameIndex; // 현재 표시 중인 프레임 인덱스
        private float _timer;      // 다음 프레임까지 누적 시간
        private Coroutine _coPlay; // 프레임 재생 코루틴

        // 재생을 계속해도 되는 상태인지 (Stop 호출 또는 프레임 미지정 시 false).
        private bool ShouldKeepPlaying()
        {
            return _playing && (_frames != null) && (_frames.Length > 0);
        }

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            if (_image == null)
            {
                _image = GetComponent<Image>();
            }
            _rect = _image.rectTransform;
            _image.preserveAspect = false;

            if (_rotate90)
            {
                // 세로 소스를 90도 돌려서 화면 전체(가로)를 덮도록 배치.
                // 회전 전 rect 크기를 화면 크기의 가로/세로를 바꿔서 잡아야
                // 회전 후 바운딩 박스가 정확히 화면 크기가 된다.
                RectTransform canvasRect = GetComponentInParent<Canvas>().rootCanvas.GetComponent<RectTransform>();
                Vector2 screenSize = canvasRect.rect.size;

                _rect.anchorMin        = new Vector2(0.5f, 0.5f);
                _rect.anchorMax        = new Vector2(0.5f, 0.5f);
                _rect.pivot            = new Vector2(0.5f, 0.5f);
                _rect.anchoredPosition = Vector2.zero;
                _rect.sizeDelta        = new Vector2(screenSize.y, screenSize.x);
                _rect.localRotation    = Quaternion.Euler(0f, 0f, 90f);
            }
            else
            {
                // 화면 전체를 덮도록 강제 스트레치
                _rect.anchorMin = Vector2.zero;
                _rect.anchorMax = Vector2.one;
                _rect.offsetMin = Vector2.zero;
                _rect.offsetMax = Vector2.zero;
            }

            Stop();
        }


        /****************************************
        *                Methods
        ****************************************/

        /// <summary> VHS 오버레이 재생 시작. 프레임이 지정되지 않았으면 무시. </summary>
        public void Play()
        {
            if ((_frames == null) || (_frames.Length == 0))
            {
                return;
            }

            _playing    = true;
            _frameIndex = 0;
            _timer      = 0f;
            _image.color   = _color;
            _image.sprite  = _frames[0];
            _image.enabled = true;

            UtilCoroutine.CheckRunCoroutine(ref _coPlay, StartCoroutine(CoPlay()), this);
        }

        // 지정 fps 간격으로 프레임을 순환 재생. Stop()이 호출되면 루프가 끝난다.
        private IEnumerator CoPlay()
        {
            while (ShouldKeepPlaying())
            {
                _timer += Time.deltaTime;
                if (_timer >= (1f / _fps))
                {
                    _timer = 0f;
                    ++_frameIndex;
                    if (_frameIndex >= _frames.Length)
                    {
                        _frameIndex = 0; // 마지막 프레임 다음은 처음으로 (루프)
                    }
                    _image.sprite = _frames[_frameIndex];
                }

                yield return null;
            }

            _coPlay = null;
        }

        /// <summary> 재생 정지 + 오버레이 숨김. </summary>
        public void Stop()
        {
            _playing       = false;
            _image.enabled = false;

            UtilCoroutine.CheckStopCoroutine(ref _coPlay, this);
        }
    }
}
