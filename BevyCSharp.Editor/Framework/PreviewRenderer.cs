using Bevy;
using ImGuiNET;
using System.Numerics;

namespace BevyCSharp.Editor.Framework;

/// <summary>What a preview draws.</summary>
internal abstract record PreviewSubject
{
    private PreviewSubject()
    {
    }

    /// <summary>A glTF file's scene, as the file lays it out.</summary>
    /// <param name="Path">The file, under the asset root.</param>
    internal sealed record Scene(string Path) : PreviewSubject;

    /// <summary>A mesh on its own, in a plain gray.</summary>
    internal sealed record Mesh(AssetHandle Handle) : PreviewSubject;

    /// <summary>A material on a sphere.</summary>
    internal sealed record Material(AssetHandle Handle) : PreviewSubject;
}

/// <summary>
/// Pictures of assets, each drawn by a camera of its own into an image the interface shows.
/// </summary>
/// <remarks>
/// <para>
/// The asset browser, the Mesh card and the Material card all draw an asset, and three ways of
/// doing it would be three cameras, three lights and three framings to keep alike. So there is one
/// way, and a small pool of places to do it in. A slot is a render target, a camera, a light and
/// what is shown, all on a render layer bit of the slot's own, so no camera sees another slot's
/// subject or the project's world, and the world's camera sees none of them.
/// </para>
/// <para>
/// A slot is asked for by a key each frame it is wanted. One nobody asked for this frame is put
/// away after the panels have drawn, because a camera drawing into an image costs a render pass a
/// frame whether anybody looks at the picture or not. With more keys than slots, the one asked for
/// least recently is taken over.
/// </para>
/// <para>
/// A picture can be turned by dragging it, and the subject drawn as its edges. The framing follows
/// the subject's bounds, since a model is authored at whatever size its maker chose.
/// </para>
/// </remarks>
internal static class PreviewRenderer
{
    /// <summary>How large each picture is, in pixels.</summary>
    private const uint Side = 256;

    /// <summary>The first render layer bit a slot takes.</summary>
    /// <remarks>
    /// High enough to be somewhere nothing else would think to put anything, since a layer is a
    /// bit in a mask and a game reaching for a spare one starts at the bottom.
    /// </remarks>
    private const int FirstLayer = 12;

    /// <summary>The dark gray a card's picture is drawn on, linear.</summary>
    private static readonly (float R, float G, float B, float A) Swatch = (0.09f, 0.09f, 0.11f, 1f);

    /// <summary>How wide each camera sees, in degrees, which the framing works back from.</summary>
    private const float FieldOfView = 35f;

    /// <summary>The slot for each key, at most this many at once.</summary>
    private static readonly Slot?[] Pool = new Slot?[4];

    /// <summary>What a mesh is shown in, made the first time one is.</summary>
    private static AssetHandle _plain;

    /// <summary>What a material is shown on, made the first time one is.</summary>
    private static AssetHandle _sphere;

    /// <summary>One place a picture is drawn.</summary>
    private sealed class Slot(int index)
    {
        public uint Layer { get; } = 1u << (FirstLayer + index);

        public string Key { get; set; } = string.Empty;

        public PreviewSubject? Showing { get; set; }

        public AssetHandle Target { get; set; }

        public Entity Camera { get; set; }

        public Entity Light { get; set; }

        public Entity Subject { get; set; }

        public ulong Asked { get; set; }

        /// <summary>How far round the subject the camera is, in radians.</summary>
        public float Yaw { get; set; } = 0.55f;

        /// <summary>How far above it.</summary>
        public float Pitch { get; set; } = 0.42f;

        public bool Wireframe { get; set; }

        /// <summary>What the camera clears to, which a different one asked for makes it again.</summary>
        public (float R, float G, float B, float A) Clear { get; set; } = Swatch;

        /// <summary>Whether the edges were turned off since the last frame, which every entity has to be told once.</summary>
        public bool Unwire { get; set; }

        public HashSet<Entity> Owned { get; } = [];
    }

