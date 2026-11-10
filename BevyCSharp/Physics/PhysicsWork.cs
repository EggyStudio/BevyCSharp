namespace Bevy;

/// <summary>The physics' part in the app, which <see cref="EnginePlugin"/> adds to every app.</summary>
internal sealed class PhysicsWork : IPlugin
{
    public void Build(App app)
    {
        // The colliders fitted to meshes belong to the app whose meshes they fit.
        Bevy.Physics.Colliders.Forget();
    }
}
