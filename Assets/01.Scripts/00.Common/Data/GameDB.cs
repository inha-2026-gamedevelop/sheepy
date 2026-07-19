// Unity
using UnityEngine;

namespace Minsung.Common.Data
{
    // 게임 데이터 DB 정적 접근자 - 사용처는 GameDB.Player.MoveSpeed 형태로 읽는다
    // 루트 GameDatabaseSO(08.Data/Resources/GameDB.asset)를 첫 접근 시 1회 로드해 캐싱한다
    // 주의: Resources.Load를 쓰므로 MonoBehaviour 필드 초기화식/생성자에서는 호출 금지 (Awake 이후 사용)
    public static class GameDB
    {
        /****************************************
        *                Fields
        ****************************************/

        private static GameDatabaseSO _rootSo;

        public static PlayerDataSO Player => Root.PlayerSo;
        public static BossDataSO   Boss   => Root.BossSo;
        public static TimeDataSO   Time   => Root.TimeSo;
        public static LpDataSO     Lp     => Root.LpSo;
        public static PotionDataSO Potion => Root.PotionSo;

        private static GameDatabaseSO Root
        {
            get
            {
                if (_rootSo == null)
                {
                    _rootSo = Resources.Load<GameDatabaseSO>(GameDatabaseSO.RESOURCES_PATH);
                    if (_rootSo == null)
                    {
                        Debug.LogError($"[GameDB] 루트 DB 에셋이 없습니다: Resources/{GameDatabaseSO.RESOURCES_PATH} (08.Data/Resources/GameDB.asset 확인)");
                    }
                }
                return _rootSo;
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        // 도메인 리로드를 꺼도 static 캐시가 초기화되도록
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _rootSo = null;
        }
    }
}
