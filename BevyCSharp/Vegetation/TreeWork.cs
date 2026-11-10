namespace Bevy;

/// <summary>The trees' part in the app's frame, which <see cref="EnginePlugin"/> adds to every app.</summary>
internal sealed class TreeWork : IPlugin
{
    public void Build(App app)
    {
        // A reading of global illumination's reference says how many trees did not stand yet.
        GiReference.UndressedTrees = Trees.Undressed;

        // The trees planted, dressed as their variants grow, their colliders in every run and their
        // meshes where something draws.
        app.AreasWork.Add(1400, world =>
        {
            if (App.HasRenderer) Trees.Tick(world);
        });
    }
}
