using System.Globalization;
using System.Runtime.CompilerServices;
using ImGuiNET;
using NumericsVector2 = System.Numerics.Vector2;
using NumericsVector4 = System.Numerics.Vector4;

namespace Bevy;

// The modeling window, an ImGui window a program draws with one call, which shapes an edit mesh by
// its operations' buttons and by the pointer in the 3D view, a click picking a face and a drag
// along its normal extruding it.
public static partial class Modeling
{
    private static readonly ConditionalWeakTable<EditMesh, WindowState> Windows = new();

    // The meshes a window was drawn on in the last frames, outlined from the frame's tick, since
    // lines drawn from the interface's pass come after the frame's lines are gathered and do not
    // show.
    private static readonly List<(EditMesh Mesh, Transform Placed, long Frame)> Outlined = [];
    private static long _windowFrame;
    private static App? _outlinedApp;

    // The frame a window last held the pointer on, counted as the outlines are.
    private static long _pointerOn = -2;

    /// <summary>What a window remembers of a mesh between frames, the fields typed and the pointer's press.</summary>
    private sealed class WindowState
    {
        public readonly Dictionary<string, string> Fields = new(StringComparer.Ordinal);
        public int Hovered = -1;
        public int Pressed = -1;
        public (float X, float Y) PressedAt;
        public bool Dragging;
        public bool Moving;
        public float Distance;
        public string? Problem;
    }

    /// <summary>A measure an operation's button takes, its label, what it starts as, and the word written before it.</summary>
    private readonly record struct Field(string Label, string Start, string? Word = null);

    /// <summary>The operations the window offers, by group, each its recipe's verb and the measures it takes.</summary>
    private static readonly (string Group, string Verb, Field[] Fields)[] Offered =
    [
        ("Add", "box", [new("x y z", "1 1 1"), new("at", "0 0 0", "at")]),
        ("Add", "plane", [new("x z", "2 2"), new("at", "0 0 0", "at")]),
        ("Add", "cylinder", [new("radius height sides", "0.5 1 16"), new("at", "0 0 0", "at")]),
        ("Add", "cone", [new("radius height sides", "0.5 1 16"), new("at", "0 0 0", "at")]),
        ("Add", "wedge", [new("x y z", "1 1 1"), new("at", "0 0 0", "at")]),
        ("Add", "stairs", [new("steps rise run width", "8 0.2 0.3 1"), new("at", "0 0 0", "at")]),
        ("Add", "arch", [new("radius thickness depth sides", "1 0.2 0.4 12"), new("at", "0 0 0", "at")]),
        ("Add", "room", [new("x y z thickness", "6 3 5 0.2"), new("at", "0 0 0", "at")]),
        ("Add", "ramp", [new("rise run width", "1 3 2"), new("at", "0 0 0", "at")]),
        ("Change", "move", [new("x y z", "0 1 0")]),
        ("Change", "rotate", [new("axis degrees", "y 90")]),
        ("Change", "scale", [new("factor", "2")]),
        ("Change", "material", [new("slot", "wall")]),
        ("Change", "extrude", [new("distance", "1")]),
        ("Change", "inset", [new("distance", "0.25")]),
        ("Change", "merge", [new("distance", "0.001")]),
        ("Change", "shade", [new("smooth degrees or flat", "smooth 30")]),
        ("Change", "delete", [new("fill or nothing", "")]),
        ("Change", "flip", []),
        ("Change", "triangulate", []),
        ("Change", "duplicate", []),
        ("Change", "snap", []),
        ("Edges", "select edge", [new("a b, more pairs after", "0 1")]),
        ("Edges", "bevel", [new("width segments", "0.1 2")]),
        ("Edges", "loopcut", [new("fraction", "0.5")]),
        ("Edges", "subdivide", [new("cuts", "1")]),
        ("Edges", "bridge", []),
        ("Edges", "fill", []),
        ("Edges", "dissolve", [new("edges or nothing", "")]),
        ("Edges", "knife", [new("axis", "x"), new("at", "0 0 0", "at")]),
        ("Edges", "mirror", [new("axis", "x"), new("at", "0 0 0", "at")]),
        ("Edges", "spin", [new("axis degrees steps", "y 360 16"), new("at", "0 0 0", "at")]),
        ("Edges", "smooth", [new("factor repeat", "0.5 1")]),
        ("Cut", "cut", [new("window w h along sill, door w h along, or a primitive", "window 1 1.2 2 0.9")]),
        ("Cut", "union", [new("path", "models/part.model"), new("at", "0 0 0", "at")]),
        ("Cut", "subtract", [new("path", "models/part.model"), new("at", "0 0 0", "at")]),
        ("Cut", "intersect", [new("path", "models/part.model"), new("at", "0 0 0", "at")]),
        ("Cut", "join", [new("path", "models/part.model")]),
        ("Texture", "uv planar", [new("+x to -z, or x y z", "+y")]),
        ("Texture", "uv cylinder", [new("x, y, z, or x y z", "y")]),
        ("Texture", "uv move", [new("u v", "0.5 0")]),
        ("Texture", "uv scale", [new("factor, or u v, then each", "2")]),
        ("Texture", "uv rotate", [new("degrees, then each", "90")]),
        ("Texture", "uv density", [new("pixels a meter", "256")]),
        ("Slots", "slot texture", [new("slot path, or none", "default textures/wall.png")]),
        ("Slots", "slot normal-map", [new("slot path, or none", "default textures/wall_normal.png")]),
        ("Slots", "slot color", [new("slot r g b", "default 1 1 1")]),
        ("Slots", "slot roughness", [new("slot value", "default 0.8")]),
        ("Slots", "slot metallic", [new("slot value", "default 0")]),
        ("Slots", "slot add", [new("slot", "trim")]),
        ("Slots", "slot rename", [new("slot new", "default floor")]),
        ("Slots", "slot remove", [new("slot", "trim")]),
    ];

