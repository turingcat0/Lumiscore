namespace Lumiscore.Core
{
    public interface IStageClip
    {
        float StartTime { get; }
        float EndTime { get; }
        float Duration { get; }
        IStageEffect Effect { get; }
        bool IsActiveAt(float timelineTime);
    }
}
