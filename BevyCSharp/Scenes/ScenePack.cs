namespace Bevy;

/// <summary>
/// A well-known graphics scene kept as an asset pack fetched on demand, as its manifest names it.
/// </summary>
/// <remarks>
/// <para>
/// A scene such as Intel's Sponza is hundreds of megabytes even with its textures made small, too
/// much for a repository to hold, so a manifest is kept in its place, a file of JSON holding these
/// fields, and the pack is fetched from <see cref="Url"/> the first time it is asked for, checked
/// against <see cref="Sha256"/> and kept in a folder every game on the machine shares
/// (<see cref="ScenePacks"/>).
/// </para>
/// <para>
/// The license and the attribution are the scene's, which a game showing it shows with it, as
/// Creative Commons asks.
/// </para>
/// <para>
/// Its fields are settable rather than given at its making, since a source-generated reader gives a
/// field given at making that a manifest leaves out the zero of its type, a null string here, and
/// gives a settable one the value it starts with.
/// </para>
/// </remarks>
public sealed record ScenePack
{
    /// <summary>What it is called, which its manifest's file, its pack and its folder are named by.</summary>
    public string Name { get; set; } = "";

    /// <summary>What a player is shown it as.</summary>
    public string Title { get; set; } = "";

    /// <summary>Where the scene was published by its makers.</summary>
    public string Source { get; set; } = "";

    /// <summary>The license it is published under.</summary>
    public string License { get; set; } = "";

    /// <summary>The words its license asks to be shown with it.</summary>
    public string Attribution { get; set; } = "";

    /// <summary>Where the pack is fetched from, over HTTPS or as a file on this machine.</summary>
    public string Url { get; set; } = "";

    /// <summary>How many bytes the pack is.</summary>
    public long Size { get; set; }

    /// <summary>The pack's SHA-256 in lowercase hexadecimal, which a fetched copy must have.</summary>
    public string Sha256 { get; set; } = "";

    /// <summary>The model to load from the pack, by its path in the pack, such as <c>sponza.gltf</c>.</summary>
    public string Model { get; set; } = "";
}