    private static readonly NumericsVector4 Warning = new(1f, 0.45f, 0.35f, 1f);

    /// <summary>
    /// Draws the modeling window on a mesh, and takes the pointer in the 3D view, where a camera is
    /// given, to pick its faces and extrude or move them. Call it every frame the window is to be up, inside a
    /// system, with ImGui running (<see cref="Config.Gui"/>).
    /// </summary>
    /// <param name="ctx">The system's context, which the pointer is read from.</param>
    /// <param name="mesh">The mesh to shape.</param>
    /// <param name="camera">The camera the 3D view is drawn by, or none for the window alone.</param>
    /// <param name="placed">Where the mesh is drawn, its entity's transform, or nothing for where it is built.</param>
    /// <remarks>
    /// <para>
    /// The window lists the recipe, its last lines taken back and done again by its buttons, the
    /// counts and what is selected, the grid step, and each operation with its measures as fields
    /// and a button that runs its line, so each change made in the window is a line of the recipe,
    /// as a command's is. A line that cannot run says why under the selection's buttons. Its Texture
    /// group draws the selected faces' texture coordinates over their slot's texture, and projects
    /// them along the camera's view among its directions.
    /// </para>
    /// <para>
    /// In the 3D view, a click on a face selects it by a ray from the camera tested against the
    /// mesh's faces, with shift adding it and control taking it away. A drag from a face along its
    /// normal extrudes the selection by whole grid steps on release, the distance shown beside
    /// the pointer, and with alt moves it as far instead. The face under the pointer is outlined
    /// in yellow and the selection in orange. The pointer over a window of the interface is the
    /// interface's, and picks nothing.
    /// </para>
    /// <para>
    /// A program whose camera turns on a drag asks <see cref="WantsPointer"/> before it begins
    /// one, as it asks <see cref="ImGuiRuntime.WantsMouse"/>, so a drag on a face extrudes or
    /// moves the face rather than turning the view.
    /// </para>
    /// </remarks>
    public static void DrawWindow(BehaviorContext ctx, EditMesh mesh, Entity camera = default, Transform? placed = null)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(mesh);
        if (!ImGuiRuntime.IsRunning) return;

