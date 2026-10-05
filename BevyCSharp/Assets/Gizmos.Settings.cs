using System.Runtime.InteropServices;
using Bevy.Interop;

namespace Bevy;

public static unsafe partial class Gizmos
{
    /// <summary>
    /// Sets how every gizmo is drawn.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <paramref name="which"/> is the only way one kind of debug drawing is told from another
    /// here. Bevy groups gizmos by a config group, which is a Rust type rather than a value, so
    /// the two a game draws in are the two a shape's <c>inFront</c> already chooses between, and a
    /// game wanting more than that runs out of groups rather than of settings. Bevy's own groups,
    /// for lights and bounding boxes, are set here as well.
    /// </para>
    /// <para>
    /// Those two are enough for the thing usually wanted, because what is drawn *in* a scene and
    /// what is drawn *about* it are already on opposite sides of the split. Turning off the group
    /// the scene can hide takes the floor grid and the paths away and leaves the handles.
    /// </para>
    /// <para>
    /// <paramref name="enabled"/> suits a debug overlay bound to a key, because it stops the
    /// drawing without the systems that ask for it having to know they should stop.
    /// </para>
    /// </remarks>
    /// <param name="width">Line thickness in pixels, or 0 to leave it as it is.</param>
    /// <param name="layers">
    /// Which render layers gizmos appear on, as a bit mask. Only a camera whose layers overlap
    /// draws them. Zero keeps Bevy's own default of layer zero.
    /// </param>
    /// <param name="enabled">Whether to draw gizmos at all.</param>
    /// <param name="which">Which group this applies to.</param>
    /// <exception cref="BevyNativeException">There is nothing to draw on.</exception>
    public static void Configure(
        float width = 0f,
        uint layers = 0,
        bool enabled = true,
        GizmoGroup which = GizmoGroup.Both) =>
        Native.Check(
            Native.bcs_gizmo_configure(width, layers, enabled ? 1 : 0, (int)which),
            "configuring gizmos");

    /// <summary>
    /// Sets what a gizmo line looks like.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Apart from <see cref="Configure"/> because how thick a line is and who can see it is one
    /// decision, and what the line looks like is another. Both cover every gizmo, for the same
    /// reason.
    /// </para>
    /// <para>
    /// A dotted or dashed line tells one meaning from another without a second color, so a path
    /// already walked can be drawn against the one still to come. <see cref="GizmoJoint"/> only
    /// shows on a shape whose lines meet, which is every closed shape and no single segment.
    /// </para>
    /// </remarks>
    /// <param name="style">Whether the line is solid, dotted or dashed.</param>
    /// <param name="gapScale">
    /// How long the gap in a dashed line is, in line widths. Zero takes Bevy's own default of one,
    /// and it is read only for <see cref="GizmoLine.Dashed"/>.
    /// </param>
    /// <param name="lineScale">
    /// How long the drawn run of a dashed line is, in line widths. Zero takes Bevy's own default
    /// of three.
    /// </param>
    /// <param name="joint">How two lines meet at a corner.</param>
    /// <param name="jointResolution">
    /// How many triangles a round joint is drawn with. Zero takes Bevy's own default of four, and
    /// it is read only for <see cref="GizmoJoint.Round"/>.
    /// </param>
    /// <param name="perspective">
    /// Whether the width is a size at the camera's near plane rather than a size on screen, so a
    /// line further away is drawn thinner. Only a perspective 3D camera can honor it.
    /// </param>
    /// <exception cref="BevyNativeException">There is nothing to draw on.</exception>
    public static void SetLineStyle(
        GizmoLine style = GizmoLine.Solid,
        float gapScale = 0f,
        float lineScale = 0f,
        GizmoJoint joint = GizmoJoint.None,
        uint jointResolution = 0,
        bool perspective = false,
        GizmoGroup which = GizmoGroup.Both) =>
        Native.Check(
            Native.bcs_gizmo_style(
                (int)style,
                gapScale,
                lineScale,
                (int)joint,
                jointResolution,
                perspective ? 1 : 0,
                (int)which),
            "styling gizmo lines");

