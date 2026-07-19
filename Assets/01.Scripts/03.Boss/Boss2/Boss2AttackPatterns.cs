// Unity
using UnityEngine;

// 부유 보스(Boss2) 원거리 패턴 코디네이터 - 낙뢰/강타/레이저 3종을 묶어서 재생한다
// 각 패턴은 Minsung.Boss.BossHazardPool을 재사용하는 독립 클래스(Boss2LightningPattern/Boss2WavePattern/Boss2LaserPattern)
public class Boss2AttackPatterns : MonoBehaviour
{
    /****************************************
    *                Fields
    ****************************************/

    [Header("참조")]
    [SerializeField] private Transform   _target; // 낙뢰 낙하 지점 기준(플레이어) - 미연결 시 아레나 전체 랜덤
    [SerializeField] private Boss2DataSO _dataSo;

    [Header("아레나 경계 (패턴 배치 기준)")]
    [SerializeField] private float _arenaMinX    = -10f;
    [SerializeField] private float _arenaMaxX    = 10f;
    [SerializeField] private float _arenaGroundY = -3f;

    private Boss2LightningPattern _lightning;
    private Boss2WavePattern      _wave;
    private Boss2LaserPattern     _laser;

    /****************************************
    *              Unity Event
    ****************************************/

    private void Start()
    {
        if (_dataSo == null)
        {
            return;
        }

        _lightning = new Boss2LightningPattern(this, _target, _dataSo, _arenaMinX, _arenaMaxX, _arenaGroundY);
        _wave      = new Boss2WavePattern(this, _dataSo, _arenaMinX, _arenaMaxX, _arenaGroundY);
        _laser     = new Boss2LaserPattern(this, _dataSo, _arenaMinX, _arenaMaxX, _arenaGroundY);

        _lightning.Play();
        _wave.Play();
        _laser.Play();
    }

    private void OnDestroy()
    {
        _lightning?.Dispose();
        _wave?.Dispose();
        _laser?.Dispose();
    }
}
