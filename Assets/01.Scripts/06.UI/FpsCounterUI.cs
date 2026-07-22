// Unity
using UnityEngine;
using TMPro;

using Minsung.Common;
using Minsung.Utility;

namespace Minsung.UI
{
    // 화면 우상단에 실시간 프레임 수를 "60 FPS" 형태로 띄우는 개발용 오버레이.
    // 씬마다 배치할 필요 없이 게임 시작 시 Resources 프리팹을 자동 인스턴스화해 모든 씬을 따라다닌다.
    public class FpsCounterUI : PersistentSingleton<FpsCounterUI>
    {
        private const string RESOURCES_PATH = "UI/FpsCounterUI"; // Assets/Resources/UI/FpsCounterUI.prefab

        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private TMP_Text _label;

        private float _elapsed;         // 갱신 주기 누적 시간(unscaled)
        private int   _frames;          // 갱신 주기 동안 그린 프레임 수
        private int   _lastFps = -1;    // 값이 바뀔 때만 텍스트를 갱신해 문자열 할당을 줄인다

        /****************************************
        *              Unity Event
        ****************************************/

        // 도메인 리로드를 꺼도 static이 깨끗하게 초기화되도록
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ResetStatic();
        }

        // 씬에 배치하지 않아도 게임 시작 시 프리팹을 자동 인스턴스화
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (Instance != null)
            {
                return;
            }

            GameObject prefab = Resources.Load<GameObject>(RESOURCES_PATH);
            if (prefab == null)
            {
                Debug.LogError($"[FpsCounterUI] 프리팹을 찾을 수 없습니다: Resources/{RESOURCES_PATH}");
                return;
            }

            Instantiate(prefab);
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            ++_frames;

            if (_elapsed < Constants.UI.FPS_REFRESH_INTERVAL)
            {
                return;
            }

            int fps  = (_elapsed > 0f) ? Mathf.RoundToInt(_frames / _elapsed) : 0;
            _frames  = 0;
            _elapsed = 0f;

            if ((fps != _lastFps) && (_label != null))
            {
                _lastFps    = fps;
                _label.text = fps + " FPS";
            }
        }
    }
}