    /// <summary>
    /// Asks for a picture of a subject under a key this frame, making or moving a slot for it.
    /// </summary>
    /// <param name="ctx">This frame.</param>
    /// <param name="key">Who is asking, which keeps one picture per place it is shown.</param>
    /// <param name="subject">What to draw, or nothing to show nothing.</param>
    /// <param name="clear">
    /// What the picture's background is, linear with alpha, or nothing for the dark swatch a card
    /// shows. A thumbnail asks for its own, transparent unless a person set one.
    /// </param>
    /// <returns>Whether there is a picture to show.</returns>
    internal static bool Show(
        BehaviorContext ctx, string key, PreviewSubject? subject, (float R, float G, float B, float A)? clear = null)
    {
        if (!App.HasRenderer || subject is null) return false;

        var slot = Take(ctx, key);
        slot.Asked = EditorShell.Frame;

        // A camera's background is set when it is made, so another one is a camera made again.
        var wanted = clear ?? Swatch;
        if (slot.Clear != wanted)
        {
            if (!slot.Camera.IsNone)
            {
                slot.Owned.Remove(slot.Camera);
                ctx.Ecs.Despawn(slot.Camera);
                slot.Camera = Entity.None;
            }

            slot.Clear = wanted;
        }

        if (slot.Showing != subject)
        {
            Clear(ctx, slot);
            slot.Showing = subject;
            slot.Subject = Spawn(ctx, subject);
        }

        Build(ctx, slot);
        Walk(ctx, slot, slot.Subject);
        slot.Unwire = false;
        Frame(ctx, slot);

        return !slot.Subject.IsNone;
    }

    /// <summary>Draws a key's picture at the cursor, turned by dragging it when asked to be.</summary>
    /// <param name="key">Whose picture.</param>
    /// <param name="size">How large, in logical pixels.</param>
    /// <param name="turnable">Whether a drag across it turns the subject.</param>
    internal static void Draw(string key, float size, bool turnable)
    {
        if (Find(key) is not { } slot) return;

        var picture = ImGuiTextures.Of(slot.Target);
        var at = ImGui.GetCursorScreenPos();

        ImGui.InvisibleButton($"##preview-{key}", new Vector2(size, size));

        // Across turns it round, down tips it toward the top, stopping short of straight down so the
        // camera's up never lines up with where it looks.
        if (turnable && ImGui.IsItemActive())
        {
            var moved = ImGui.GetIO().MouseDelta;
            slot.Yaw -= moved.X * 0.01f;
            slot.Pitch = Math.Clamp(slot.Pitch + (moved.Y * 0.01f), -1.3f, 1.3f);
        }

        if (turnable && ImGui.IsItemHovered()) EditorWidgets.Tip("Drag to turn it.");

        if (picture == 0) return;

        ImGui.GetWindowDrawList().AddImageRounded(
            (IntPtr)picture,
            at,
            at + new Vector2(size, size),
            Vector2.Zero,
            Vector2.One,
            0xFFFFFFFF,
            ImGui.GetStyle().FrameRounding);
    }

    /// <summary>Draws a key's subject as its edges, or stops.</summary>
    internal static void SetWireframe(string key, bool on)
    {
        if (Find(key) is not { } slot || slot.Wireframe == on) return;

        slot.Wireframe = on;
        slot.Unwire = !on;
    }

    /// <summary>The image a key's picture is drawn into, or no handle when it has no slot.</summary>
    internal static AssetHandle TargetOf(string key) => Find(key)?.Target ?? AssetHandle.None;

    /// <summary>Whether a key's subject is drawn as its edges.</summary>
    internal static bool Wireframe(string key) => Find(key)?.Wireframe ?? false;

    /// <summary>Whether an entity is part of a preview rather than of the project.</summary>
    internal static bool Owns(Entity entity)
    {
        foreach (var slot in Pool)
            if (slot is not null && slot.Owned.Contains(entity)) return true;

        return false;
    }

