namespace Bevy;

/// <summary>
/// A reference to a loaded asset.
/// </summary>
/// <remarks>
/// <para>
/// Bevy's own handle is generic and reference counted, and neither property survives a trip
/// through a C ABI. What C# holds instead is a key into a table on the engine side that owns the
/// real handle. Holding one keeps the asset loaded; <see cref="AssetServer.Release"/> lets it go.
/// </para>
/// <para>
/// Table slots are reused, so the key carries a generation as well as an index. A handle that has
/// been released does not start naming whatever took its slot; it reports
/// <see cref="AssetLoadState.Unknown"/> instead.
/// </para>
/// </remarks>
/// <summary>
/// An asset that could not be loaded, and why.
/// </summary>
/// <remarks>
/// <para>
/// Read like any other message, with <c>ctx.Read&lt;AssetLoadFailed&gt;()</c>. A handle says that a
/// load failed and nothing else, so this is where a misspelled path is told apart from a file that
/// is there and unreadable.
/// </para>
/// <para>
/// Sent in every profile, because an asset that will not load is exactly as wrong in a headless run
/// and a good deal harder to notice there.
/// </para>
/// </remarks>
/// <param name="Path">What was asked for, as the asset server saw it.</param>
/// <param name="Reason">Why it failed, as the engine described it.</param>
/// <param name="Kind">
/// What kind of asset it was, named the way <see cref="AssetServer.Load"/> names one. An asset
/// type the bridge does not load under a name of its own, such as one the engine loaded for itself
/// as part of something else, is named as Bevy's reflection names it (<c>GltfMesh</c>), and one
/// Bevy does not reflect is empty.
/// </param>
public readonly record struct AssetLoadFailed(string Path, string Reason, string Kind = "");
