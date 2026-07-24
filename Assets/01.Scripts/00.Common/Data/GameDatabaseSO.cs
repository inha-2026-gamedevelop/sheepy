// Unity
using UnityEngine;

namespace Minsung.Common.Data
{
    // 도메인별 데이터 SO를 한데 묶는 루트 DB
    // 에셋: 08.Data/Resources/GameDB.asset - GameDB 정적 접근자가 Resources에서 자동 로드한다
    [CreateAssetMenu(fileName = "GameDB", menuName = "TheLastRewind/GameDB/GameDB (Root)")]
    public class GameDatabaseSO : ScriptableObject
    {
        /****************************************
        *                Fields
        ****************************************/

        public const string RESOURCES_PATH = "GameDB";

        [SerializeField] private PlayerDataSO _playerSo;
        [SerializeField] private BossDataSO   _bossSo;
        [SerializeField] private TimeDataSO   _timeSo;
        [SerializeField] private PotionDataSO _potionSo;

        public PlayerDataSO PlayerSo => _playerSo;
        public BossDataSO   BossSo   => _bossSo;
        public TimeDataSO   TimeSo   => _timeSo;
        public PotionDataSO PotionSo => _potionSo;
    }
}
