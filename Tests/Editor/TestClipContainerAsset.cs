using UnityEngine;

namespace Lumiscore.Tests.Editor
{
    public class TestClipContainerAsset : ScriptableObject
    {
        [SerializeField] public TestNonGenericEffectClip NonGenericClip = new TestNonGenericEffectClip();
        [SerializeField] public TestGenericEffectClip<TestPositionModule> GenericClip = new TestGenericEffectClip<TestPositionModule>();
    }
}
