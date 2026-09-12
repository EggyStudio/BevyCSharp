using System.Numerics;
using System.Runtime.InteropServices;
using Bevy.Interop;
using ImGuiNET;

namespace Bevy;

/// <summary>
/// Dear ImGui, running inside the engine.
/// </summary>
/// <remarks>
/// <para>
/// The interface is built here, in C#, the way ImGui is built anywhere: a call per widget, every
/// frame, with no state kept between them. What crosses to the engine is the triangles ImGui asked
/// for, which Bevy draws over whatever the scene drew.
/// </para>
/// <para>
/// Three calls make a frame: <see cref="Start"/> once, then <see cref="Begin"/> and
/// <see cref="End"/> around whatever draws the windows. Everything between them is ordinary ImGui.
/// </para>
/// <para>
/// It needs a bridge with the interface compiled in (<c>build/build-native.sh --editor</c>) and
/// <see cref="Config.Gui"/> asked for, and says so rather than drawing nothing.
/// </para>
/// </remarks>
public static unsafe class ImGuiRuntime
{
    /// <summary>What the atlas is called, once the engine has taken it.</summary>
    private static ulong _atlas;

    /// <summary>Where the buffers handed over last frame live, so they can be reused.</summary>
    private static NativeImGuiVertex* _vertices;
    private static ushort* _indices;
    private static NativeImGuiCommand* _commands;

    private static int _vertexRoom;
    private static int _indexRoom;
    private static int _commandRoom;

    /// <summary>Whether the context is up.</summary>
    public static bool IsRunning { get; private set; }

    /// <summary>Whether the interface is using the pointer, so nothing else should.</summary>
    /// <remarks>
    /// What keeps the camera still while a panel is being dragged. Asked after <see cref="Begin"/>,
    /// because ImGui works it out from where the pointer is and what is under it.
    /// </remarks>
    public static bool WantsMouse => IsRunning && ImGui.GetIO().WantCaptureMouse;

    /// <summary>Whether the interface is taking the keyboard, so no key binding should fire.</summary>
    public static bool WantsKeyboard => IsRunning && ImGui.GetIO().WantCaptureKeyboard;

    /// <summary>
    /// Whether a box somebody is typing into has the keyboard.
    /// </summary>
    /// <remarks>
    /// The question a shortcut has to ask, and not the same question as
    /// <see cref="WantsKeyboard"/>. With keyboard navigation switched on, the interface wants the
    /// keyboard whenever any of its windows is focused, which in an editor whose panels are always
    /// up is always: a shortcut that steps aside for that is a shortcut that never runs. What it
    /// has to step aside for is a field with a caret in it.
    /// </remarks>
    public static bool Typing => IsRunning && ImGui.GetIO().WantTextInput;

    /// <summary>How large the interface thinks the window is, in logical pixels.</summary>
    public static Vector2 Size { get; private set; }

    /// <summary>How many physical pixels a logical one is.</summary>
    public static float Scale { get; private set; } = 1f;

    /// <summary>
    /// Creates the context and hands the engine the font atlas.
    /// </summary>
    /// <param name="fonts">
    /// A directory holding the fonts to load, or <see langword="null"/> for ImGui's built-in one.
    /// Every <c>.ttf</c> named in <paramref name="faces"/> is loaded from it at
    /// <paramref name="size"/> logical pixels.
    /// </param>
    /// <param name="size">How large the text is, in logical pixels.</param>
    /// <param name="faces">The font files to load, the first being the one text uses by default.</param>
    public static void Start(string? fonts = null, float size = 15f, params string[] faces)
    {
        if (IsRunning) return;

        if (!App.HasEditor)
        {
            Console.WriteLine(
                "[imgui] this bridge has no interface compiled in, so nothing is drawn."
                + " Rebuild it with build/build-native.sh --editor.");
            return;
        }

        ImGui.CreateContext();

        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

        // No layout file. Every window here is placed by the editor from its own state, so what
        // ImGui would write is a file that is never read and appears in whichever directory the
        // program happened to start in.
        io.NativePtr->IniFilename = null;

        // A draw call says where its own vertices begin, so ImGui is free to put a whole window in
        // one buffer instead of splitting it every sixty-five thousand vertices.
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        if (fonts is { Length: > 0 } && faces.Length > 0)
        {
            foreach (var face in faces)
            {
                var path = Path.Combine(fonts, face);

                if (!File.Exists(path))
                {
                    Console.WriteLine($"[imgui] no font at {path}, keeping the built-in one");
                    continue;
                }

                Faces[face] = io.Fonts.AddFontFromFileTTF(path, size);
            }
        }

        Atlas(io);

        IsRunning = true;
    }

    /// <summary>Builds the font atlas and gives the engine its pixels.</summary>
    private static void Atlas(ImGuiIOPtr io)
    {
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out var width, out var height, out _);

        if (pixels == IntPtr.Zero || width <= 0 || height <= 0) return;

        _atlas = Native.bcs_imgui_texture((byte*)pixels, (uint)width, (uint)height);

        // ImGui puts this in every draw call that reads from the atlas, and it comes back
        // unchanged on the other side.
        io.Fonts.SetTexID((IntPtr)_atlas);

