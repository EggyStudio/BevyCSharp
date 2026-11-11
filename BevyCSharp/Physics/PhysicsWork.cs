namespace Bevy;

/// <summary>The physics' part in the app, which <c>EnginePlugin</c> adds to every app.</summary>
internal sealed class PhysicsWork : IPlugin
{
    public void Build(App app)
    {
        // The colliders fitted to meshes belong to the app whose meshes they fit, and one fitted to
        // a mesh goes with the mesh's handle.
        Bevy.Physics.Colliders.Forget();
        AssetServer.AddForgetter(Bevy.Physics.Colliders.Forget);
    }
}
