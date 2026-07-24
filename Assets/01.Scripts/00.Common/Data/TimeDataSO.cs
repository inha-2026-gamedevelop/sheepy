// Unity
using UnityEngine;

namespace Minsung.Common.Data
{
    // 타임리와인드/분신/슬로우 밸런싱 데이터 DB - 에셋: 08.Data/Time/TimeDB.asset (GameDB.Time으로 접근)
    [CreateAssetMenu(fileName = "TimeDB", menuName = "TheLastRewind/GameDB/TimeDB")]
    public class TimeDataSO : ScriptableObject
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("타임리와인드")]
        // 리와인드에 참여하는 모든 버퍼(플레이어/몬스터/보스)의 용량 기준
        // 서로 다르면 되감기 인덱스가 어긋나므로 반드시 RewindManager.TickCapacity를 통해서만 쓴다
        [SerializeField] private float _recordSeconds  = 10f; // 기록 최대 길이(초)
        [SerializeField] private float _rewindDuration = 1f;  // 역재생 연출 길이(초)

        [Header("분신 (체력은 본체와 동일한 하트 방식)")]
        [SerializeField] private int   _maxCloneCount        = 3;
        [SerializeField] private float _cloneAttackFlashTime = 0.1f; // 분신 피격 플래시 지속시간(초)
        [SerializeField] private float _cloneLifetime        = 60f;  // 분신 유지 시간(현실 시간 초)

        [SerializeField] private Color _cloneTintColor = new Color(0.4f, 0.8f, 1f, 0.6f); // 분신 표시 색 (반투명 하늘색)

        [Header("슬로우 (fixedDeltaTime 보정은 SlowMotionController가 자동 적용)")]
        [SerializeField, Range(0.1f, 1f)] private float _slowTimeScale = 0.4f; // 슬로우 배율 (1.0 = 정상)

        /****************************************
        *              Properties
        ****************************************/

        public float RecordSeconds  => _recordSeconds;
        public float RewindDuration => _rewindDuration;

        public int   MaxCloneCount        => _maxCloneCount;
        public float CloneAttackFlashTime => _cloneAttackFlashTime;
        public float CloneLifetime        => _cloneLifetime;
        public Color CloneTintColor       => _cloneTintColor;

        public float SlowTimeScale => _slowTimeScale;
    }
}
