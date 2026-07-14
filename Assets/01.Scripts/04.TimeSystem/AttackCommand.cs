namespace Minsung.TimeSystem
{
    // 공격 한 번을 캡슐화한 커맨드 - 정방향(피해+모션)/역방향(모션만)을 Execute/Undo로 나눠 처리한다.
    public readonly struct AttackCommand
    {
        public readonly bool Charged; // 차지공격 여부 - 분신 재연 시 같은 배율로 재현

        public AttackCommand(bool charged)
        {
            Charged = charged;
        }

        public void Execute(ICommandActor actor)
        {
            actor.PlayAttack(reversed: false, charged: Charged);
        }

        public void Undo(ICommandActor actor)
        {
            actor.PlayAttack(reversed: true, charged: Charged);
        }
    }
}
