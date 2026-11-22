using System;
using UnityEngine;

namespace Lumiscore.Lighting
{
    [Serializable]
    public class GridLightLayout : ILightLayout
    {
        [SerializeField] private int _columns = 4;
        [SerializeField] private float _spacingX = 1f;
        [SerializeField] private float _spacingY = 1f;
        [SerializeField] private bool _centered = true;

        public int Columns
        {
            get => _columns;
            set => _columns = value;
        }

        public float SpacingX
        {
            get => _spacingX;
            set => _spacingX = value;
        }

        public float SpacingY
        {
            get => _spacingY;
            set => _spacingY = value;
        }

        public bool Centered
        {
            get => _centered;
            set => _centered = value;
        }

        public (int Row, int Column) GetGridCoord(int index)
        {
            int cols = _columns > 0 ? _columns : 1;
            return (index / cols, index % cols);
        }
    }
}
