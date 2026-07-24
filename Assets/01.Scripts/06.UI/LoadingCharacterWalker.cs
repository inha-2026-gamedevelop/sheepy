// Unity
using UnityEngine;
using UnityEngine.UI;

namespace Minsung.UI
{
    // 로딩 화면 하단에서 캐릭터가 좌->우로 걸어가는 연출 - 지정한 프레임을 순환 표시하며 오른쪽 끝에 닿으면 왼쪽으로 돌아가 반복한다
    [AddComponentMenu("Minsung/UI/Loading Character Walker")]
    public class LoadingCharacterWalker : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private Image    _image;
        [SerializeField] private Sprite[] _walkFrames;
        [SerializeField] private float    _frameRate = 8f;   // Move.anim 샘플링과 동일

        [Header("이동")]
        [SerializeField] private float _moveSpeed = 160f; // 캔버스 단위/초
        [SerializeField] private float _leftX     = -60f;
        [SerializeField] private float _rightX    = 1980f;

        private RectTransform _rectTransform;
        private float _frameElapsed;
        private int   _frameIndex;

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
        }

        private void OnEnable()
        {
            Vector2 pos = _rectTransform.anchoredPosition;
            pos.x = _leftX;
            _rectTransform.anchoredPosition = pos;

            _frameIndex   = 0;
            _frameElapsed = 0f;
            ApplyFrame();
        }

        private void Update()
        {
            TickMove();
            TickFrame();
        }

        /****************************************
        *                Methods
        ****************************************/

        private void TickMove()
        {
            Vector2 pos = _rectTransform.anchoredPosition;
            pos.x += _moveSpeed * Time.unscaledDeltaTime;
            if (pos.x > _rightX)
            {
                pos.x = _leftX;
            }
            _rectTransform.anchoredPosition = pos;
        }

        private void TickFrame()
        {
            if ((_image == null) || (_walkFrames == null) || (_walkFrames.Length == 0))
            {
                return;
            }

            _frameElapsed += Time.unscaledDeltaTime;
            float frameDuration = 1f / _frameRate;
            if (_frameElapsed < frameDuration)
            {
                return;
            }
            _frameElapsed -= frameDuration;

            _frameIndex = (_frameIndex + 1) % _walkFrames.Length;
            ApplyFrame();
        }

        private void ApplyFrame()
        {
            _image.sprite = _walkFrames[_frameIndex];
        }
    }
}
