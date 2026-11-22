namespace Lumiscore.Core
{
    public interface IStageClip
    {
        string Id { get; }
        float StartTime { get; set; }
        float EndTime { get; set; }
        float Duration { get; }
        StageBlendMode BlendMode { get; set; }
        float Weight { get; set; }
        float EaseDuration { get; set; }
        IStageEffect Effect { get; }
        bool TrySetEffect(IStageEffect effect);
        bool IsActiveAt(float timelineTime);
    }
}
