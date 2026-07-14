namespace Minsung.Boss
{
    // 페이즈와 무관하게 BossController가 구동하는 공통 패턴 계약 (낙뢰 / 장풍 / 레이저 ...)
    public interface IBossPattern
    {
        /// <summary> 패턴 루프 시작 </summary>
        void Play();

        /// <summary> 패턴 루프 정지 + 떠 있는 연출 정리 </summary>
        void Stop();

        /// <summary> 풀 등 리소스까지 파괴. 보스 파괴 시 호출된다 </summary>
        void Dispose();

        // 전역 되감기 훅 - BossController가 IRewindable 콜백을 일괄 전달한다
        void OnRewindStart();
        void OnRewindEnd();
    }
}
