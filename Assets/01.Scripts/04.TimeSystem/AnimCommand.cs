namespace Minsung.TimeSystem
{
    // 한 틱의 애니메이터 상태 스냅샷 - 되감기 시 해당 프레임을 그대로 스크럽해 보여준다.
    // 클립 역재생은 이 스냅샷을 역순으로 적용하는 것으로 구현된다 (컨트롤러 에셋 수정 불필요).
    public readonly struct AnimCommand
    {
        public readonly int   StateHash;      // 재생 중이던 상태 (shortNameHash)
        public readonly float NormalizedTime; // 상태 정규화 시간
        public readonly bool  FlipX;          // 좌우 반전 (레버는 우향 강제라 속도 기반 복원만으로는 어긋난다)

        public AnimCommand(int stateHash, float normalizedTime, bool flipX)
        {
            StateHash      = stateHash;
            NormalizedTime = normalizedTime;
            FlipX          = flipX;
        }
    }
}