    /// <summary>
    /// Moves a group's gizmos toward the camera or away from it before they are tested against
    /// the scene's depth.
    /// </summary>
    /// <remarks>
    /// From -1, in front of everything, through 0, where they are, to 1, behind everything. The
    /// group drawn with <c>inFront</c> starts at -1, which is how it stays in front, and every
    /// other group at 0. A small negative bias keeps a wireframe drawn over a model from flickering
    /// through its surface, and a key that toggles -1 shows a group through walls on demand.
    /// </remarks>
    /// <param name="bias">From -1 to 1, clamped.</param>
    /// <param name="which">Which groups it applies to.</param>
    /// <exception cref="BevyNativeException">There is nothing to draw on.</exception>
    public static void SetDepthBias(float bias, GizmoGroup which = GizmoGroup.Both) =>
        Native.Check(Native.bcs_gizmo_depth_bias(bias, (int)which), "setting the gizmo depth bias");

    /// <summary>
    /// Sets whether Bevy draws the shape of every light, and how it colors them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A point light is drawn as a sphere of its radius and range, a spot light as its cone, a
    /// directional light as an arrow and a rectangle light as its rectangle. Without
    /// <paramref name="all"/> only lights carrying Bevy's <c>ShowLightGizmo</c> are drawn, which
    /// is reached as <c>ShowLightGizmoRef</c> and also holds a color of its own for one light.
    /// </para>
    /// <para>
    /// How thick the lines are, whether they are drawn at all and whether the scene can hide them
    /// are set for <see cref="GizmoGroup.Lights"/> by <see cref="Configure"/> and
    /// <see cref="SetDepthBias"/>, as for any group.
    /// </para>
    /// </remarks>
    /// <param name="all">Whether every light is drawn.</param>
    /// <param name="coloring">How their colors are chosen.</param>
    /// <param name="color">The one color for <see cref="LightGizmoColoring.Manual"/>, linear RGBA.</param>
    /// <exception cref="BevyNativeException">There is nothing to draw on.</exception>
    public static void ShowLights(
        bool all,
        LightGizmoColoring coloring = LightGizmoColoring.MatchLight,
        (float R, float G, float B, float A) color = default)
    {
        var rgba = stackalloc float[] { color.R, color.G, color.B, color.A };
        Native.Check(Native.bcs_gizmo_lights(all ? 1 : 0, (int)coloring, rgba), "showing light gizmos");
    }

    /// <summary>
    /// Sets whether Bevy draws every entity's bounding box, and in what color.
    /// </summary>
    /// <remarks>
    /// The box Bevy culls an entity by, the one to look at when something vanishes at the edge of
    /// the screen or a picking ray misses what it looks to hit. Without
    /// <paramref name="all"/> only entities carrying Bevy's <c>ShowAabbGizmo</c> are drawn,
    /// reached as <c>ShowAabbGizmoRef</c>. <see cref="GizmoGroup.Bounds"/> sets their lines.
    /// </remarks>
    /// <param name="all">Whether every entity's box is drawn.</param>
    /// <param name="color">One color for all of them, linear RGBA, or null for a color of each box's own.</param>
    /// <exception cref="BevyNativeException">There is nothing to draw on.</exception>
    public static void ShowBounds(bool all, (float R, float G, float B, float A)? color = null)
    {
        if (color is not { } c)
        {
            Native.Check(Native.bcs_gizmo_bounds(all ? 1 : 0, null), "showing bounding boxes");
            return;
        }

        var rgba = stackalloc float[] { c.R, c.G, c.B, c.A };
        Native.Check(Native.bcs_gizmo_bounds(all ? 1 : 0, rgba), "showing bounding boxes");
    }

