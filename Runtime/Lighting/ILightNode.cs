using System;
using Lumiscore.Core;
using UnityEngine;

namespace Lumiscore.Lighting
{
    public interface ILightNode : IStageNode
    {
        string StateTypeName { get; set; }
        Transform RootTransform { get; set; }
        Type ResolveStateType();
    }
}