    /// <summary>
    /// Puts away every slot nothing asked for this frame.
    /// </summary>
    /// <remarks>
    /// Called after the panels have drawn, so what decides is whether any of them wanted a picture
    /// this frame.
    /// </remarks>
    internal static void Keep(BehaviorContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        for (var i = 0; i < Pool.Length; i++)
        {
            if (Pool[i] is not { } slot || slot.Asked == EditorShell.Frame) continue;

            Clear(ctx, slot);
            if (!slot.Camera.IsNone) ctx.Ecs.Despawn(slot.Camera);
            if (!slot.Light.IsNone) ctx.Ecs.Despawn(slot.Light);
            Pool[i] = null;
        }
    }

    /// <summary>The slot a key has, or nothing.</summary>
    private static Slot? Find(string key) => Pool.FirstOrDefault(slot => slot?.Key == key);

    /// <summary>The slot for a key: its own, a free one, or the one asked for least recently.</summary>
    private static Slot Take(BehaviorContext ctx, string key)
    {
        if (Find(key) is { } held) return held;

        for (var i = 0; i < Pool.Length; i++)
        {
            if (Pool[i] is not null) continue;

            Pool[i] = new Slot(i) { Key = key };
            return Pool[i]!;
        }

        // Every slot is in use, so the stalest is handed over, its subject cleared so the new key
        // does not show the old picture for a frame.
        var oldest = Pool.OrderBy(slot => slot!.Asked).First()!;
        Clear(ctx, oldest);
        oldest.Key = key;
        oldest.Yaw = 0.55f;
        oldest.Pitch = 0.42f;
        oldest.Wireframe = false;
        return oldest;
    }

    /// <summary>Takes away what a slot shows, leaving its camera and light.</summary>
    private static void Clear(BehaviorContext ctx, Slot slot)
    {
        if (!slot.Subject.IsNone && ctx.Ecs.IsAlive(slot.Subject)) ctx.Ecs.Despawn(slot.Subject);

        slot.Subject = Entity.None;
        slot.Showing = null;
        slot.Owned.Clear();
        if (!slot.Camera.IsNone) slot.Owned.Add(slot.Camera);
        if (!slot.Light.IsNone) slot.Owned.Add(slot.Light);
    }

    /// <summary>Spawns what a subject is drawn as.</summary>
    private static Entity Spawn(BehaviorContext ctx, PreviewSubject subject)
    {
        switch (subject)
        {
            case PreviewSubject.Scene scene:
                return ctx.Ecs.SpawnScene(AssetServer.LoadGltfScene(scene.Path));

            case PreviewSubject.Mesh mesh when mesh.Handle.IsValid:
            {
                if (!_plain.IsValid)
                    _plain = Render.CreateMaterial(new MaterialSettings { BaseColor = (0.72f, 0.73f, 0.76f, 1f), Roughness = 0.55f });

                var shown = ctx.Ecs.Spawn();
                ctx.Ecs.Add(shown, Transform.Identity);
                Render.SetMesh(ctx.Ecs, shown, mesh.Handle);
                Render.SetMaterial(ctx.Ecs, shown, _plain);
                return shown;
            }

            case PreviewSubject.Material material when material.Handle.IsValid:
            {
                if (!_sphere.IsValid) _sphere = Render.CreateMesh(MeshShape.Sphere, 0.5f);

                var shown = ctx.Ecs.Spawn();
                ctx.Ecs.Add(shown, Transform.Identity);
                Render.SetMesh(ctx.Ecs, shown, _sphere);
                Render.SetMaterial(ctx.Ecs, shown, material.Handle);
                return shown;
            }

            default:
                return Entity.None;
        }
    }