    /// <summary>
    /// Keeps the shapes <paramref name="draw"/> asks for in an asset, rather than drawing them for
    /// one frame, for an entity to draw every frame with <see cref="Attach"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bevy's retained gizmos. Lines that do not change, an outline, a skeleton, a sphere drawn
    /// with tens of thousands of segments, are made once and drawn from the asset with nothing
    /// asked for again, which costs a fraction of asking for them every frame. Lines that change
    /// stay with the calls above.
    /// </para>
    /// <para>
    /// Every shape call here can be made inside <paramref name="draw"/>, and they all land in the
    /// one asset, whatever their <c>inFront</c> says, since the entity drawing it carries its own
    /// line settings and depth bias. Text is drawn the frame it is asked for rather than kept.
    /// Recordings do not nest. Only valid inside a system.
    /// </para>
    /// </remarks>
    /// <param name="draw">Draws the shapes to keep.</param>
    /// <returns>The asset, for <see cref="Attach"/>.</returns>
    /// <exception cref="BevyNativeException">There is nothing to draw on, or a recording is already open.</exception>
    public static AssetHandle Record(Action draw)
    {
        ArgumentNullException.ThrowIfNull(draw);

        // Anything a batch gathered so far is handed over first, so it is drawn this frame
        // rather than kept, and the recording starts empty.
        var batch = _batch;
        if (batch is { Count: > 0 })
        {
            DrawMany(CollectionsMarshal.AsSpan(batch), "drawing a batch of gizmos");
            batch.Clear();
        }

        Native.Check(Native.bcs_gizmo_record_begin(), "starting a gizmo recording");
        int key;
        try
        {
            draw();
            if (_batch is { Count: > 0 } gathered)
            {
                DrawMany(CollectionsMarshal.AsSpan(gathered), "recording a batch of gizmos");
                gathered.Clear();
            }
        }
        finally
        {
            Native.Check(Native.bcs_gizmo_record_end(&key), "ending a gizmo recording");
        }

        return new AssetHandle(key);
    }

    /// <summary>
    /// Has an entity draw a gizmo asset <see cref="Record"/> made, every frame, placed by the
    /// entity's transform.
    /// </summary>
    /// <remarks>
    /// Bevy's <c>Gizmo</c> component, reached as <c>GizmoRef</c>, whose line width, style, joints
    /// and depth bias are the entity's own rather than a group's and are set on that wrapper. A
    /// width of zero, which a new one starts with, is Bevy's two pixels.
    /// </remarks>
    /// <param name="world">The world the entity is in.</param>
    /// <param name="entity">The entity to draw it.</param>
    /// <param name="gizmo">The asset.</param>
    public static void Attach(EcsWorld world, Entity entity, AssetHandle gizmo)
    {
        ArgumentNullException.ThrowIfNull(world);
        world.InsertReflected(entity, "bevy_gizmos::retained::Gizmo");
        world.SetReflectedAsset(entity, "bevy_gizmos::retained::Gizmo", "handle", gizmo);
    }

    /// <summary>
    /// Fills in the shape every call above builds, so each of them is its own arguments and
    /// nothing else.
    /// </summary>
    private static NativeGizmoConfig Shape(
        int kind,
        Vec3 center,
        Quat rotation,
        (float R, float G, float B, float A) color,
        bool inFront,
        float radius = 0f,
        Vec3 end = default) => new()
    {
        Kind = kind,
        InFront = inFront ? 1 : 0,
        StartX = center.X,
        StartY = center.Y,
        StartZ = center.Z,
        EndX = end.X,
        EndY = end.Y,
        EndZ = end.Z,
        RotationX = rotation.X,
        RotationY = rotation.Y,
        RotationZ = rotation.Z,
        RotationW = rotation.W,
        Radius = radius,
        ColorR = color.R,
        ColorG = color.G,
        ColorB = color.B,
        ColorA = color.A,
    };

