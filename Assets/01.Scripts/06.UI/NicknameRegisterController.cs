// Unity
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using TMPro;

using Minsung.Backend;
using Minsung.Common;

namespace Minsung.UI
{
    // 닉네임 등록 씬 컨트롤러.
    // 입력한 이름을 서버에서 조회해서:
    //  - 없으면      → 현재 PC 기기값과 함께 신규 등록 후 다음 씬으로
    //  - 본인(같은 PC) → 로그인 처리 후 다음 씬으로
    //  - 다른 사람     → "이미 등록된 이름입니다." 안내
    [AddComponentMenu("Minsung/UI/Nickname Register Controller")]
    public class NicknameRegisterController : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("참조")]
        [SerializeField] private TMP_InputField _nameInput;
        [SerializeField] private Button         _registerButton;
        [SerializeField] private TMP_Text       _messageText; // 에러/안내 표시

        [Header("설정")]
        [SerializeField] private string _nextScene = Constants.Scene.MAIN_MENU; // 성공 시 이동할 씬
        [SerializeField] private int    _maxLength = 16;                        // 서버 username 길이 제한

        private bool _busy; // 중복 요청 방지

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            if (_registerButton != null)
            {
                _registerButton.onClick.AddListener(OnClickRegister);
            }
            if (_nameInput != null)
            {
                _nameInput.characterLimit = _maxLength;
                _nameInput.onSubmit.AddListener(_ => OnClickRegister()); // Enter 제출
            }
            SetMessage(string.Empty);
        }

        private void Start()
        {
            // 서버 권위: 이 기기(device_id)에 이미 등록된 계정이 있으면 등록을 건너뛰고 바로 다음 씬으로.
            if (BackendMirror.Instance == null)
            {
                SetBusy(false);
                SetMessage(string.Empty);
                return;
            }

            SetBusy(true);
            SetMessage("계정 확인 중...");

            BackendMirror.Instance.TryAutoLogin(
                onLoggedIn: GoToNextScene, // 이 기기에 계정 있음 → 메뉴로 바로 진입
                onNoAccount: () =>          // 계정 없음 → 등록 폼 노출
                {
                    SetBusy(false);
                    SetMessage(string.Empty);
                },
                onError: err =>            // 서버 확인 실패 → 수동 등록 허용
                {
                    SetBusy(false);
                    SetMessage("서버 확인에 실패했습니다. 이름을 등록해 주세요.");
                    Debug.LogWarning($"[NicknameRegister] 자동 로그인 실패: {err}");
                });
        }

        private void OnDestroy()
        {
            if (_registerButton != null)
            {
                _registerButton.onClick.RemoveListener(OnClickRegister);
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 등록 버튼(또는 Enter) - 이름 검증 후 서버 등록/로그인 시도. </summary>
        public void OnClickRegister()
        {
            if (_busy)
            {
                return;
            }

            string username = (_nameInput != null) ? _nameInput.text.Trim() : string.Empty;

            if (string.IsNullOrEmpty(username))
            {
                SetMessage("이름을 입력하세요.");
                return;
            }
            if (username.Length > _maxLength)
            {
                SetMessage($"이름은 최대 {_maxLength}자까지 가능합니다.");
                return;
            }
            if (BackendMirror.Instance == null)
            {
                SetMessage("서버에 연결할 수 없습니다.");
                return;
            }

            SetBusy(true);
            SetMessage("확인 중...");

            BackendMirror.Instance.RegisterOrLogin(username,
                onSuccess: () =>
                {
                    // 씬 전환 예정이므로 busy 유지 (다음 씬 로드)
                    GoToNextScene();
                },
                onBlocked: reason =>
                {
                    SetBusy(false);
                    SetMessage(reason); // "이미 등록된 이름입니다." / "이 기기는 이미 '...' 이름으로 등록되어 있습니다."
                },
                onError: err =>
                {
                    SetBusy(false);
                    SetMessage("네트워크 오류입니다. 다시 시도해주세요.");
                    Debug.LogWarning($"[NicknameRegister] 등록 실패: {err}");
                });
        }

        private void GoToNextScene()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadScene(_nextScene);
            }
            else
            {
                SceneManager.LoadScene(_nextScene);
            }
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            if (_registerButton != null)
            {
                _registerButton.interactable = !busy;
            }
            if (_nameInput != null)
            {
                _nameInput.interactable = !busy;
            }
        }

        private void SetMessage(string message)
        {
            if (_messageText != null)
            {
                _messageText.text = message;
            }
        }
    }
}
