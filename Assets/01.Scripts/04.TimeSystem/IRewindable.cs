namespace Minsung.TimeSystem
{
    // 리와인드에 참여하는 오브젝트가 구현하는 인터페이스.
    public interface IRewindable
    {
        void RecordTick();
        void OnRewindStart();
        void ApplyRewindTick(int orderedIndex);
        void OnRewindEnd(int orderedIndex);
    }
}