        // The pixels are the engine's now; ImGui's copy is a few megabytes doing nothing.
        io.Fonts.ClearTexData();
    }

    /// <summary>Every face that was asked for and found, by the file it came from.</summary>
    private static readonly Dictionary<string, ImFontPtr> Faces = [];

    /// <summary>
    /// One of the loaded faces, or whatever is in force when it was not loaded.
    /// </summary>
    /// <remarks>
    /// By name rather than by the order they were added, so the caller that wants a particular
    /// face says which one it wants. An index would put the same piece of knowledge in two places
    /// and one of them would eventually be wrong.
    /// </remarks>
    /// <param name="face">The font file it was loaded from.</param>
    public static ImFontPtr Face(string face) =>
        Faces.TryGetValue(face, out var found) ? found : ImGui.GetFont();

    /// <summary>Starts a frame: how large the window is, what the pointer did, what was typed.</summary>
    public static void Begin(BehaviorContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        if (!IsRunning) return;

        var io = ImGui.GetIO();

        var window = Window.Size();
        var scale = Window.Scale();

        // Logical pixels, which is what everything the bridge reports about a window is in, and
        // what the pointer arrives in. The scale is only what a clip rectangle is turned into.
        Size = new Vector2(Math.Max(1f, window.Width), Math.Max(1f, window.Height));
        Scale = scale;

        io.DisplaySize = Size;
        io.DisplayFramebufferScale = new Vector2(scale, scale);
        io.DeltaTime = ctx.Time.Delta > 0f ? ctx.Time.Delta : 1f / 60f;

        ImGuiInput.Feed(io, ctx.Input, scale);

        ImGui.NewFrame();
    }

    /// <summary>Ends the frame and hands the engine what came of it.</summary>
    public static void End()
    {
        if (!IsRunning) return;

        ImGui.Render();

        var data = ImGui.GetDrawData();
        if (data.NativePtr is null || data.CmdListsCount == 0) return;

        Room(ref _vertices, ref _vertexRoom, data.TotalVtxCount, sizeof(NativeImGuiVertex));
        Room(ref _indices, ref _indexRoom, data.TotalIdxCount, sizeof(ushort));

        var calls = 0;
        for (var list = 0; list < data.CmdListsCount; list++) calls += data.CmdLists[list].CmdBuffer.Size;

        Room(ref _commands, ref _commandRoom, calls, sizeof(NativeImGuiCommand));

        var vertices = 0;
        var indices = 0;
        var written = 0;

        for (var list = 0; list < data.CmdListsCount; list++)
        {
            var commands = data.CmdLists[list];

            var vertexCount = commands.VtxBuffer.Size;
            var indexCount = commands.IdxBuffer.Size;

            Buffer.MemoryCopy(
                (void*)commands.VtxBuffer.Data,
                _vertices + vertices,
                (long)(_vertexRoom - vertices) * sizeof(NativeImGuiVertex),
                (long)vertexCount * sizeof(NativeImGuiVertex));

            Buffer.MemoryCopy(
                (void*)commands.IdxBuffer.Data,
                _indices + indices,
                (long)(_indexRoom - indices) * sizeof(ushort),
                (long)indexCount * sizeof(ushort));

            for (var index = 0; index < commands.CmdBuffer.Size; index++)
            {
                var call = commands.CmdBuffer[index];

                // A callback is a piece of code ImGui wants run mid-list. Nothing here asks for
                // one, and a backend that cannot run them says so by skipping rather than by
                // drawing something else.
                if (call.UserCallback != IntPtr.Zero) continue;

                _commands[written++] = new NativeImGuiCommand
                {
                    Texture = (ulong)call.TextureId,
                    ClipLeft = call.ClipRect.X,
                    ClipTop = call.ClipRect.Y,
                    ClipRight = call.ClipRect.Z,
                    ClipBottom = call.ClipRect.W,

                    // Both are counted from the start of this list's share of the buffers, so what
                    // goes over is where that share begins plus what the call said.
                    Index = (uint)(indices + (int)call.IdxOffset),
                    Vertex = (uint)(vertices + (int)call.VtxOffset),
                    Elements = call.ElemCount,
                };
            }

            vertices += vertexCount;
            indices += indexCount;
        }

        var frame = new NativeImGuiFrame
        {
            Vertices = _vertices,
            VertexCount = (uint)vertices,
            Indices = _indices,
            IndexCount = (uint)indices,
            Commands = _commands,
            CommandCount = (uint)written,
            DisplayX = data.DisplayPos.X,
            DisplayY = data.DisplayPos.Y,
            DisplayWidth = data.DisplaySize.X,
            DisplayHeight = data.DisplaySize.Y,
            ScaleX = data.FramebufferScale.X,
            ScaleY = data.FramebufferScale.Y,
        };

        Native.Check(Native.bcs_imgui_frame(&frame), nameof(Native.bcs_imgui_frame));
    }

    /// <summary>Makes sure a buffer has room, keeping it between frames.</summary>
    /// <remarks>
    /// Grown and kept rather than allocated per frame: an interface produces a few thousand
    /// vertices sixty times a second, and the size it settles at is the size it stays.
    /// </remarks>
    private static void Room<T>(ref T* buffer, ref int room, int wanted, int size) where T : unmanaged
    {
        if (room >= wanted && buffer is not null) return;

        var grown = Math.Max(wanted, Math.Max(room * 2, 1024));

        if (buffer is not null) NativeMemory.Free(buffer);

        buffer = (T*)NativeMemory.Alloc((nuint)grown, (nuint)size);
        room = grown;
    }
}
