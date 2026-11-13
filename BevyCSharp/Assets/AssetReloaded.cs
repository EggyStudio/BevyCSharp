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
/// Sent where Bevy watches the asset folder, which needs a bridge of the render profile or one over
/// it and <see cref="Config.WatchAssets"/> on. A file holding several assets, a glTF with its meshes,
/// materials and scenes, sends one for each, the path naming the asset within the file after a
/// <c>#</c>. An asset edited in place by code, a material's color set or an image's pixels written,
/// is no reload and sends nothing.
/// </para>
/// </remarks>
/// <param name="Path">The asset's path as the asset server knows it, with its label where it has one.</param>
/// <param name="Kind">
/// What kind of asset it is, named the way <see cref="AssetServer.Load"/> names one, such as
/// <c>Image</c>, <c>Mesh</c>, <c>Gltf</c> or <c>Scene</c>.
/// </param>
public readonly record struct AssetReloaded(string Path, string Kind);
