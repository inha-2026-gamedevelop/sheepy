// Unity
using UnityEngine;

using Minsung.Utility;

namespace Minsung.Common
{
    // 최소 진행도(마지막 플레이 씬) 저장/로드 - PlayerPrefs 기반
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
    }
}
