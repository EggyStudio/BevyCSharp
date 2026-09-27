using System.Runtime.InteropServices;
using System.Numerics;
using ImGuiNET;

namespace Bevy;

/// <summary>
/// What the engine's input means to the interface.
/// </summary>
/// <remarks>
/// <para>
/// ImGui is told what happened rather than asked to look: a position, a button, a key, a character.
/// The engine reports the state of things once a frame instead, so this turns one into the other,
/// as every ImGui backend does.
/// </para>
/// <para>
/// The keys mapped are the ones an interface acts on. A letter reaches a text field as the
/// character it typed, which is the platform's answer and not something derived from the key.
/// </para>
/// </remarks>
internal static class ImGuiInput
{
    private static bool _left;
    private static bool _right;
    private static bool _middle;

    /// <summary>What ImGui says about a field being typed into, as <c>ImGuiPlatformImeData</c> lays it out.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ImeData
    {
        public byte WantVisible;
        public Vector2 InputPos;
        public float InputLineHeight;
    }

    /// <summary>What ImGui last said about the caret of the field being typed into.</summary>
    private static ImeData _wanted;

    /// <summary>What the window was last told, so it is told again only when that changes.</summary>
    private static ImeData _told;

    /// <summary>Whether the window turned the input method down, so it is not asked every frame.</summary>
    private static bool _noWindow;

    /// <summary>
    /// ImGui's callback for where a field being typed into has its caret, which it calls while it
    /// renders.
    /// </summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static unsafe void OnImeData(IntPtr context, IntPtr viewport, ImeData* data)
    {
        if (data is not null) _wanted = *data;
    }

    /// <summary>
    /// Turns the window's input method on while a field has the focus, with its candidate list
    /// under the caret, and off again once no field does.
    /// </summary>
    /// <remarks>
    /// Only while a field is being typed into, because with the input method on the keys it takes
    /// stop reaching the editor's shortcuts and the camera. Composed text reaches the field once it
    /// is committed, through <see cref="Feed"/>.
    /// </remarks>
    internal static void Ime()
    {
        if (_noWindow) return;

        var wanted = _wanted;
        if (wanted.WantVisible == _told.WantVisible && (wanted.WantVisible == 0 || wanted.InputPos == _told.InputPos)) return;

        try
        {
            Window.SetIme(wanted.WantVisible != 0, wanted.InputPos.X, wanted.InputPos.Y + wanted.InputLineHeight);
            _told = wanted;
        }
        catch (Bevy.Interop.BevyNativeException)
        {
            // An offscreen run has no window to turn one on in, and never will.
            _noWindow = true;
        }
    }

    /// <summary>Tells the interface what the pointer and the keyboard did.</summary>
    internal static void Feed(ImGuiIOPtr io, Input input, float scale, ReadOnlySpan<ImeCommit> committed = default)
    {
        _ = scale;

        // A pretend pointer wins, so a test that put it somewhere is not overruled by the hand
        // resting on the desk.
        if (SyntheticInput.Pretend is null)
        {
            io.AddMousePosEvent(input.MouseX, input.MouseY);

            Button(io, 0, input.MouseDown(MouseButton.Left), ref _left);
            Button(io, 1, input.MouseDown(MouseButton.Right), ref _right);
            Button(io, 2, input.MouseDown(MouseButton.Middle), ref _middle);
        }

        if (input.WheelX != 0f || input.WheelY != 0f)
        {
            io.AddMouseWheelEvent(input.WheelX, input.WheelY);
        }

        io.AddKeyEvent(ImGuiKey.ModCtrl, input.AnyKeyDown([Key.ControlLeft, Key.ControlRight]));
        io.AddKeyEvent(ImGuiKey.ModShift, input.AnyKeyDown([Key.ShiftLeft, Key.ShiftRight]));
        io.AddKeyEvent(ImGuiKey.ModAlt, input.AnyKeyDown([Key.AltLeft, Key.AltRight]));
        io.AddKeyEvent(ImGuiKey.ModSuper, input.AnyKeyDown([Key.SuperLeft, Key.SuperRight]));

        for (var index = 0; index < Table.Length; index++)
        {
            var (key, mapped) = Table[index];
            var down = input.KeyDown(key);

            if (down == Held[index]) continue;

            Held[index] = down;
            io.AddKeyEvent(mapped, down);
        }

        // What the keys produced, which is the platform's answer, because a layout, a dead key and
        // a modifier all change it, and none of that can be worked out from the key alone.
        foreach (var character in input.Text)
        {
            io.AddInputCharacter(character);
        }

        // What an input method composed, once it is chosen, which arrives in place of the text a
        // key would have typed.
        foreach (var commit in committed)
        {
            io.AddInputCharactersUTF8(commit.Text);
        }
    }

    /// <summary>Reports a button when it changes, as ImGui expects.</summary>
    private static void Button(ImGuiIOPtr io, int button, bool down, ref bool held)
    {
        if (down == held) return;

        held = down;
        io.AddMouseButtonEvent(button, down);
    }

    /// <summary>What was down last frame, so a change can be reported rather than a state.</summary>
    /// <remarks>
    /// Written after the table it is sized from, because a static field is built in the order it is
    /// written, and one built from a field below it is built from nothing.
    /// </remarks>
    private static readonly bool[] Held;

    static ImGuiInput() => Held = new bool[Table.Length];

    /// <summary>Which engine key is which ImGui one.</summary>
    private static readonly (Key Key, ImGuiKey Mapped)[] Table =
    [
        (Key.Tab, ImGuiKey.Tab),
        (Key.ArrowLeft, ImGuiKey.LeftArrow),
        (Key.ArrowRight, ImGuiKey.RightArrow),
        (Key.ArrowUp, ImGuiKey.UpArrow),
        (Key.ArrowDown, ImGuiKey.DownArrow),
        (Key.PageUp, ImGuiKey.PageUp),
        (Key.PageDown, ImGuiKey.PageDown),
        (Key.Home, ImGuiKey.Home),
        (Key.End, ImGuiKey.End),
        (Key.Insert, ImGuiKey.Insert),
        (Key.Delete, ImGuiKey.Delete),
        (Key.Backspace, ImGuiKey.Backspace),
        (Key.Space, ImGuiKey.Space),
        (Key.Enter, ImGuiKey.Enter),
        (Key.Escape, ImGuiKey.Escape),
        (Key.NumpadEnter, ImGuiKey.KeypadEnter),
        (Key.ControlLeft, ImGuiKey.LeftCtrl),
        (Key.ControlRight, ImGuiKey.RightCtrl),
        (Key.ShiftLeft, ImGuiKey.LeftShift),
        (Key.ShiftRight, ImGuiKey.RightShift),
        (Key.AltLeft, ImGuiKey.LeftAlt),
        (Key.AltRight, ImGuiKey.RightAlt),
        (Key.SuperLeft, ImGuiKey.LeftSuper),
        (Key.SuperRight, ImGuiKey.RightSuper),
        (Key.A, ImGuiKey.A),
        (Key.C, ImGuiKey.C),
        (Key.V, ImGuiKey.V),
        (Key.X, ImGuiKey.X),
        (Key.Y, ImGuiKey.Y),
        (Key.Z, ImGuiKey.Z),
    ];
}