    /// <summary>
    /// Draws a set of axes, so an orientation can be read at a glance.
    /// </summary>
    /// <remarks>
    /// Colored by Bevy, red for X, green for Y and blue for Z. Drawing these on an entity is the
    /// quickest way to see whether something is facing where it should. The arms are scaled as
    /// the transform is, so each is as long as <paramref name="length"/> in the entity's own units
    /// and a stretched entity shows it.
    /// </remarks>
    /// <param name="transform">Where the axes sit, which way they point and how they are scaled.</param>
    /// <param name="length">How long each arm is, before the transform's scale.</param>
    /// <param name="inFront">Whether the scene can hide them. See <see cref="Line"/>.</param>
    public static void Axes(Transform transform, float length = 1f, bool inFront = true) =>
        Draw(new NativeGizmoConfig
        {
            Kind = 2,
            InFront = inFront ? 1 : 0,
            StartX = transform.Translation.X,
            StartY = transform.Translation.Y,
            StartZ = transform.Translation.Z,
            RotationX = transform.Rotation.X,
            RotationY = transform.Rotation.Y,
            RotationZ = transform.Rotation.Z,
            RotationW = transform.Rotation.W,
            EndX = transform.Scale.X,
            EndY = transform.Scale.Y,
            EndZ = transform.Scale.Z,
            Radius = length,
            ColorA = 1f,
        });

    /// <summary>The shapes gathered by the batch that is open, or null where none is.</summary>
    private static List<NativeGizmoConfig>? _batch;

    /// <summary>
    /// Gathers every shape drawn until the returned scope ends, and hands them over in one call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each shape otherwise crosses to the engine on its own, which is nothing for a handful and
    /// adds up for a scene drawing thousands of spheres or boxes a frame. Inside a batch every
    /// shape call is kept here instead, and the lot is handed over when the scope is disposed,
    /// as <see cref="Lines"/> hands over its run.
    /// </para>
    /// <para>
    /// Batches do not nest; a batch opened inside another joins it. A shape drawn in a batch whose
    /// scope is never disposed is never drawn, so the scope belongs in a <c>using</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using (Gizmos.Batch())
    /// {
    ///     foreach (var row in ctx.Ecs.Query&lt;Collider&gt;())
    ///         Gizmos.Sphere(row.Position, row.Radius, (0f, 1f, 0f, 1f));
    /// }
    /// </code>
    /// </example>
    public static BatchScope Batch()
    {
        if (_batch is not null) return default;

        _batch = [];
        return new BatchScope(owns: true);
    }

    /// <summary>An open batch, which hands its shapes over when disposed.</summary>
    public readonly struct BatchScope : IDisposable
    {
        private readonly bool _owns;

        internal BatchScope(bool owns) => _owns = owns;

        /// <summary>Hands the gathered shapes to the engine.</summary>
        public void Dispose()
        {
            if (!_owns || _batch is not { } gathered) return;

            _batch = null;
            if (gathered.Count > 0) DrawMany(CollectionsMarshal.AsSpan(gathered), "drawing a batch of gizmos");
        }
    }

    /// <summary>Hands a run of shapes over in one call.</summary>
    private static void DrawMany(ReadOnlySpan<NativeGizmoConfig> configs, string doing)
    {
        fixed (NativeGizmoConfig* at = configs)
        {
            var status = Native.bcs_gizmo_draw_many(at, configs.Length);

            if (status == NativeStatus.Unsupported)
                throw new BevyNativeException(
                    NativeStatus.Unsupported,
                    "Drawing gizmos failed, because gizmos are drawn by a plugin that comes with "
                    + "the window, so a windowless run has nothing to draw on. Guard with "
                    + "App.HasRenderer and Config.Headless.");

            Native.Check(status, doing);
        }
    }

