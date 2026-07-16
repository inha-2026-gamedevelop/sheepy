namespace Minsung.TimeSystem
{
    // 한 틱에 기록된 커맨드 묶음. RingBuffer/재생 루프가 다루는 기본 단위.
    public readonly struct TickCommand
    {
        public readonly MoveCommand     Move;
        public readonly bool            HasAttack;
        public readonly AttackCommand   Attack;
        public readonly bool            HasInteract;
        public readonly InteractCommand Interact;
        public readonly int             Hearts; // 이 틱 종료 시점의 하트 수 - 되감기 시 체력까지 복원한다
        public readonly AnimCommand     Anim;   // 이 틱의 애니메이터 스냅샷 - 되감기 모션 역재생용 (분신도 재사용)

        public TickCommand(MoveCommand move, bool hasAttack, AttackCommand attack, bool hasInteract, InteractCommand interact, int hearts, AnimCommand anim)
        {
            Move        = move;
            HasAttack   = hasAttack;
            Attack      = attack;
            HasInteract = hasInteract;
            Interact    = interact;
            Hearts      = hearts;
            Anim        = anim;
        }
    }
}