        // Not focused as it appears, so the key that opened it, a Tab as often as not, does not move
        // into its first field, and the program's keys stay its own until the window is clicked.
        ImGui.SetNextWindowSize(new NumericsVector2(380f, 560f), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Model##bcs-model", ImGuiWindowFlags.NoFocusOnAppearing)) DrawPanel(ctx, mesh, camera, placed);
        ImGui.End();
    }

    /// <summary>
    /// Draws the modeling window's contents where the program has begun a window or a panel of
    /// its own, as the editor's Model tab does, and takes the pointer in the 3D view as
    /// <see cref="DrawWindow"/> does.
    /// </summary>
    /// <param name="ctx">The system's context, which the pointer is read from.</param>
    /// <param name="mesh">The mesh to shape.</param>
    /// <param name="camera">The camera the 3D view is drawn by, or none for the contents alone.</param>
    /// <param name="placed">Where the mesh is drawn, its entity's transform, or nothing for where it is built.</param>
    public static void DrawPanel(BehaviorContext ctx, EditMesh mesh, Entity camera = default, Transform? placed = null)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(mesh);

        var state = Windows.GetOrCreateValue(mesh);
        var where = placed ?? Transform.Identity;

        if (ImGuiRuntime.IsRunning)
        {
            // A strip wider than it is tall, as the editor's tab is, takes the operations in a
            // column beside the recipe, scrolled on its own, where a window taller than it is wide
            // takes them below, so neither leaves the operations past where the panel ends.
            var room = ImGui.GetContentRegionAvail();
            if (room.X > 640f && room.X > 2f * room.Y && ImGui.BeginTable("columns##bcs-model", 2, ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("recipe", ImGuiTableColumnFlags.WidthStretch, 0.4f);
                ImGui.TableSetupColumn("operations", ImGuiTableColumnFlags.WidthStretch, 0.6f);
                ImGui.TableNextColumn();
                Summary(mesh, state);
                Recipe(mesh, MathF.Max(ImGui.GetContentRegionAvail().Y - 4f, 60f));
                ImGui.TableNextColumn();
                if (ImGui.BeginChild("operations##bcs-model", new NumericsVector2(0f, MathF.Max(room.Y - 4f, 60f)))) Operations(ctx, mesh, state, camera, where);
                ImGui.EndChild();
                ImGui.EndTable();
            }
            else
            {
                Summary(mesh, state);
                Recipe(mesh, 140f);
                Operations(ctx, mesh, state, camera, where);
            }
        }

        if (!camera.IsNone) Pointer(ctx, mesh, camera, where, state);

        lock (Outlined)
        {
            Outlined.RemoveAll(outlined => ReferenceEquals(outlined.Mesh, mesh));
            Outlined.Add((mesh, where, _windowFrame));
        }
    }

    /// <summary>
    /// Whether a modeling window's pointer is over a face of its mesh in the 3D view, or pressed on
    /// one, this frame or the last, so a program leaves a drag begun there to the window.
    /// </summary>
    /// <remarks>
    /// The 3D view's counterpart of <see cref="ImGuiRuntime.WantsMouse"/>. A camera that turns on a
    /// drag, as the editor's does with alt held, would turn while a face is moved, so it begins a
    /// drag only where this is false, and a drag begun beside the model still turns it. The frame
    /// before counts as well, since a program's system may run before the window is drawn in a
    /// frame and read what the window found the frame before.
    /// </remarks>
    public static bool WantsPointer => _windowFrame - Volatile.Read(ref _pointerOn) <= 1;

    /// <summary>Outlines what the windows drawn this frame or the last are on, once a frame from the engine's tick.</summary>
    /// <param name="draws">Whether the run draws, without which there are no lines to draw the outlines with.</param>
    internal static void OutlineWindows(bool draws)
    {
        (EditMesh Mesh, Transform Placed, long Frame)[] outlined;
        lock (Outlined)
        {
            // A window drawn in the app before, as the last test's was, is neither outlined in this
            // one nor holding its pointer.
            if (!ReferenceEquals(App.Running, _outlinedApp))
            {
                Outlined.Clear();
                Volatile.Write(ref _pointerOn, -2);
                _outlinedApp = App.Running;
            }

            _windowFrame++;
            Outlined.RemoveAll(entry => _windowFrame - entry.Frame > 2);
            if (!draws) return;
            outlined = [.. Outlined];
        }

        foreach (var (mesh, placed, _) in outlined)
        {
            if (Windows.TryGetValue(mesh, out var state)) Outlines(mesh, state, placed);
        }
    }

