using Lumiscore.Core;
using UnityEngine;

namespace Lumiscore.Lighting
{
    public interface ILightNode : IStageNode
    {
        Transform RootTransform { get; set; }
    }
}
