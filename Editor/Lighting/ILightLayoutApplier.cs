using Lumiscore.Lighting;

namespace Lumiscore.Editor.Lighting
{
    public interface ILightLayoutApplier<TLayout>
        where TLayout : class, ILightLayout
    {
        void Apply(TLayout layout, LightNodeBase group);
    }
}