    /// <summary>The face a ray meets first, the ray in the world and the mesh drawn where a transform puts it, or nothing.</summary>
    /// <remarks>What a click picks, tested against each face as its triangles, so a face is picked wherever it is hit.</remarks>
    internal static int? Pick(EditMesh mesh, Transform placed, Vec3 origin, Vec3 direction)
    {
        var (from, along) = ToLocal(placed, origin, direction);
        return mesh.Cast(from, along)?.Face;
    }

    /// <summary>
    /// How far along a face's normal from its middle the point nearest a ray is, in whole grid
    /// steps, which a drag extrudes or moves the face by.
    /// </summary>
    /// <param name="middle">The face's middle.</param>
    /// <param name="normal">Its normal, a length of one.</param>
    /// <param name="origin">Where the ray starts.</param>
    /// <param name="direction">Which way it goes, a length of one.</param>
    /// <param name="step">The grid step the distance is rounded to.</param>
    /// <returns>The distance, nothing where the ray runs along the normal and meets no point of it nearer than another.</returns>
    internal static float DragDistance(Vec3 middle, Vec3 normal, Vec3 origin, Vec3 direction, float step)
    {
        // The closest points of two lines, the normal's through the middle and the ray's, by the
        // usual pair of dot products.
        var between = middle - origin;
        var b = Vec3.Dot(normal, direction);
        var d = Vec3.Dot(normal, between);
        var e = Vec3.Dot(direction, between);
        var denominator = 1f - b * b;
        if (denominator < 1e-6f) return 0f;

        var along = (b * e - d) / denominator;
        return MathF.Round(along / step, MidpointRounding.AwayFromZero) * step;
    }

    /// <summary>A ray in the world turned into the mesh's own space, where the transform draws it.</summary>
    private static (Vec3 Origin, Vec3 Direction) ToLocal(Transform placed, Vec3 origin, Vec3 direction)
    {
        var back = placed.Rotation.Conjugate;
        var scale = placed.Scale;
        var from = back * (origin - placed.Translation);
        var along = back * direction;
        from = new Vec3(from.X / scale.X, from.Y / scale.Y, from.Z / scale.Z);
        along = new Vec3(along.X / scale.X, along.Y / scale.Y, along.Z / scale.Z).Normalized;
        return (from, along);
    }

    private static Vec3 ToWorld(Transform placed, Vec3 point) =>
        placed.Translation + placed.Rotation * new Vec3(point.X * placed.Scale.X, point.Y * placed.Scale.Y, point.Z * placed.Scale.Z);

    private static void Summary(EditMesh mesh, WindowState state)
    {
        ImGui.TextUnformatted($"{mesh.VertexCount} vertices, {mesh.EdgeCount} edges, {mesh.FaceCount} faces");
        ImGui.TextUnformatted($"{mesh.SelectedFaceCount} faces and {mesh.SelectedVertexCount} vertices selected");

        // The grid step from a sixteenth to sixty-four, as its line takes it.
        var steps = Enumerable.Range(-4, 11).Select(power => MathF.Pow(2f, power)).ToArray();
        var current = Array.IndexOf(steps, mesh.GridStep);
        var labels = steps.Select(step => step.ToString(CultureInfo.InvariantCulture) + " m").ToArray();
        ImGui.SetNextItemWidth(120f);
        if (ImGui.Combo("grid##bcs-model-grid", ref current, labels, labels.Length)) Apply(mesh, state, $"grid {steps[current].ToString(CultureInfo.InvariantCulture)}");

        foreach (var (word, line) in (ReadOnlySpan<(string, string)>)[("all", "select all"), ("none", "select none"), ("invert", "select invert")])
        {
            ImGui.SameLine();
            if (ImGui.SmallButton($"{word}##bcs-model-select")) Apply(mesh, state, line);
        }

        foreach (var way in (ReadOnlySpan<string>)["+x", "-x", "+y", "-y", "+z", "-z"])
        {
            if (way != "+x") ImGui.SameLine();
            if (ImGui.SmallButton($"{way}##bcs-model-facing")) Apply(mesh, state, $"select {way}");
        }

        if (state.Problem is { } problem) ImGui.TextColored(Warning, problem);
    }

