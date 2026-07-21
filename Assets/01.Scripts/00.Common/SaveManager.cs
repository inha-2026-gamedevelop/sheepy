// System
using System;

// Unity
using UnityEngine;

using Minsung.Common.Data;
using Minsung.Utility;

namespace Minsung.Common
{
    // 최소 진행도저장/로드
    [AddComponentMenu("Minsung/Save Manager")]
    public class SaveManager : PersistentSingleton<SaveManager>
    {
        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 저장된 진행도가 있는지 여부 (로비 '이어하기' 버튼 활성화 판단용) </summary>
        public bool HasSaveData()
        {
            return PlayerPrefs.HasKey(Constants.Save.KEY_LAST_SCENE);
        }

        /// <summary> 마지막으로 진입한 게임플레이 씬 이름을 저장 </summary>
        public void SaveProgress(string sceneName)
        {
            PlayerPrefs.SetString(Constants.Save.KEY_LAST_SCENE, sceneName);
            PlayerPrefs.Save();
        }

        /// <summary> 저장된 씬 이름을 반환. 저장 데이터가 없으면 defaultSceneName 반환 </summary>
        public string LoadLastScene(string defaultSceneName)
        {
            return PlayerPrefs.GetString(Constants.Save.KEY_LAST_SCENE, defaultSceneName);
        }

        /****************************************
        *          Player State (위치 기반)
        ****************************************/

        /// <summary> 저장된 플레이어 상태(위치 기반)가 있는지 여부 </summary>
        public bool HasPlayerState()
        {
            return PlayerPrefs.HasKey(Constants.Save.KEY_PLAYER_STATE);
        }

        /// <summary> 플레이어 위치/방향/씬을 SaveData로 직렬화해 저장. useDefaultSpawn=true면 이어하기 때 위치 복원을 건너뛴다(보스 중 저장 등). </summary>
        public void SavePlayerState(string sceneName, Vector3 position, int facingDir, bool useDefaultSpawn = false)
        {
            var data = new SaveData(sceneName, position, facingDir, useDefaultSpawn);
            PlayerPrefs.SetString(Constants.Save.KEY_PLAYER_STATE, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        /// <summary> 저장된 플레이어 상태를 로드. 데이터가 없거나 파싱에 실패하면 false 반환. </summary>
        public bool TryLoadPlayerState(out SaveData data)
        {
            data = null;

            if (!PlayerPrefs.HasKey(Constants.Save.KEY_PLAYER_STATE))
            {
                return false;
            }

            string json = PlayerPrefs.GetString(Constants.Save.KEY_PLAYER_STATE);
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] 플레이어 저장 데이터 파싱 실패: {e.Message}");
                data = null;
            }

            return (data != null) && !string.IsNullOrEmpty(data.SceneName);
        }

        /// <summary> 저장된 플레이어 상태 삭제 (새 게임 시작 등). 닉네임(계정)은 유지한다. </summary>
        public void ClearPlayerState()
        {
            PlayerPrefs.DeleteKey(Constants.Save.KEY_PLAYER_STATE);
            PlayerPrefs.DeleteKey(Constants.Save.KEY_BOSS_CLEARED);
            PlayerPrefs.Save();
        }

        /****************************************
        *            닉네임 (서버 식별자)
        ****************************************/

        /// <summary> 등록된 닉네임이 로컬에 저장되어 있는지 여부 (서버 미러 가능 조건) </summary>
        public bool HasUsername()
        {
            return PlayerPrefs.HasKey(Constants.Save.KEY_USERNAME);
        }

        /// <summary> 저장된 닉네임 반환 (없으면 빈 문자열) </summary>
        public string GetUsername()
        {
            return PlayerPrefs.GetString(Constants.Save.KEY_USERNAME, string.Empty);
        }

        /// <summary> 닉네임을 로컬에 영구 저장 (등록 성공 시 BackendMirror가 호출) </summary>
        public void SaveUsername(string username)
        {
            PlayerPrefs.SetString(Constants.Save.KEY_USERNAME, username);
            PlayerPrefs.Save();
        }

        /****************************************
        *              보스 클리어 여부
        ****************************************/

        /// <summary> 보스 클리어 여부 (로컬 기준) </summary>
        public bool IsBossCleared()
        {
            return PlayerPrefs.GetInt(Constants.Save.KEY_BOSS_CLEARED, 0) == 1;
        }

        /// <summary> 보스 클리어 여부 저장 </summary>
        public void SetBossCleared(bool cleared)
        {
            PlayerPrefs.SetInt(Constants.Save.KEY_BOSS_CLEARED, cleared ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
