// System
using System;

// Unity
using UnityEngine;

namespace Minsung.Common.Data
{
    // 게임 종료 시점의 "플레이어 위치 기반" 진행 상태 저장 구조.
    // SaveManager가 이 구조를 JsonUtility로 직렬화해 PlayerPrefs(Constants.Save.KEY_PLAYER_STATE)에 보관한다.
    // JsonUtility 호환을 위해 필드는 직렬화 가능한 타입([SerializeField] private)만 사용한다.
    [Serializable]
    public class SaveData
    {
        /****************************************
        *                Fields
        ****************************************/

        [SerializeField] private string  _sceneName;       // 저장 당시 게임플레이 씬 이름
        [SerializeField] private Vector3 _playerPosition;  // 플레이어 월드 좌표
        [SerializeField] private int     _facingDir;       // 바라보던 방향 (1: 오른쪽, -1: 왼쪽)
        [SerializeField] private bool    _useDefaultSpawn; // true면 위치 복원을 건너뛰고 씬 기본 스폰 사용(보스 중 저장 등)
        [SerializeField] private long    _savedAtUtcTicks; // 저장 시각(DateTime.UtcNow.Ticks) - 최신성 판단용

        /****************************************
        *              Properties
        ****************************************/

        public string   SceneName       => _sceneName;
        public Vector3  PlayerPosition  => _playerPosition;
        public int      FacingDir       => _facingDir;
        public bool     UseDefaultSpawn => _useDefaultSpawn;
        public DateTime SavedAtUtc      => new DateTime(_savedAtUtcTicks, DateTimeKind.Utc);

        /****************************************
        *              Constructors
        ****************************************/

        // JsonUtility 역직렬화용 기본 생성자
        public SaveData() { }

        public SaveData(string sceneName, Vector3 playerPosition, int facingDir, bool useDefaultSpawn = false)
        {
            _sceneName       = sceneName;
            _playerPosition  = playerPosition;
            _facingDir       = (facingDir < 0) ? -1 : 1;
            _useDefaultSpawn = useDefaultSpawn;
            _savedAtUtcTicks = DateTime.UtcNow.Ticks;
        }
    }
}
