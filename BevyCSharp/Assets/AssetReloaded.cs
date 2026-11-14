namespace Bevy;

/// <summary>
/// An asset read again from its file while the app runs, since the file changed on disk.
/// </summary>
/// <remarks>
/// <para>
/// Read like any other message, with <c>ctx.Read&lt;AssetReloaded&gt;()</c>. A handle keeps pointing
/// at the same asset, so whatever draws with it shows the new version without being told; this says
/// that it happened, for what keeps something it worked out from the old one, a size read from an
/// image or a collider fitted to a mesh, to work it out again.
/// </para>
/// <para>
/// Sent where the app watches its asset folder (<see cref="Config.WatchAssets"/>). Bevy's assets
/// need a bridge of the render profile or one over it, which carries Bevy's watcher. A file holding
/// several assets, a glTF with its meshes, materials and scenes, sends one for each, the path
/// naming the asset within the file after a <c>#</c>. An asset edited in place by code, a
/// material's color set or an image's pixels written, is no reload and sends nothing.
/// </para>
/// <para>
/// The files the engine reads itself say the same on any bridge, a scene file spawned again as
/// <c>SceneFile</c>, a material file laid over its material again as <c>MaterialFile</c> and a data
/// asset read again as <c>DataAsset</c>, and behavior scripts compiled again where an app compiles
/// them say <c>Scripts</c> with their folder as the path. Each is said in the log as well, and
/// <c>live.status</c> lists the latest.
/// </para>
/// </remarks>
/// <param name="Path">
/// The asset's path as the asset server knows it, with its label where it has one, or a file's path
/// under the asset root.
/// </param>
/// <param name="Kind">
/// What kind of asset it is, named the way <see cref="AssetServer.Load"/> names one, such as
/// <c>Image</c>, <c>Mesh</c>, <c>Gltf</c> or <c>Scene</c>, or <c>SceneFile</c>, <c>MaterialFile</c>,
/// <c>DataAsset</c> or <c>Scripts</c> for the files the engine reads itself.
/// </param>
public readonly record struct AssetReloaded(string Path, string Kind);
