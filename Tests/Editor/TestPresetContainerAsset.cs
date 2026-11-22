using System.Collections.Generic;
using UnityEngine;

namespace Lumiscore.Tests.Editor
{
    public class TestPresetContainerAsset : ScriptableObject
    {
        [SerializeReference] public List<ITestPresetValue> Values = new List<ITestPresetValue>();
    }
}
