using Bevy.Interop;

namespace Bevy;

/// <summary>The asset server's part in the app and its frame, which <c>EnginePlugin</c> adds to every app.</summary>
internal sealed class AssetWork : IPlugin
{
    public void Build(App app)
    {
        // The ids the scenes, data assets and other files the managed side reads carry, from the
        // folder or, for a game that compiled them into itself, from its own assembly. A data file
        // changed on disk is read again when assets are, and not in a shipped game.
        AssetIds.Reindex();
        DataAssets.Watching = app.Config.WatchAssets;

        // The failures said belong to the app whose loads failed.
        AssetServer.ForgetFailed();

        // The project's icon on the window it opens, where there is one. An offscreen or headless
        // run has none to show it on.
        if (App.HasRenderer && app.Project.Icon is { Length: > 0 } icon)
        {
            app.AddSystem(Stage.Startup, new SystemDescriptor(_ =>
            {
                if (Window.Entity().IsNone) return;
                Window.SetIcon(AssetServer.Load(AssetKind.Image, icon));
            }, "Engine.ProjectIcon"));
        }

        app.AreasWork.Add(100, world =>
        {
            var bus = world.Resource<MessageBus>();
            PostFailures(bus);
            PostReloads(bus);
        });

        // The handles a scene made for its entities that none of them draws with any more.
        app.AreasWork.Add(200, _ => AssetServer.Sweep());

        // The data files changed on disk since the frame before.
        app.AreasWork.Add(4000, world => DataAssets.PostChanges(world.Resource<MessageBus>()));
    }

    /// <summary>
    /// Moves the assets that failed to load onto the message bus.
    /// </summary>
    /// <remarks>
    /// Three texts per failure, each read the way every text crosses the boundary, which is to ask
    /// with nothing to learn the length and then ask again with a buffer that size. Failures are
    /// rare, so the reads cost nothing and a frame with none costs one call that answers zero.
    /// </remarks>
    private static unsafe void PostFailures(MessageBus bus)
    {
        // The files this side reads, mesh and material files and data assets, first.
        while (AssetServer.TakeFailed(out var failure)) bus.Send(failure);

        var count = NativeAssets.bcs_asset_failures_drain();
        if (count <= 0) return;

        for (var i = 0; i < count; i++)
        {
            var index = i;

            var path = Native.ReadText(
                (buffer, capacity) => NativeAssets.bcs_asset_failure_path(index, buffer, capacity),
                "reading the path of an asset that failed to load");

            var reason = Native.ReadText(
                (buffer, capacity) => NativeAssets.bcs_asset_failure_reason(index, buffer, capacity),
                "reading why an asset failed to load");

            // What kind it was, which the path alone does not say once a loader is picked by
            // content rather than by extension.
            var kind = Native.ReadText(
                (buffer, capacity) => NativeAssets.bcs_asset_failure_kind(index, buffer, capacity),
                "reading what kind of asset failed to load");

            bus.Send(new AssetLoadFailed(path, reason, kind));
        }
    }

    /// <summary>
    /// Moves the assets Bevy's watcher read again from their files onto the message bus.
    /// </summary>
    /// <remarks>
    /// Two texts each, read as the failures' are. A frame with none costs one call that answers
    /// zero, and a run that does not watch never has any.
    /// </remarks>
    private static unsafe void PostReloads(MessageBus bus)
    {
        var count = NativeAssets.bcs_asset_reloads_drain();
        for (var i = 0; i < count; i++)
        {
            var index = i;
            var path = Native.ReadText(
                (buffer, capacity) => NativeAssets.bcs_asset_reload_path(index, buffer, capacity),
                "reading the path of an asset read again");
            var kind = Native.ReadText(
                (buffer, capacity) => NativeAssets.bcs_asset_reload_kind(index, buffer, capacity),
                "reading what kind of asset was read again");
            bus.Send(new AssetReloaded(path, kind));
        }
    }
}
