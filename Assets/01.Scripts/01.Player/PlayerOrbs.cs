// System
using System;

// Unity
using UnityEngine;

using Minsung.Common;
using Minsung.Common.Data;
using Minsung.TimeSystem;

namespace Minsung.Player
{
    // 플레이어를 따라다니는 오브
    public class PlayerOrbs : MonoBehaviour
    {
        /****************************************
        *                Fields
        ****************************************/

        [Header("오브")]
        [SerializeField] private OrbController _orbPrefab;
        [SerializeField] private Color _orbTint = Color.white;

        private static readonly Collider2D[] _overlapResults = new Collider2D[16];

        private static readonly ContactFilter2D _anyCollider = CreateNoFilter();

        private OrbController[] _orbs;
        private int _nextOrb;
        private PlayerHealth _ownerHealth;    // 감정 반사 시 피해를 받을 소유자 체력
        private DamageSource _damageSource;   // 본체/분신 출처 (남색/하양 감정 반사 판정)

        /****************************************
        *              Unity Event
        ****************************************/

        private void Awake()
        {
            TryGetComponent(out _ownerHealth);
            // 분신에 붙어 있으면 분신 출처로 판정 (남색 감정이 분신 공격을 반사)
            _damageSource = (GetComponent<CloneController>() != null)
                ? DamageSource.PlayerClone
                : DamageSource.Player;

            int orbCount = GameDB.Player.OrbCount;
            _orbs = new OrbController[orbCount];

            for (int i = 0; i < orbCount; ++i)
            {
                OrbController orb = (_orbPrefab != null)
                    ? Instantiate(_orbPrefab)
                    : CreateDefaultOrb(i);

                // 오브 개수만큼 가로로 나란히 벌려놓는 슬롯 오프셋 (중앙 정렬)
                Vector2 slotOffset = Vector2.right * ((i - ((orbCount - 1) / 2f)) * GameDB.Player.OrbSpacing);

                orb.transform.SetParent(null);
                orb.Init(transform, i * 53.7f, slotOffset); // 오브마다 다른 노이즈 시드 - 겹치지 않는 자유로운 움직임

                // 분신 오브는 반투명화
                if (orb.TryGetComponent(out SpriteRenderer orbRenderer))
                {
                    orbRenderer.color *= _orbTint;
                }
                _orbs[i] = orb;
            }
        }

        private void OnEnable()
        {
            SetOrbsActive(true);
        }

        private void OnDisable()
        {
            SetOrbsActive(false);
        }

        private void SetOrbsActive(bool active)
        {
            if (_orbs == null)
            {
                return;
            }
            foreach (OrbController orb in _orbs)
            {
                if (orb == null)
                {
                    continue;
                }
                orb.gameObject.SetActive(active);
                if (active)
                {
                    orb.SnapToFollowTarget();
                }
            }
        }

        private void OnDestroy()
        {
            if (_orbs == null)
            {
                return;
            }
            foreach (OrbController orb in _orbs)
            {
                if (orb != null)
                {
                    Destroy(orb.gameObject);
                }
            }
        }

        /****************************************
        *                Methods
        ****************************************/

        /// <summary> 범위 안 가장 가까운 적에게 오브를 출격시킨다. 대상이 없으면 false. damage로 차지 배율을 반영한다. </summary>
        public bool TryAttackNearest(float damage)
        {
            if (!FindNearestTarget(transform.position, damage, _damageSource, _ownerHealth, out Transform target, out Action onHit))
            {
                return false;
            }

            SelectOrb().Attack(target, onHit);
            return true;
        }

        // 다음 순번부터 돌며 놀고 있는 오브 우선 선택, 전부 바쁘면 순번대로.
        private OrbController SelectOrb()
        {
            for (int i = 0; i < _orbs.Length; ++i)
            {
                int index = (_nextOrb + i) % _orbs.Length;
                if (!_orbs[index].IsAttacking)
                {
                    _nextOrb = (index + 1) % _orbs.Length;
                    return _orbs[index];
                }
            }

            OrbController orb = _orbs[_nextOrb];
            _nextOrb = (_nextOrb + 1) % _orbs.Length;
            return orb;
        }

        // 범위 안에서 가장 가까운 IDamageable(몬스터/보스/보스 분신)을 찾고,
        // 확정된 대상 하나에 대해서만 타격 콜백을 만든다.
        // 본체와 분신이 같은 조준 규칙을 공유하도록 하기 위해 static이다.
        private static bool FindNearestTarget(Vector2 origin, float damage, DamageSource source, PlayerHealth attacker,
                                              out Transform target, out Action onHit)
        {
            target = null;
            onHit  = null;

            IDamageable nearest     = null;
            Component   nearestBody = null; // 파괴 감지용 - 인터페이스 참조로는 Unity fake null을 못 잡는다
            float       nearestSqr  = float.MaxValue;

            int count = Physics2D.OverlapCircle(origin, GameDB.Player.OrbAttackRange, _anyCollider, _overlapResults);
            for (int i = 0; i < count; ++i)
            {
                Collider2D hit = _overlapResults[i];
                float sqr = ((Vector2)hit.transform.position - origin).sqrMagnitude;
                if (sqr >= nearestSqr)
                {
                    continue;
                }

                if (hit.TryGetComponent(out IDamageable damageable))
                {
                    nearest     = damageable;
                    nearestBody = hit;
                    nearestSqr  = sqr;
                    target      = hit.transform;
                }
            }

            // 도달 시점에 대상이 파괴됐을 수 있어 콜백 안에서 Component로 다시 체크한다.
            if (nearest != null)
            {
                IDamageable damageable = nearest;
                Component   body       = nearestBody;
                onHit = () =>
                {
                    if (body == null)
                    {
                        return;
                    }
                    // 실제로 피해가 들어갔을 때만 히트스톱 (반사/동결/사망 무효는 제외)
                    if (damageable.TakeDamage(damage, source, attacker))
                    {
                        HitStopController.Request();
                    }
                };
            }

            return target != null;
        }

        // "필터 없음" ContactFilter2D 생성 (트리거 콜라이더도 대상에 포함).
        private static ContactFilter2D CreateNoFilter()
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.NoFilter();
            filter.useTriggers = true; // NoFilter는 필터 쿼리에서 트리거를 제외하므로 명시적으로 포함 (트리거 판정형 보스 유닛 대응)
            return filter;
        }

        // 프리팹이 없을 때 쓰는 기본 오브
        // ! 프리팹 갖고오면 이건 X
        private OrbController CreateDefaultOrb(int index)
        {
            GameObject go = new GameObject($"PlayerOrb_{index}");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            Texture2D tex = Texture2D.whiteTexture;
            sr.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            sr.color  = GameDB.Player.OrbColor;

            go.transform.localScale = Vector3.one * GameDB.Player.OrbSize;
            return go.AddComponent<OrbController>();
        }
    }
}
