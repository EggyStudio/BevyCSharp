using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>
/// Text a component holds a handle to, of any length, as Bevy's components hold a <c>String</c>.
/// </summary>
/// <remarks>
/// <para>
/// A component lives in Bevy's storage as bytes and cannot hold a <see cref="string"/>. This is two
/// numbers, a slot and a generation, into the store on the managed side that <see cref="EcsList{T}"/>
/// keeps its lists in, so it sits in a component and names text of any length. It converts to and
/// from <see cref="string"/>, so a field reads and is assigned as text, a scene file writes it as a
/// JSON string, and the inspector draws it as a text field.
/// </para>
/// <para>
/// Assigning a string makes new text and a new handle, as <c>new ComponentB { Value = "hello" }</c>
/// does, and setting <see cref="Value"/> changes the text the handle names, which every copy of the
/// handle then reads. The text is freed when the component leaves its entity, by removal or despawn,
/// through the hook the generator registers, and cloning the entity gives the clone text of its own.
/// A handle used after that throws <see cref="ObjectDisposedException"/>. Assigning over a field that
/// already names text leaves that text for the program to <see cref="Free"/>, as an
/// <see cref="EcsList{T}"/> replaced in a component does.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct EcsText : IEquatable<EcsText>
{
    private int _slot;
    private int _generation;

    /// <summary>Makes text holding <paramref name="text"/>.</summary>
    public static EcsText New(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var (slot, generation) = EcsStore.Allocate(new StrongBox<string>(text));
        return new EcsText { _slot = slot, _generation = generation };
    }

    /// <summary>Whether text has been made for this handle.</summary>
    public readonly bool IsCreated => _generation != 0;

    /// <summary>The text, empty for a handle with none made.</summary>
    /// <remarks>
    /// Setting it on a handle with none made makes the text, writing the new handle into this value,
    /// so it has to be set on the component's own field (a <c>ref</c> to it, or <c>this</c> in a
    /// behavior method) for the component to keep it, as <see cref="EcsList{T}.Add"/> is called.
    /// </remarks>
    public string Value
    {
        readonly get => Existing?.Value ?? string.Empty;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (Existing is { } box) box.Value = value;
            else this = New(value);
        }
    }

    /// <summary>The text, empty for a handle with none made.</summary>
    public static implicit operator string(EcsText text) => text.Value;

    /// <summary>New text holding <paramref name="text"/>.</summary>
    public static implicit operator EcsText(string text) => New(text);

    /// <summary>A handle to new text the same as this, or a handle to nothing when this names none.</summary>
    /// <remarks>The clone hook the generator registers calls this, so a cloned entity's text is its own.</remarks>
    public readonly EcsText Copy() => Existing is { } box ? New(box.Value ?? string.Empty) : default;

    /// <summary>Frees the text, after which this handle and every copy of it name nothing.</summary>
    /// <remarks>
    /// The remove hook does this for text held by a component as the component goes. Call it for text
    /// held anywhere else, or replaced in a component by other text.
    /// </remarks>
    /// <returns>Whether there was text to free.</returns>
    public bool Free()
    {
        var freed = EcsStore.Release(_slot, _generation);
        _slot = 0;
        _generation = 0;
        return freed;
    }

    /// <summary>The text's box, or nothing for a handle with none made, throwing for one freed since.</summary>
    private readonly StrongBox<string>? Existing
    {
        get
        {
            if (_generation == 0) return null;

            return EcsStore.Get(_slot, _generation) as StrongBox<string>
                ?? throw new ObjectDisposedException(
                    nameof(EcsText),
                    "The text was freed with the component that held it, and its slot may hold "
                    + "something else now.");
        }
    }

    /// <summary>Whether both handles name the same text, rather than equal text.</summary>
    public readonly bool Equals(EcsText other) => _slot == other._slot && _generation == other._generation;

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj) => obj is EcsText other && Equals(other);

    /// <inheritdoc/>
    public readonly override int GetHashCode() => HashCode.Combine(_slot, _generation);

    /// <summary>The text.</summary>
    public readonly override string ToString() => Value;
}
