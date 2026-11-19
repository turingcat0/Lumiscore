namespace Lumiscore.Lighting
{
    public class GridLightLayout : ILightLayout
    {
        public int Columns { get; set; } = 4;
        public float SpacingX { get; set; } = 1f;
        public float SpacingY { get; set; } = 1f;
        public bool Centered { get; set; } = true;

        public (int Row, int Column) GetGridCoord(int index)
        {
            int cols = Columns > 0 ? Columns : 1;
            return (index / cols, index % cols);
        }
    }
}