    /// <summary>Makes a slot's image, camera and light, the first time it is wanted.</summary>
    private static void Build(BehaviorContext ctx, Slot slot)
    {
        if (!slot.Target.IsValid) slot.Target = Render.CreateTarget(Side, Side);

        if (slot.Camera.IsNone)
        {
            slot.Camera = Render.SpawnCamera3d(new CameraSettings
            {
                FieldOfView = FieldOfView,
                Layers = slot.Layer,

                // Its own color rather than the world's, so the picture reads as a swatch of the
                // subject rather than as a window onto the scene behind the panel.
                Clear = ClearMode.Custom,
                ClearColor = slot.Clear,

                // After the main view, so nothing about the order it is drawn in can disturb it.
                Order = 8,
            });

            Render.SetCameraTarget(slot.Camera, slot.Target);
            slot.Owned.Add(slot.Camera);
        }

        if (slot.Light.IsNone)
        {
            slot.Light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 9_000f });
            Render.SetLayers(ctx.Ecs, slot.Light, slot.Layer);
            ctx.Ecs.Add(slot.Light, Transform.LookingAt(new Vec3(3f, 5f, 4f), Vec3.Zero, Vec3.UnitY));
            slot.Owned.Add(slot.Light);
        }
    }

    /// <summary>Puts a subject and everything under it on the slot's layer, and its edges on or off.</summary>
    /// <remarks>
    /// Every frame rather than once, because a glTF scene's children arrive frames after it is
    /// asked for, and writing the layer again costs a component insert on a handful of entities.
    /// </remarks>
    private static void Walk(BehaviorContext ctx, Slot slot, Entity entity)
    {
        if (entity.IsNone || !ctx.Ecs.IsAlive(entity)) return;

        Render.SetLayers(ctx.Ecs, entity, slot.Layer);
        slot.Owned.Add(entity);

        if ((slot.Wireframe || slot.Unwire) && Render.IsDrawn(entity))
            Render.SetWireframe(entity, slot.Wireframe, (0.85f, 0.88f, 0.95f, 1f));

        foreach (var child in ctx.Ecs.ChildrenOf(entity)) Walk(ctx, slot, child);
    }

    /// <summary>Points the camera at the subject from where the drag has turned it to.</summary>
    private static void Frame(BehaviorContext ctx, Slot slot)
    {
        if (slot.Subject.IsNone || slot.Camera.IsNone) return;

        var min = new Vec3(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new Vec3(float.MinValue, float.MinValue, float.MinValue);
        var any = false;

        Reach(ctx, slot.Subject, ref min, ref max, ref any);

        // Nothing drawn yet, which is ordinary on the frames between asking for a file and its
        // meshes arriving.
        if (!any) return;

        var middle = (min + max) * 0.5f;
        var radius = (max - min).Length * 0.5f;
        if (radius <= 0f) radius = 0.5f;

        // Far enough back that a ball round the whole of it fits the field of view, from any side
        // the drag turns it to, with a little air. A box's corners reach past its widest face, so
        // its size alone puts them out of the picture.
        var away = radius / MathF.Sin(FieldOfView * 0.5f * MathF.PI / 180f) * 1.08f;
        var toward = new Vec3(
            MathF.Cos(slot.Pitch) * MathF.Sin(slot.Yaw),
            MathF.Sin(slot.Pitch),
            MathF.Cos(slot.Pitch) * MathF.Cos(slot.Yaw));

        ctx.Ecs.Add(slot.Camera, Transform.LookingAt(middle + (toward * away), middle, Vec3.UnitY));
    }

    /// <summary>Grows a box to hold an entity and everything under it.</summary>
    private static void Reach(BehaviorContext ctx, Entity entity, ref Vec3 min, ref Vec3 max, ref bool any)
    {
        if (Render.TryGetBounds(entity, out var low, out var high))
        {
            min = new Vec3(MathF.Min(min.X, low.X), MathF.Min(min.Y, low.Y), MathF.Min(min.Z, low.Z));
            max = new Vec3(MathF.Max(max.X, high.X), MathF.Max(max.Y, high.Y), MathF.Max(max.Z, high.Z));
            any = true;
        }

        foreach (var child in ctx.Ecs.ChildrenOf(entity)) Reach(ctx, child, ref min, ref max, ref any);
    }
}
