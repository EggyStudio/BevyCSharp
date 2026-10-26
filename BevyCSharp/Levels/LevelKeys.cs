namespace Bevy;

/// <summary>
/// What a level file gave an entity, its class, its name and every keyvalue, so a game reads a key
/// the engine's own classes do not.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Levels.Spawn"/> gives it to every entity it spawns from a level's entity, the
/// <c>worldspawn</c> among them, beside what the class makes of it, so an entity of a class the
/// engine does not know still stands where the level put it with everything the level said of it.
/// <see cref="Levels.KeysOf"/> reads the keyvalues back in their order.
/// </para>
/// <para>
/// The name is the entity's <c>targetname</c>, by which another entity's output names it, or an
/// empty text where it has none.
/// </para>
/// </remarks>
[Behavior]
public partial struct LevelKeys
{
    /// <summary>Its class, as <c>info_player_start</c>.</summary>
    public EcsText ClassName;

    /// <summary>Its <c>targetname</c>, or an empty text.</summary>
    public EcsText TargetName;

    /// <summary>Every keyvalue, a line each as the level file writes them, which <see cref="Levels.KeysOf"/> reads.</summary>
    public EcsText Keys;
}
