namespace Lumiscore.State
{
    public interface IFixtureState
    {
        TModule GetModule<TModule>() where TModule : class, IFixtureStateModule;
        void SetModule<TModule>(TModule module) where TModule : class, IFixtureStateModule;
    }

    public interface IHasModule<TModule> : IFixtureState
        where TModule : class, IFixtureStateModule
    {
    }
}
