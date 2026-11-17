using Lumiscore.State;

namespace Lumiscore.Patterns
{
    public interface ILightPattern<TModule>
        where TModule : class, IFixtureStateModule
    {
        TModule Evaluate(PatternContext<TModule> context);
    }
}
