// Unity
using UnityEngine;

using Minsung.TimeSystem;
using Minsung.Common;
using Minsung.Player;
using Minsung.Boss;

// 부유 보스(Boss2) 원거리 패턴 코디네이터 - 낙뢰/강타/레이저 3종을 묶어서 재생한다
// 각 패턴은 Minsung.Boss.BossHazardPool을 재사용하는 독립 클래스(Boss2LightningPattern/Boss2WavePattern/Boss2LaserPattern)
// 리와인드: 프레임 단위 스냅샷은 아직 없음(원본 BossLightningPattern과 동일한 수준) - 되감기 중엔 정지+회수, 종료 시 재시작만
// 감정(BossEmotion)도 여기서 함께 코디네이트한다 - Minsung.Boss.BossController가 하던 역할을 Boss2에선 이 클래스가 맡는다
public class Boss2AttackPatterns : MonoBehaviour, IRewindable
{
    /****************************************
    *                Fields
    ****************************************/

    [Header("참조")]
    [SerializeField] private Transform   _target; // 낙뢰 낙하 지점 기준 + 감정 대상(플레이어) - 미연결 시 아레나 전체 랜덤 / 감정 부가효과 없음
    [SerializeField] private Boss2DataSO _dataSo;
    [SerializeField] private HeartPickup _heartPickup; // 파랑 감정 하트 픽업 - 미연결 시 그냥 생략(Minsung Boss1도 실전 미배치 상태와 동일)

    [Header("아레나 경계 (패턴 배치 기준)")]
    [SerializeField] private float _arenaMinX    = -10f;
    [SerializeField] private float _arenaMaxX    = 10f;
    [SerializeField] private float _arenaGroundY = -3f;

    private Boss2LightningPattern _lightning;
    private Boss2WavePattern      _wave;
    private Boss2LaserPattern     _laser;
    private Boss2EmotionController _emotionController;

    public Boss2EmotionController EmotionController => _emotionController;

    /****************************************
    *              Unity Event
    ****************************************/

    // Boss2EmotionHUD 등 같은 오브젝트의 다른 컴포넌트가 OnEnable에서 이 컴포넌트를 찾으므로,
    // OnEnable보다 먼저 실행되는 Awake에서 만들어둬야 한다(Start에서 만들면 늦어 구독을 놓친다)
    private void Awake()
    {
        if (!TryGetComponent(out _emotionController))
        {
            _emotionController = gameObject.AddComponent<Boss2EmotionController>();
        }
    }

    private void Start()
    {
        if (_dataSo == null)
        {
            return;
        }

        PlayerController player = (_target != null) ? _target.GetComponent<PlayerController>() : null;
        // Boss2엔 아직 페이즈 전환 컷신이 없어 isTransitioning은 항상 false - 페이즈 시스템이 붙으면 그 상태를 넘겨야 한다
        _emotionController.Configure(player, _heartPickup, _dataSo, _arenaMinX, _arenaMaxX, _arenaGroundY, () => false);

        _lightning = new Boss2LightningPattern(this, _target, _dataSo, _arenaMinX, _arenaMaxX, _arenaGroundY,
            _emotionController.LightningRateMultiplier);
        _wave      = new Boss2WavePattern(this, _dataSo, _arenaMinX, _arenaMaxX, _arenaGroundY);
        _laser     = new Boss2LaserPattern(this, _dataSo, _arenaMinX, _arenaMaxX, _arenaGroundY);

        _lightning.Play();
        _wave.Play();
        _laser.Play();

        RewindManager.Instance?.Register(this);

        // 보스 스폰 == 전투 시작으로 간주 - Boss2엔 별도 입장 트리거가 없어 여기서 클리어 타이머를 켠다
        GameManager.Instance?.StartBossTimer();
        _emotionController.StartEmotionLoop(applyImmediately: true);
    }

    private void OnDestroy()
    {
        _lightning?.Dispose();
        _wave?.Dispose();
        _laser?.Dispose();
        RewindManager.Instance?.Unregister(this);
    }

    /****************************************
    *            IRewindable
    ****************************************/

    // 프레임 스냅샷 없음 - 정지/재시작만으로 되감기에 대응한다
    public void RecordTick() { }

    public void OnRewindStart()
    {
        _lightning?.Stop();
        _wave?.Stop();
        _laser?.Stop();
        _emotionController?.StopEmotionLoop();
    }

    public void ApplyRewindTick(int orderedIndex) { }

    public void OnRewindEnd(int orderedIndex)
    {
        _lightning?.Play();
        _wave?.Play();
        _laser?.Play();
        _emotionController?.StartEmotionLoop();
    }
}