    private static void Recipe(EditMesh mesh, float height)
    {
        ImGui.BeginDisabled(!mesh.CanUndo);
        if (ImGui.Button("Undo##bcs-model")) mesh.Undo();
        ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.BeginDisabled(!mesh.CanRedo);
        if (ImGui.Button("Redo##bcs-model")) mesh.Redo();
        ImGui.EndDisabled();

        if (ImGui.BeginChild("recipe##bcs-model", new NumericsVector2(0f, height), ImGuiChildFlags.Borders))
        {
            for (var line = 0; line < mesh.Recipe.Count; line++) ImGui.TextUnformatted(mesh.Recipe[line]);

            // Kept at its end as lines are added, as a log is.
            if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY()) ImGui.SetScrollHereY(1f);
        }

        ImGui.EndChild();
    }

    private static void Operations(BehaviorContext ctx, EditMesh mesh, WindowState state, Entity camera, Transform placed)
    {
        foreach (var group in Offered.Select(offer => offer.Group).Distinct())
        {
            if (!ImGui.CollapsingHeader($"{group}##bcs-model-group", group == "Add" ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None)) continue;
            if (group == "Texture") Coordinates(ctx, mesh, state, camera, placed);

            foreach (var (_, verb, fields) in Offered.Where(offer => offer.Group == group))
            {
                var words = new List<string> { verb };
                for (var i = 0; i < fields.Length; i++)
                {
                    var key = $"{verb}/{i}";
                    if (!state.Fields.TryGetValue(key, out var value)) value = fields[i].Start;

                    ImGui.SetNextItemWidth(i == 0 ? 150f : 90f);
                    if (i > 0) ImGui.SameLine();
                    ImGui.InputTextWithHint($"##{key}", fields[i].Label, ref value, 128);
                    state.Fields[key] = value;

                    if (value.Trim().Length == 0) continue;
                    if (fields[i].Word is { } before) words.Add(before);
                    words.Add(value.Trim());
                }

                if (fields.Length > 0) ImGui.SameLine();
                if (ImGui.Button($"{verb}##bcs-model-run")) Apply(mesh, state, string.Join(' ', words));
            }
        }
    }

    /// <summary>
    /// The projections that take no measure as buttons, the one along the view among them, and the
    /// selected faces' coordinates over their slot's picture.
    /// </summary>
    private static void Coordinates(BehaviorContext ctx, EditMesh mesh, WindowState state, Entity camera, Transform placed)
    {
        foreach (var (label, line) in (ReadOnlySpan<(string, string)>)[("box", "uv box"), ("sphere", "uv sphere"), ("reset", "uv reset")])
        {
            if (label != "box") ImGui.SameLine();
            if (ImGui.SmallButton($"{label}##bcs-model-uv")) Apply(mesh, state, line);
        }

        // Along the view, the way from the faces back toward the camera, in the mesh's own space.
        if (!camera.IsNone && ctx.Ecs.TryGet<GlobalTransform>(camera, out var eye))
        {
            ImGui.SameLine();
            if (ImGui.SmallButton("from the view##bcs-model-uv"))
            {
                var facing = placed.Rotation.Conjugate * -eye.Forward;
                Apply(mesh, state, string.Create(CultureInfo.InvariantCulture, $"uv planar {facing.X} {facing.Y} {facing.Z}"));
            }
        }

        Preview(mesh);
    }

    /// <summary>The selected faces' coordinates drawn over their slot's picture, repeated as far as they reach.</summary>
    private static void Preview(EditMesh mesh)
    {
        var faces = Enumerable.Range(0, mesh.FaceCount).Where(mesh.IsFaceSelected).ToArray();
        if (faces.Length == 0 || !App.HasRenderer)
        {
            ImGui.TextDisabled("Select faces to see their coordinates over their slot's texture.");
            return;
        }

        var loops = faces.Select(mesh.UvsOf).ToArray();
        var (low, high) = (new Vec2(float.MaxValue), new Vec2(float.MinValue));
        foreach (var uv in loops.SelectMany(loop => loop))
        {
            low = new Vec2(MathF.Min(low.X, uv.X), MathF.Min(low.Y, uv.Y));
            high = new Vec2(MathF.Max(high.X, uv.X), MathF.Max(high.Y, uv.Y));
        }

        // A square of whole repeats from the one the coordinates start in, as many to a side as
        // they reach along the farther of the two.
        var (fromU, fromV) = (MathF.Floor(low.X), MathF.Floor(low.Y));
        var repeats = MathF.Max(1f, MathF.Max(MathF.Ceiling(high.X) - fromU, MathF.Ceiling(high.Y) - fromV));
        var side = MathF.Max(MathF.Min(ImGui.GetContentRegionAvail().X, 240f), 64f);
        var cell = side / repeats;
        var origin = ImGui.GetCursorScreenPos();
        var draw = ImGui.GetWindowDrawList();

        // The picture once for each repeat, up to eight to a side, past which the lines are drawn
        // over the dark, which a wall of sixty-four meters on a meter's grid would reach.
        draw.AddRectFilled(origin, origin + new NumericsVector2(side, side), ImGui.GetColorU32(new NumericsVector4(0.1f, 0.1f, 0.1f, 1f)));
        var slot = mesh.SlotOf(faces[0]);
        var picture = ImGuiTextures.Of(PictureOf(mesh.LookOf(slot)));
        var drawn = (int)MathF.Min(repeats, 8f);
        for (var x = 0; x < drawn && picture != 0; x++)
        {
            for (var y = 0; y < drawn; y++)
                draw.AddImage((IntPtr)picture, origin + new NumericsVector2(x * cell, y * cell), origin + new NumericsVector2((x + 1) * cell, (y + 1) * cell));
        }

        var color = ImGui.GetColorU32(new NumericsVector4(1f, 0.55f, 0.1f, 1f));
        foreach (var loop in loops)
        {
            for (var i = 0; i < loop.Length; i++)
            {
                var (a, b) = (loop[i], loop[(i + 1) % loop.Length]);
                draw.AddLine(
                    origin + new NumericsVector2((a.X - fromU) * cell, (a.Y - fromV) * cell),
                    origin + new NumericsVector2((b.X - fromU) * cell, (b.Y - fromV) * cell),
                    color,
                    1.5f);
            }
        }

        ImGui.Dummy(new NumericsVector2(side, side));
        ImGui.TextDisabled(string.Create(CultureInfo.InvariantCulture, $"slot {slot}, {repeats} to a side from ({fromU}, {fromV})"));
    }

    /// <summary>Runs a line on the mesh, saying why where it cannot run.</summary>
    /// <remarks>A density typed with one number takes the width of its slot's texture, as the console's command does.</remarks>
    private static void Apply(EditMesh mesh, WindowState state, string line)
    {
        try
        {
            mesh.Run(Completed(mesh, line));
            state.Problem = null;
        }
        catch (Exception error) when (error is FormatException or ArgumentException or InvalidOperationException or IOException)
        {
            state.Problem = $"{line}: {error.Message}";
        }
    }

    /// <summary>Picks the face under the pointer, and selects, extrudes or moves it as the press and the drag say.</summary>
    private static void Pointer(BehaviorContext ctx, EditMesh mesh, Entity camera, Transform placed, WindowState state)
    {
        var input = ctx.Input;
        var (x, y) = input.MousePosition;
        var over = ImGuiRuntime.WantsMouse;

        if (!Render.TryRay(camera, x, y, out var origin, out var direction))
        {
            state.Hovered = -1;
            return;
        }

        state.Hovered = over && state.Pressed < 0 ? -1 : Pick(mesh, placed, origin, direction) ?? -1;

        if (input.MousePressed(MouseButton.Left) && !over && state.Hovered >= 0)
        {
            (state.Pressed, state.PressedAt, state.Dragging, state.Moving, state.Distance) = (state.Hovered, (x, y), false, false, 0f);
        }

        // The pointer is the window's while it is over a face or a press on one is held, a face
        // hovered under a window of the interface counting only once a press began on it.
        if (state.Hovered >= 0 || state.Pressed >= 0) Volatile.Write(ref _pointerOn, _windowFrame);

        if (state.Pressed < 0) return;

        if (input.MouseDown(MouseButton.Left))
        {
            // A press becomes a drag once the pointer has gone a few pixels, so a click that shakes
            // a little still picks.
            if (!state.Dragging && MathF.Abs(x - state.PressedAt.X) + MathF.Abs(y - state.PressedAt.Y) > 4f) state.Dragging = true;
            if (state.Dragging)
            {
                var (from, along) = ToLocal(placed, origin, direction);
                state.Distance = DragDistance(mesh.Center(state.Pressed), mesh.Normal(state.Pressed), from, along, mesh.GridStep);
                if (ImGuiRuntime.IsRunning) ImGui.SetTooltip($"{state.Distance.ToString(CultureInfo.InvariantCulture)} m");
            }

            // Alt read while the button is held rather than at its release, so letting go of both in
            // the same frame still moves the face.
            state.Moving = input.KeyDown(Key.AltLeft) || input.KeyDown(Key.AltRight);

            return;
        }

        if (!input.MouseReleased(MouseButton.Left)) return;

        var (pressed, dragged) = (state.Pressed, state.Dragging);
        (state.Pressed, state.Dragging) = (-1, false);
        var shift = input.KeyDown(Key.ShiftLeft) || input.KeyDown(Key.ShiftRight);
        var control = input.KeyDown(Key.ControlLeft) || input.KeyDown(Key.ControlRight);

        if (!dragged)
        {
            var mode = shift ? "add " : control ? "remove " : "";
            Apply(mesh, state, $"select {mode}face {pressed}");
            return;
        }

        if (state.Distance == 0f) return;

        // The face dragged goes with the rest of the selection, selected first where it was not.
        if (!mesh.IsFaceSelected(pressed)) Apply(mesh, state, $"select face {pressed}");

        var distance = state.Distance.ToString(CultureInfo.InvariantCulture);
        if (state.Moving)
        {
            var by = mesh.Normal(pressed) * state.Distance;
            Apply(mesh, state, string.Create(CultureInfo.InvariantCulture, $"move {by.X} {by.Y} {by.Z}"));
        }
        else
        {
            Apply(mesh, state, $"extrude {distance}");
        }
    }

    /// <summary>Outlines the face under the pointer, the selection, and where a drag would put the face pressed.</summary>
    private static void Outlines(EditMesh mesh, WindowState state, Transform placed)
    {
        for (var face = 0; face < mesh.FaceCount; face++)
        {
            if (mesh.IsFaceSelected(face)) Outline(mesh, face, placed, Vec3.Zero, (1f, 0.55f, 0.1f, 1f));
        }

        if (state.Hovered >= 0 && state.Hovered < mesh.FaceCount) Outline(mesh, state.Hovered, placed, Vec3.Zero, (1f, 0.95f, 0.2f, 1f));

        if (state.Dragging && state.Pressed >= 0 && state.Pressed < mesh.FaceCount)
            Outline(mesh, state.Pressed, placed, mesh.Normal(state.Pressed) * state.Distance, (0.3f, 0.85f, 1f, 1f));
    }

    private static void Outline(EditMesh mesh, int face, Transform placed, Vec3 offset, (float R, float G, float B, float A) color)
    {
        var corners = mesh.Corners(face);
        for (var i = 0; i < corners.Length; i++)
        {
            var from = ToWorld(placed, mesh.Position(corners[i]) + offset);
            var to = ToWorld(placed, mesh.Position(corners[(i + 1) % corners.Length]) + offset);
            Gizmos.Line(from, to, color);
        }
    }
}