    /// <summary>Draws a run of text in the world, in Bevy's stroke font, facing the way it is turned.</summary>
    /// <param name="text">What to write, in printable ASCII, a line break starting a new line.</param>
    /// <param name="position">Where its anchor sits, in world space.</param>
    /// <param name="rotation">
    /// Which way it faces. Unturned, it reads along X and up Y, so a camera looking down negative Z
    /// reads it.
    /// </param>
    /// <param name="size">The height of a capital letter, in world units.</param>
    /// <param name="anchor">
    /// The point of the text's bounds at <paramref name="position"/>, from minus a half to a half
    /// on each axis: (0, 0) its middle, (-0.5, 0) the middle of its left side, (0, 0.5) the middle
    /// of its top.
    /// </param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    /// <remarks>
    /// <para>
    /// Bevy's text gizmos, drawn as lines in its Simplex stroke font, so a label costs no font
    /// asset, no texture and no entity, and lasts the frame it was asked for like any gizmo. For a
    /// value watched as it changes, a name over what it names, or a measurement beside what it
    /// measures. Text a player reads belongs in <see cref="Ui"/> or on an entity with
    /// <c>Text2d</c>, which are shaped by a real font and kept.
    /// </para>
    /// <para>
    /// The font has the ninety-five printable ASCII characters and draws anything else as a space.
    /// Its lines are as thick as <see cref="Configure"/> sets every gizmo's, in pixels, so a large
    /// letter is drawn in the same thin line as a small one.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">There is nothing to draw on.</exception>
    public static void Text(
        string text,
        Vec3 position,
        Quat rotation,
        float size,
        (float X, float Y) anchor,
        (float R, float G, float B, float A) color,
        bool inFront = true)
    {
        ArgumentNullException.ThrowIfNull(text);

        var settings = new NativeGizmoText { Size = size, InFront = inFront ? 1 : 0 };
        (settings.Position[0], settings.Position[1], settings.Position[2]) = (position.X, position.Y, position.Z);
        (settings.Rotation[0], settings.Rotation[1], settings.Rotation[2], settings.Rotation[3]) = (rotation.X, rotation.Y, rotation.Z, rotation.W);
        (settings.Anchor[0], settings.Anchor[1]) = anchor;
        (settings.Color[0], settings.Color[1], settings.Color[2], settings.Color[3]) = color;

        var status = Native.bcs_gizmo_text(text, &settings);
        if (status == NativeStatus.Unsupported)
            throw new BevyNativeException(
                NativeStatus.Unsupported,
                "Drawing gizmo text failed, because gizmos are drawn by a plugin that comes with the "
                + "window, so a windowless run has nothing to draw on.");

        Native.Check(status, "drawing gizmo text");
    }

    /// <summary>Draws a run of text, flat, for a 2D camera.</summary>
    /// <inheritdoc cref="Text" path="/remarks"/>
    /// <param name="text">What to write, in printable ASCII, a line break starting a new line.</param>
    /// <param name="position">Where its anchor sits, on the XY plane.</param>
    /// <param name="angle">How far it is turned about Z, in radians, counterclockwise.</param>
    /// <param name="size">The height of a capital letter, in pixels under a 2D camera.</param>
    /// <param name="anchor">The point of its bounds at <paramref name="position"/>. See <see cref="Text"/>.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <param name="inFront">Whether the scene can hide it. See <see cref="Line"/>.</param>
    /// <exception cref="BevyNativeException">There is nothing to draw on.</exception>
    public static void Text2d(
        string text,
        (float X, float Y) position,
        float angle,
        float size,
        (float X, float Y) anchor,
        (float R, float G, float B, float A) color,
        bool inFront = true) =>
        Text(text, Flat(position), Turn(angle), size, anchor, color, inFront);

    private static void Draw(NativeGizmoConfig config)
    {
        if (_batch is { } gathering)
        {
            gathering.Add(config);
            return;
        }

        var status = Native.bcs_gizmo_draw(&config);
        if (status == NativeStatus.Unsupported)
            throw new BevyNativeException(
                NativeStatus.Unsupported,
                "Drawing a gizmo failed, because gizmos are drawn by a plugin that comes with the "
                + "window, so a windowless run has nothing to draw on. Guard with App.HasRenderer "
                + "and Config.Headless.");

        Native.Check(status, "drawing a gizmo");
    }
}
