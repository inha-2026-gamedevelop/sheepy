// System
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Boss;

namespace Minsung.Boss2
{
    // 부유 보스(Boss2) 레이저 패턴 - Minsung.Boss.Phase3State의 가로지르는 레이저 로직을 참고해 이식.
    // BossHazardPool(Minsung.Boss)은 BossController에 묶여 있지 않아 그대로 재사용한다(수정 없음)
    // 기본 간격마다 아레나를 가로지르는 레이저를 빨간 점멸 경고 후 발사한다. 시작/도착 높이는 랜덤
    // TODO: 리와인드 타임라인 미연동 - 결정 로그(시작/도착 높이) 기록/재현 미구현
    public class Boss2LaserPattern
    {
        /****************************************
        *                Fields
        ****************************************/

        private const int POOL_SIZE = 2; // 동시 사용 최대: 경고 또는 발사 1 + 여유 1

        private readonly MonoBehaviour _owner;
        private readonly Boss2DataSO   _dataSo;
        private readonly float         _arenaMinX;
        private readonly float         _arenaMaxX;
        private readonly float         _arenaGroundY;

        private readonly BossHazardPool _pool;
        private Coroutine _loop;

        private readonly WaitForSeconds _waitInterval;
        private readonly WaitForSeconds _waitBlink;
        private readonly WaitForSeconds _waitActive;

        /****************************************
        *              Constructor
        ****************************************/

        public Boss2LaserPattern(MonoBehaviour owner, Boss2DataSO dataSo,
            float arenaMinX, float arenaMaxX, float arenaGroundY)
        {
            _owner        = owner;
            _dataSo       = dataSo;
            _arenaMinX    = arenaMinX;
            _arenaMaxX    = arenaMaxX;
            _arenaGroundY = arenaGroundY;

            Material laserMat = _dataSo.LaserMaterial;
            _pool = new BossHazardPool(POOL_SIZE, "Boss2_Laser", customMaterial: laserMat, attachParticle: true,
                particleSize: _dataSo.LaserFlowParticleSize, particleColors: _dataSo.LaserFlowColors,
                particleOnHitOnly: true, particleFlowAlongX: true,
                particleFlowSpeed: _dataSo.LaserFlowSpeed, particleRate: _dataSo.LaserFlowRate);

            _waitInterval = new WaitForSeconds(_dataSo.LaserInterval);
            _waitBlink    = new WaitForSeconds(_dataSo.LaserBlinkInterval);
            _waitActive   = new WaitForSeconds(_dataSo.LaserActiveTime);
        }

        /****************************************
        *                Methods
        ****************************************/

        public void Play()
        {
            if (_loop == null)
            {
                _loop = _owner.StartCoroutine(CoLaserLoop());
            }
        }

        public void Stop()
        {
            if (_loop != null)
            {
                _owner.StopCoroutine(_loop);
                _loop = null;
            }
            _pool.FreeAll();
        }

        public void Dispose()
        {
            Stop();
            _pool.Dispose();
        }

        /****************************************
        *              Coroutine
        ****************************************/

        private IEnumerator CoLaserLoop()
        {
            while (true)
            {
                yield return _waitInterval;

                float startY = Random.Range(0f, _dataSo.LaserMaxHeight);
                float endY   = Random.Range(0f, _dataSo.LaserMaxHeight);
                yield return CoFireCrossLaser(startY, endY);
            }
        }

        // 한 발: 경고(빨간 깜빡임 얇은 실선, 판정 없음) -> 발사(회전 사각 판정, 한 칸) -> 좁아지며 회수
        private IEnumerator CoFireCrossLaser(float startY, float endY)
        {
            Vector2 start  = new Vector2(_arenaMinX, _arenaGroundY + startY);
            Vector2 end    = new Vector2(_arenaMaxX, _arenaGroundY + endY);
            Vector2 center = (start + end) * 0.5f;
            Vector2 delta  = end - start;
            float   angle  = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Vector2 scale  = new Vector2(delta.magnitude, _dataSo.LaserThickness);

            Vector2 warnScale = new Vector2(delta.magnitude, _dataSo.LaserWarningThickness);
            int warnSlot = _pool.Alloc(center, warnScale, _dataSo.LaserWarningColor, false, rotationDeg: angle);
            
            // 풀 고갈 - 이번 레이저는 생략
            if (warnSlot < 0)
            {
                yield break;
            }

            bool  visible = true;
            float elapsed = 0f;
            while (elapsed < _dataSo.LaserWarningTime)
            {
                yield return _waitBlink;
                elapsed += _dataSo.LaserBlinkInterval;
                visible = !visible;
                _pool.SetVisible(warnSlot, visible);
            }
            _pool.Free(warnSlot);

            int laserSlot = _pool.Alloc(center, scale, _dataSo.LaserColor, true, _dataSo.LaserDamageHalves, rotationDeg: angle);
            if (laserSlot < 0)
            {
                yield break;
            }
            yield return _waitActive;

            _pool.SetColliderActive(laserSlot, false);
            float retractElapsed = 0f;
            while (retractElapsed < _dataSo.LaserRetractTime)
            {
                yield return null;
                retractElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(retractElapsed / _dataSo.LaserRetractTime);
                _pool.SetScale(laserSlot, new Vector2(scale.x, Mathf.Lerp(scale.y, 0f, t)));
            }
            _pool.Free(laserSlot);
        }
    }
}
