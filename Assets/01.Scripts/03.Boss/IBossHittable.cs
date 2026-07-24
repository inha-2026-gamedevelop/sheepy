// System
using System;

// Unity
using UnityEngine;

namespace Minsung.Boss
{
    // 보스 피격 타격감 연출(히트스톱/스파크/데미지 넘버/HP바)이 구독하는 공용 이벤트 계약
    // Boss1(BossController)과 Boss2(Boss2Health)가 모두 구현해 같은 피드백 컴포넌트를 재사용한다
    public interface IBossHittable
    {
        // 보스가 실제 피해를 입은 순간 (반사/동결 제외) - 인자: 적용된 피해량
        event Action<float> OnDamaged;

        // 감정 반사로 공격자(플레이어)가 대신 피해를 입은 순간 - 인자: 플레이어 위치, 반칸량
        event Action<Vector3, int> OnDamageReflected;
    }
}
