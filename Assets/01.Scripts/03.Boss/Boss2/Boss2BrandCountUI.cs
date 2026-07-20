// Unity
using UnityEngine;
using TMPro;

using Minsung.Player;

// 4페이즈 낙인 스택 표시 - 플레이어 머리 위에 count.png 배경 + "n/7" 텍스트를 띄운다
// PlayerInteractionSensor의 키 가이드 패널과 동일한 방식(월드 오프셋을 매 프레임 재적용, 회전 무시)을 직접 구현한다(Minsung 코드 수정 없이)
// 이 컴포넌트가 붙은 오브젝트 자체는 항상 활성 상태를 유지해야 한다(비활성화하면 Start/LateUpdate가 멈춰 구독이 끊긴다) - 실제 표시는 _visualRoot로 토글
public class Boss2BrandCountUI : MonoBehaviour
{
    /****************************************
    *                Fields
    ****************************************/

    [Header("참조")]
    [SerializeField] private Transform  _player;     // 미지정 시 자동 탐색
    [SerializeField] private GameObject _visualRoot; // count.png + 텍스트 - 기본 비활성([OFF])
    [SerializeField] private TMP_Text   _countText;

    private Boss2Health          _health;
    private Boss2BrandController _brandController;
    private Vector3 _offset;

    /****************************************
    *              Unity Event
    ****************************************/

    private void Awake()
    {
        if (_visualRoot != null)
        {
            _visualRoot.SetActive(false);
        }
    }

    private void Start()
    {
        if (_player == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            _player = (player != null) ? player.transform : null;
        }
        _offset = (_player != null) ? (transform.position - _player.position) : Vector3.zero;

        _health          = FindAnyObjectByType<Boss2Health>();
        _brandController = FindAnyObjectByType<Boss2BrandController>();

        if (_health != null)
        {
            _health.OnPhaseChanged += HandlePhaseChanged;
        }
        if (_brandController != null)
        {
            _brandController.OnStackChanged += HandleStackChanged;
        }
    }

    private void OnDestroy()
    {
        if (_health != null)
        {
            _health.OnPhaseChanged -= HandlePhaseChanged;
        }
        if (_brandController != null)
        {
            _brandController.OnStackChanged -= HandleStackChanged;
        }
    }

    private void LateUpdate()
    {
        if (_player == null)
        {
            return;
        }
        transform.SetPositionAndRotation(_player.position + _offset, Quaternion.identity);
    }

    /****************************************
    *                Methods
    ****************************************/

    private void HandlePhaseChanged(int phaseIndex)
    {
        if ((_visualRoot != null) && (_health != null) && _health.IsFinalPhase)
        {
            _visualRoot.SetActive(true);
        }
    }

    private void HandleStackChanged(int current, int max)
    {
        if (_countText != null)
        {
            _countText.text = $"{current}/{max}";
        }
    }
}
