// Unity
using UnityEngine;

using Minsung.TimeSystem;
using Minsung.Common;
using Minsung.Player;
using Minsung.Boss;
using Minsung.CameraSystem;

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
    [SerializeField] private Transform   _target; // 낙뢰 낙하 지점 기준 + 감정 대상(플레이어)
    [SerializeField] private Boss2DataSO _dataSo;
    [SerializeField] private HeartPickup _heartPickup; // 파랑 감정 하트 픽업

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
        // Boss1(BossController.BeginBattle)과 동일한 줌아웃 값 - 아레나가 한 화면에 들어오도록(Map2/Map3 공통)
        CameraManager.Instance?.SetPlayerZoom(Constants.Camera.BOSS_ORTHOGRAPHIC_SIZE);
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
    *            공간찢기 독점 제어
    ****************************************/

    // 공간찢기 시퀀스 동안 일반 낙뢰/강타/레이저를 멈춘다(감정 루프는 화남 고정 유지 위해 그대로 둔다)
    public void SuspendNormalPatterns()
    {
        _lightning?.Stop();
        _wave?.Stop();
        _laser?.Stop();
    }

    // 공간찢기 종료 후 일반 패턴 재개
    public void ResumeNormalPatterns()
    {
        _lightning?.Play();
        _wave?.Play();
        _laser?.Play();
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
