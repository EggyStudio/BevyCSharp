using Bevy.Interop;

namespace Bevy;

/// <summary>
/// This frame's keyboard and mouse state, mirrored from Bevy.
/// </summary>
/// <remarks>
/// Refreshed once per frame during <see cref="Stage.FrameSync"/>, before any user system runs,
/// so every system in a frame observes the same input. Reads are plain managed field accesses,
/// no call crosses into the engine.
/// </remarks>
/// <example>
/// <code>
/// [OnUpdate]
/// public static void Move(BehaviorContext ctx)
/// {
///     if (ctx.Input.KeyDown(Key.W)) Advance(ctx);
///     if (ctx.Input.KeyPressed(Key.Space)) Jump(ctx);
/// }
/// </code>
/// </example>
public sealed class Input
{
    private readonly ulong[] _down = new ulong[NativeInput.KeyWords];
    private readonly ulong[] _pressed = new ulong[NativeInput.KeyWords];
    private readonly ulong[] _released = new ulong[NativeInput.KeyWords];

    private uint _mouseDown;
    private uint _mousePressed;
    private uint _mouseReleased;

    /// <summary>Cursor X in physical window pixels. Always 0 in a headless run.</summary>
    public float MouseX { get; private set; }

    /// <summary>Cursor Y in physical window pixels. Always 0 in a headless run.</summary>
    public float MouseY { get; private set; }

    /// <summary>Cursor position.</summary>
    public (float X, float Y) MousePosition => (MouseX, MouseY);

    /// <summary>Cursor X movement since the previous frame.</summary>
    public float MouseDeltaX { get; private set; }

    /// <summary>Cursor Y movement since the previous frame.</summary>
    public float MouseDeltaY { get; private set; }

    /// <summary>Cursor movement since the previous frame.</summary>
    public (float X, float Y) MouseDelta => (MouseDeltaX, MouseDeltaY);

    /// <summary>Horizontal scroll since the previous frame.</summary>
    public float WheelX { get; private set; }

    /// <summary>Vertical scroll since the previous frame.</summary>
    public float WheelY { get; private set; }

    /// <summary>True while <paramref name="key"/> is held down.</summary>
    public bool KeyDown(Key key) => TestBit(_down, key);

    /// <summary>True on the single frame <paramref name="key"/> went down.</summary>
    public bool KeyPressed(Key key) => TestBit(_pressed, key);

    /// <summary>True on the single frame <paramref name="key"/> went up.</summary>
    public bool KeyReleased(Key key) => TestBit(_released, key);

    /// <summary>True while any key at all is held.</summary>
    public bool AnyKeyDown() => AnyBit(_down);

    /// <summary>True when any key at all went down this frame.</summary>
    public bool AnyKeyPressed() => AnyBit(_pressed);

    /// <summary>True while at least one of <paramref name="keys"/> is held.</summary>
    /// <remarks>
    /// Mirrors Bevy's <c>ButtonInput::any_pressed</c>. This is how you express a side-agnostic
    /// modifier by hand: <c>AnyKeyDown([Key.ControlLeft, Key.ControlRight])</c>.
    /// </remarks>
    public bool AnyKeyDown(ReadOnlySpan<Key> keys)
    {
        foreach (var key in keys)
            if (TestBit(_down, key))
                return true;

        return false;
    }

    /// <summary>True while every one of <paramref name="keys"/> is held.</summary>
    /// <remarks>Mirrors Bevy's <c>ButtonInput::all_pressed</c>. An empty span is true.</remarks>
    public bool AllKeysDown(ReadOnlySpan<Key> keys)
    {
        foreach (var key in keys)
            if (!TestBit(_down, key))
                return false;

        return true;
    }

    /// <summary>True when at least one of <paramref name="keys"/> went down this frame.</summary>
    /// <remarks>Mirrors Bevy's <c>ButtonInput::any_just_pressed</c>.</remarks>
    public bool AnyKeyPressed(ReadOnlySpan<Key> keys)
    {
        foreach (var key in keys)
            if (TestBit(_pressed, key))
                return true;

        return false;
    }

    /// <summary>True when at least one of <paramref name="keys"/> went up this frame.</summary>
    /// <remarks>Mirrors Bevy's <c>ButtonInput::any_just_released</c>.</remarks>
    public bool AnyKeyReleased(ReadOnlySpan<Key> keys)
    {
        foreach (var key in keys)
            if (TestBit(_released, key))
                return true;

        return false;
    }

    /// <summary>True while <paramref name="button"/> is held down.</summary>
    public bool MouseDown(MouseButton button) => (_mouseDown & Bit(button)) != 0;

    /// <summary>True on the single frame <paramref name="button"/> went down.</summary>
    public bool MousePressed(MouseButton button) => (_mousePressed & Bit(button)) != 0;

    /// <summary>True on the single frame <paramref name="button"/> went up.</summary>
    public bool MouseReleased(MouseButton button) => (_mouseReleased & Bit(button)) != 0;

    /// <summary>True while any mouse button is held.</summary>
    public bool AnyMouseDown() => _mouseDown != 0;

    /// <summary>True when any mouse button went down this frame.</summary>
    public bool AnyMousePressed() => _mousePressed != 0;

    private static uint Bit(MouseButton button) => 1u << (int)button;

    private static bool TestBit(ulong[] bits, Key key)
    {
        var index = (int)key;
        if ((uint)index >= KeyTable.Count) return false;
        return (bits[index / 64] & (1UL << (index % 64))) != 0;
    }

    private static bool AnyBit(ulong[] bits)
    {
        foreach (var word in bits)
            if (word != 0)
                return true;

        return false;
    }

    /// <summary>Copies a native snapshot into this frame's state.</summary>
    internal unsafe void Update(NativeInput snapshot)
    {
        MouseX = snapshot.MouseX;
        MouseY = snapshot.MouseY;
        MouseDeltaX = snapshot.MouseDeltaX;
        MouseDeltaY = snapshot.MouseDeltaY;
        WheelX = snapshot.WheelX;
        WheelY = snapshot.WheelY;
        _mouseDown = snapshot.MouseDown;
        _mousePressed = snapshot.MousePressed;
        _mouseReleased = snapshot.MouseReleased;

        for (var word = 0; word < NativeInput.KeyWords; word++)
        {
            _down[word] = snapshot.KeysDown[word];
            _pressed[word] = snapshot.KeysPressed[word];
            _released[word] = snapshot.KeysReleased[word];
        }
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"Input(mouse=({MouseX:F0},{MouseY:F0}), keysDown={(AnyKeyDown() ? "yes" : "no")})";
}
