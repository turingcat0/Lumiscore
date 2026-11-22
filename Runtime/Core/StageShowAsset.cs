using UnityEngine;

namespace Lumiscore.Core
{
    [CreateAssetMenu(menuName = "Lumiscore/Stage Show", fileName = "NewStageShow")]
    public class StageShowAsset : ScriptableObject
    {
        [SerializeField] private StageShow _show = new StageShow();

        public StageShow Show => _show;
    }
}
