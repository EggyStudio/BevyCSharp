using ImGuiNET;

namespace Bevy;

/// <summary>
/// The members ImGui gives <see cref="SyntheticInput"/>, a key and typed text put into the
/// interface's own queue, which a test or a tool calls as the class's own.
/// </summary>
/// <remarks>
/// They extend <see cref="SyntheticInput"/> rather than being a part of it, since they write into
/// ImGui's event queue and the input stands in Core, which every area names. The pointer's half
/// that reaches the interface is given to <see cref="SyntheticInput.Send"/> by ImGui's plugin.
/// </remarks>
public static class SyntheticInputImGui
{
    extension(SyntheticInput)
    {
        /// <summary>
        /// Presses and releases a key in the interface's own queue.
        /// </summary>
        /// <remarks>
        /// A key that types something types it as well, because a keyboard does that and a field is
        /// waiting for it. This reaches the interface only, and
        /// <see cref="SyntheticInput.Press(Key, string)"/> starts where a real key starts.
        /// </remarks>
        public static void Key(ImGuiKey key, string? typed = null)
        {
            if (!ImGuiRuntime.IsRunning) return;

            var io = ImGui.GetIO();

            io.AddKeyEvent(key, true);

            if (typed is { Length: > 0 })
            {
                foreach (var character in typed) io.AddInputCharacter(character);
            }

            io.AddKeyEvent(key, false);
        }

        /// <summary>Types a run of characters.</summary>
        public static void Type(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            if (!ImGuiRuntime.IsRunning) return;

            var io = ImGui.GetIO();
            foreach (var character in text) io.AddInputCharacter(character);
        }
    }

    /// <summary>Puts a pointer into the interface's event queue, where it is running.</summary>
    internal static void Send(float x, float y, PointerAction action, MouseButton button)
    {
        var io = ImGui.GetIO();
        io.AddMousePosEvent(x, y);

        var which = button switch
        {
            MouseButton.Right => 1,
            MouseButton.Middle => 2,
            _ => 0,
        };

        switch (action)
        {
            case PointerAction.Press:
                io.AddMouseButtonEvent(which, true);
                break;

            case PointerAction.Release:
                io.AddMouseButtonEvent(which, false);
                break;
        }
    }
}
