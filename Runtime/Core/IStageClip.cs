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
        float EaseInDuration { get; set; }
        float EaseOutDuration { get; set; }
        StageEffectBase Effect { get; }
        bool TrySetEffect(StageEffectBase effect);
        bool IsActiveAt(float timelineTime);
    }
}
