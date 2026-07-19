// Unity
using UnityEngine;
using UnityEngine.UI;

using Minsung.Common;

namespace Minsung.Player
{
    // 하트 HUD. 체력 변화에 맞춰 하트 이미지를 채움/반칸/빈칸 스프라이트로 갱신한다.
    public class PlayerHeartUI : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private PlayerHealth _playerHealth; // 구독할 체력 컴포넌트 (본체)
        [SerializeField] private Image[] _heartImages;       // 하트 슬롯 이미지들 (왼쪽부터 순서대로)
        [SerializeField] private Sprite _filledSprite;
        [SerializeField] private Sprite _halfSprite;         // 반 칸 하트 (미지정 시 빈 하트로 표시)
        [SerializeField] private Sprite _emptySprite;

        /****************************************
        *              Unity Event
        ****************************************/

        private void OnEnable()
        {
            if (_playerHealth == null)
            {
                return;
            }
            _playerHealth.OnHealthChanged += Redraw;
            Redraw(_playerHealth.CurrentHalves, _playerHealth.MaxHalves);
        }

        // 씬 로드 시 UI가 플레이어보다 먼저 깨어나면 OnEnable 시점의 MaxHalves가 0이라
        // 하트가 전부 숨겨진다 - 모든 Awake가 끝난 Start에서 한 번 더 그린다
        private void Start()
        {
            // 인스펙터 미지정이면 씬의 본체에서 자동 연결 (HUD 프리팹 드롭인용, 분신은 PlayerController가 없어 제외됨)
            if (_playerHealth == null)
            {
                PlayerController player = FindAnyObjectByType<PlayerController>();
                if ((player != null) && player.TryGetComponent(out PlayerHealth health))
                {
                    _playerHealth = health;
                    _playerHealth.OnHealthChanged += Redraw;
                }
            }

            if (_playerHealth == null)
            {
                return;
            }
            Redraw(_playerHealth.CurrentHalves, _playerHealth.MaxHalves);
        }

        private void OnDisable()
        {
            if (_playerHealth == null)
            {
                return;
            }
            _playerHealth.OnHealthChanged -= Redraw;
        }

        /****************************************
        *                Methods
        ****************************************/

        // 반칸 수 기준으로 각 하트를 채움/반칸/빈칸으로 갱신.
        // max를 넘는 슬롯은 숨겨 최대 하트 수 변화에도 대응
        private void Redraw(int currentHalves, int maxHalves)
        {
            int halvesPerHeart = Constants.Player.HALVES_PER_HEART;

            for (int i = 0; i < _heartImages.Length; ++i)
            {
                int fullThreshold = (i + 1) * halvesPerHeart;

                Sprite sprite;
                if (currentHalves >= fullThreshold)
                {
                    sprite = _filledSprite;
                }
                else if ((currentHalves == fullThreshold - 1) && (_halfSprite != null))
                {
                    sprite = _halfSprite;
                }
                else
                {
                    sprite = _emptySprite;
                }

                _heartImages[i].sprite  = sprite;
                _heartImages[i].enabled = i < (maxHalves / halvesPerHeart);
            }
        }
    }
}
