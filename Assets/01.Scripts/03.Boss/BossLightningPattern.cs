// System
using System.Collections;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;

namespace Minsung.Boss
{
    // 전 페이즈 공통 낙뢰 패턴. 기본 4초 간격으로 아레나 랜덤 지점에 위->아래로 떨어진다
    public class BossLightningPattern : IBossPattern
    {
        /****************************************
        *                Fields
        ****************************************/

        private const int POOL_SIZE = 4; // 동시 낙하 최대 수 (간격 대비 낙하 시간 여유분)

        private readonly BossController _boss;
        private readonly BossHazardPool _pool;
        private Coroutine _loop;

        // 감정 배율별 대기 캐시 (코루틴 회전마다 할당 방지)
        private readonly WaitForSeconds _waitNormal;
        private readonly WaitForSeconds _waitPink;
        private readonly WaitForSeconds _waitBlue;

        /****************************************
        *              Constructor
        ****************************************/

        public BossLightningPattern(BossController boss)
        {
            _boss = boss;
            _pool = new BossHazardPool(POOL_SIZE, "LightningBolt");

            BossDataSO bossSo = GameDB.Boss;
            _waitNormal = new WaitForSeconds(bossSo.LightningInterval);
            _waitPink   = new WaitForSeconds(bossSo.LightningInterval / bossSo.LightningRatePinkMult);
            _waitBlue   = new WaitForSeconds(bossSo.LightningInterval / bossSo.LightningRateBlueMult);
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 낙뢰 루프 시작 </summary>
        public void Play()
        {
            if (_loop == null)
            {
                _loop = _boss.StartCoroutine(CoDropLoop());
            }
        }

        /// <summary> 낙뢰 루프 정지 + 떠 있는 볼트 정리 </summary>
        public void Stop()
        {
            if (_loop != null)
            {
                _boss.StopCoroutine(_loop);
                _loop = null;
            }
            _pool.FreeAll(); // 볼트 코루틴은 IsActive 가드로 스스로 종료된다
        }

        /// <summary> 풀까지 파괴. 보스 파괴 시 호출 </summary>
        public void Dispose()
        {
            Stop();
            _pool.Dispose();
        }

        // TODO: 리와인드 스냅샷 - 볼트 위치/활성을 틱마다 기록해 역재생 (BossHazardPool.Capture/Apply 사용,
        //       구 "흑과 백" Phase1State의 8슬롯 스냅샷 방식 참고). 현재는 되감기 시 볼트를 정리하고 루프만 재시작한다
        public void OnRewindStart()
        {
            Stop();
        }

        public void OnRewindEnd()
        {
            Play();
        }

        /****************************************
        *              Coroutine
        ****************************************/

        // 낙하 루프: 감정 배율이 적용된 간격마다 랜덤 x에 볼트 하나를 떨어뜨린다
        private IEnumerator CoDropLoop()
        {
            while (true)
            {
                yield return CurrentWait();

                if (_boss.IsTransitioning)
                {
                    continue; // 기믹/컷신 중에는 낙하 정지
                }

                float x = Random.Range(_boss.ArenaMinX, _boss.ArenaMaxX);
                _boss.StartCoroutine(CoDropBolt(x));
            }
        }

        private WaitForSeconds CurrentWait()
        {
            switch (_boss.CurrentEmotion)
            {
                case BossEmotion.Pink:
                    return _waitPink;
                case BossEmotion.Blue:
                    return _waitBlue;
                default:
                    return _waitNormal;
            }
        }

        // 볼트 하나 낙하: 시작 높이에서 지면까지 등속 이동 후 풀 반환
        private IEnumerator CoDropBolt(float x)
        {
            BossDataSO bossSo = GameDB.Boss;

            Vector2 scale  = new Vector2(bossSo.LightningWidth, bossSo.LightningHeight);
            float   startY = _boss.ArenaGroundY + bossSo.LightningSpawnHeight;
            float   endY   = _boss.ArenaGroundY + (bossSo.LightningHeight * 0.5f);

            int slot = _pool.Alloc(new Vector2(x, startY), scale, bossSo.LightningColor, true,
                                   bossSo.LightningDamageHalves, bossSo.LightningStunDuration);
            if (slot < 0)
            {
                yield break; // 풀 고갈 - 이번 낙뢰는 생략
            }

            float y = startY;
            while ((y > endY) && (_pool.IsActive(slot))) // Stop()이 슬롯을 회수하면 스스로 종료
            {
                y -= bossSo.LightningFallSpeed * Time.deltaTime;
                _pool.SetPosition(slot, new Vector2(x, Mathf.Max(y, endY)));
                yield return null;
            }
            _pool.Free(slot);

            // TODO: 착탄 이펙트/사운드 (ParticlePresets / Constants.Audio 연동)
            // TODO: 낙하 전 지면 텔레그래프(예고 표시) 여부 기획 확정
        }
    }
}
