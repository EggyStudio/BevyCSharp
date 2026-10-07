namespace Bevy;

/// <summary>
/// Marks a class or struct as data kept in a file of its own and shared by whatever refers to it.
/// </summary>
/// <remarks>
/// <para>
/// An enemy's stats, a weapon, a dialogue, a loot table: values many entities share, edited in one
/// place, and changed without recompiling. Unity calls this a ScriptableObject and Godot a
/// Resource. A component refers to one through a <see cref="DataRef{T}"/> field and reads it with
/// <see cref="DataAssets.Get{T}"/>.
/// </para>
/// <para>
/// It lives on the managed side rather than in Bevy's storage, so it may hold strings, lists and
/// dictionaries, which a component cannot. The generator describes its fields as it describes a
/// component's, so the editor draws it with the same attributes and a file holds it in the same
/// form a scene holds a component. A class needs a constructor with no parameters, which makes the
/// default a new file starts with.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class DataAssetAttribute : Attribute;
