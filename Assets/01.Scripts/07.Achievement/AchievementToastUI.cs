// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;
using UnityEngine.UI;

using Minsung.Sound;

namespace Minsung.Achievement
{
    // 업적 해제 시 화면 한쪽에 잠깐 떴다 사라지는 토스트(스팀 스타일).
    public class AchievementToastUI : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _descriptionText;

        [Header("타이밍")]
        [SerializeField] private float _fadeDuration = 0.3f;
        [SerializeField] private float _showDuration = 3f;

        private readonly Queue<AchievementData> _queue = new Queue<AchievementData>();
        private bool _isShowing;
        private AchievementManager _manager;     // 구독 해제용 캐시
        private WaitForSeconds _waitShow;        // 표시 유지 시간 캐시 (GC 방지)

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            _canvasGroup.alpha = 0f;
            _waitShow = new WaitForSeconds(_showDuration);
        }

        // AchievementManager는 AfterSceneLoad에 자동 생성되므로 Start 시점에는 항상 존재한다.
        private void Start()
        {
            _manager = AchievementManager.Instance;
            if (_manager != null)
            {
                _manager.OnAchievementUnlocked += Enqueue;
            }
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.OnAchievementUnlocked -= Enqueue;
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        // OnAchievementUnlocked 콜백 - 큐에 쌓고, 표시 루프가 안 돌고 있으면 시작.
        private void Enqueue(AchievementData data)
        {
            _queue.Enqueue(data);
            if (!_isShowing)
            {
                StartCoroutine(ShowQueueRoutine());
            }
        }

        // 큐가 빌 때까지 하나씩: 페이드 인 -> 유지 -> 페이드 아웃.
        private IEnumerator ShowQueueRoutine()
        {
            _isShowing = true;
            while (_queue.Count > 0)
            {
                AchievementData data = _queue.Dequeue();
                _iconImage.sprite = data.Icon;
                _titleText.text = data.Title;
                _descriptionText.text = data.Description;

                SoundManager.Instance?.PlaySFX(ESfxState.UI, (int)EUISfx.AchievementToast);

                yield return Fade(0f, 1f);
                yield return _waitShow;
                yield return Fade(1f, 0f);
            }
            _isShowing = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            float t = 0f;
            while (t < _fadeDuration)
            {
                t += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, t / _fadeDuration);
                yield return null;
            }
            _canvasGroup.alpha = to;
        }
    }
}
