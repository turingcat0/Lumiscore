using System;
using System.Collections.Generic;

namespace Lumiscore.Core
{
    public interface IStageTrack
    {
        string Id { get; }
        string TargetNodeId { get; }
        Type ModuleType { get; }
        IReadOnlyList<IStageClip> Clips { get; }

        IStageClip FindClip(string clipId);
        IStageClip CreateClip(float startTime, float endTime, string id = null);
        IStageClip CloneClip(IStageClip source, float startTime, float endTime, string id = null);
        bool CanAcceptClip(IStageClip clip);
        bool AddClip(IStageClip clip);
        bool RemoveClip(string clipId);
        void Restore(string id, string targetNodeId);
    }
}
