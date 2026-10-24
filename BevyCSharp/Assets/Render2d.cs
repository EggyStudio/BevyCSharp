using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Draws in two dimensions, a camera that measures in pixels, and sprites to put under it.
/// </summary>
/// <remarks>
/// Separate from <see cref="Render"/> because the two are different ways of looking at the world
/// rather than different things to draw. Entities, transforms and parenting are the same
/// underneath, so a sprite can carry any component and be parented to anything.
/// </remarks>
/// <example>
/// <code>
/// Render2d.SpawnCamera2d();
///
/// var badge = ctx.Ecs.Spawn();
/// Render2d.SetSprite(ctx.Ecs, badge, AssetServer.Load(AssetKind.Image, "ui/badge.png"));
/// ctx.Ecs.Add(badge, Transform.At(120f, -80f, 0f));
/// </code>
/// </example>
public static unsafe partial class Render2d
{
    /// <summary>
    /// Spawns a 2D camera and returns it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One world unit is one pixel, and the origin is the middle of the window, so a sprite at
    /// <c>(100, 50)</c> sits a hundred pixels right and fifty up from the center.
    /// </para>
    /// <para>
    /// A 2D camera draws the world's gizmos too, a world unit a pixel, so one laid over a 3D scene
    /// that draws gizmos shows each of them within a few units of the world's middle as a mark a
    /// few pixels wide at the middle of the window. Such a camera is put on a layer of its own with
    /// <see cref="Render.SetLayers"/>, its sprites with it, where no gizmo is drawn.
    /// </para>
    /// </remarks>
    /// <param name="order">
    /// Draw order. Leave at zero for a 2D-only game. Above a 3D camera's order it draws over the
    /// scene without clearing it, which is how a 2D overlay is layered on a 3D one.
    /// </param>
    /// <returns><see cref="Entity.None"/> on a build with no renderer.</returns>
    public static Entity SpawnCamera2d(int order = 0) =>
        new(Native.bcs_render_spawn_camera_2d(order));

    /// <summary>
    /// Builds an atlas layout over a grid of equal tiles and returns it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The layout is a list of rectangles and nothing else, so it says where each frame sits while
    /// the image it describes stays a separate asset. No image is passed here for that reason,
    /// and one layout serves every sheet cut the same way.
    /// </para>
    /// <para>
    /// Frames are numbered across each row and then down, starting at zero.
    /// </para>
    /// </remarks>
    /// <param name="tileWidth">Width of one frame, in pixels.</param>
    /// <param name="tileHeight">Height of one frame, in pixels.</param>
    /// <param name="columns">How many frames across.</param>
    /// <param name="rows">How many frames down.</param>
    /// <param name="padding">Gap between neighboring frames, in pixels.</param>
    /// <param name="offset">Margin before the first frame, in pixels.</param>
    /// <exception cref="BevyNativeException">A dimension is zero, or no app is running.</exception>
    /// <example>
    /// <code>
    /// var sheet = AssetServer.Load(AssetKind.Image, "sprites/walk.png");
    /// var frames = Render2d.CreateAtlas(32, 32, columns: 8, rows: 1);
    ///
    /// Render2d.SetSprite(ctx.Ecs, walker, sheet, new SpriteSettings
    /// {
    ///     Atlas = frames,
    ///     Frame = step % 8,
    ///     Anchor = SpriteAnchor.BottomCenter,
    /// });
    /// </code>
    /// </example>
    public static AssetHandle CreateAtlas(
        uint tileWidth,
        uint tileHeight,
        uint columns,
        uint rows,
        (uint X, uint Y) padding = default,
        (uint X, uint Y) offset = default)
    {
        var key = Native.bcs_atlas_create(
            tileWidth, tileHeight, columns, rows, padding.X, padding.Y, offset.X, offset.Y);
        Native.Check(key, "building an atlas layout");

        return new AssetHandle(key);
    }

    /// <summary>
    /// Packs images into one atlas as the app runs, as Bevy's <c>TextureAtlasBuilder</c> does, or
    /// answers <see langword="null"/> while any of them is still loading. Only valid inside a
    /// system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For sprites drawn as separate files and packed into one image, so they are drawn from one
    /// texture, as Bevy's texture_atlas packs a folder of them. The packed image is a copy, kept
    /// as an asset of its own, and each image's frame in it is
    /// <see cref="PackedAtlas.Indices"/>, in the order they were given, for
    /// <see cref="SpriteSettings.Frame"/>.
    /// </para>
    /// <para>
    /// <paramref name="padding"/> leaves a gap of pixels between neighboring images, which keeps a
    /// sprite sampled between pixels, as one drawn scaled with linear filtering is, from taking
    /// color from its neighbor's edge.
    /// </para>
    /// </remarks>
    /// <param name="images">The images to pack.</param>
    /// <param name="padding">The gap between neighboring images, in pixels.</param>
    /// <exception cref="BevyNativeException">
    /// A handle names no image, or the images do not fit the largest atlas Bevy packs.
    /// </exception>
    public static PackedAtlas? PackAtlas(ReadOnlySpan<AssetHandle> images, (uint X, uint Y) padding = default)
    {
        if (images.IsEmpty) throw new ArgumentException("An atlas packs one image at least.", nameof(images));

        var keys = new int[images.Length];
        for (var i = 0; i < keys.Length; i++) keys[i] = images[i].Key;
        var indices = new int[images.Length];
        int packed, layout;

        fixed (int* given = keys)
        fixed (int* placed = indices)
        {
            var answer = Native.bcs_atlas_build(given, keys.Length, padding.X, padding.Y, &packed, &layout, placed);
            if (answer == NativeStatus.NotPresent) return null;
            Native.Check(answer, $"packing {keys.Length} images into an atlas");
        }

        return new PackedAtlas(new AssetHandle(packed), new AssetHandle(layout), [.. indices.Select(index => (uint)index)]);
    }

    /// <summary>Attaches a sprite to an entity, or replaces the one it has.</summary>
    /// <exception cref="BevyNativeException">The handle names no image, or the entity is gone.</exception>
    public static void SetSprite(EcsWorld world, Entity entity, AssetHandle image) =>
        SetSprite(world, entity, image, new SpriteSettings());

    /// <summary>Attaches a sprite drawn as <paramref name="settings"/> describes.</summary>
    /// <exception cref="BevyNativeException">The handle names no image, or the entity is gone.</exception>
    public static void SetSprite(
        EcsWorld world,
        Entity entity,
        AssetHandle image,
        SpriteSettings settings)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeSpriteConfig
        {
            Image = image.Key,
            ColorR = settings.Color.R,
            ColorG = settings.Color.G,
            ColorB = settings.Color.B,
            ColorA = settings.Color.A,
            HasSize = settings.Size is null ? 0 : 1,
            SizeX = settings.Size?.Width ?? 0f,
            SizeY = settings.Size?.Height ?? 0f,
            HasRect = settings.Rect is null ? 0 : 1,
            RectLeft = settings.Rect?.Left ?? 0f,
            RectTop = settings.Rect?.Top ?? 0f,
            RectRight = settings.Rect?.Right ?? 0f,
            RectBottom = settings.Rect?.Bottom ?? 0f,
            FlipX = settings.FlipX ? 1 : 0,
            FlipY = settings.FlipY ? 1 : 0,
            Atlas = settings.Atlas.Key,
            AtlasIndex = settings.Frame,
            HasAnchor = settings.Anchor is null ? 0 : 1,
            AnchorX = settings.Anchor?.X ?? 0f,
            AnchorY = settings.Anchor?.Y ?? 0f,
            Mode = (int)settings.Mode,
            Scaling = (int)settings.Scaling,
            SliceLeft = settings.SliceBorder.Left,
            SliceTop = settings.SliceBorder.Top,
            SliceRight = settings.SliceBorder.Right,
            SliceBottom = settings.SliceBorder.Bottom,
            CornerScale = settings.CornerScale,
            TileX = settings.TileX ? 1 : 0,
            TileY = settings.TileY ? 1 : 0,
            TileStretch = settings.TileStretch,
            SliceTiling = (int)settings.SliceTiling,
        };

        var status = Native.bcs_render_set_sprite(entity.Bits, &native);
        if (status == NativeStatus.Unsupported)
            throw new BevyNativeException(
                NativeStatus.Unsupported,
                "Attaching a sprite failed, because this native build has no renderer. Rebuild the "
                + "bridge with build/build-native.sh --render.");

        Native.Check(status, $"attaching a sprite to {entity}");
    }

    /// <summary>
    /// Moves sprites and sprite meshes to frames of their sheets, each to the frame beside it, in
    /// one call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For an animation over many sprites, which turns many of them a frame. Only the index of each
    /// one's atlas is written, where <see cref="SetSprite(EcsWorld, Entity, AssetHandle, SpriteSettings)"/>
    /// replaces the whole sprite, and the run crosses into the bridge once rather than a call a
    /// sprite.
    /// </para>
    /// <para>
    /// A sprite mesh, which Bevy's reflected <c>SpriteMesh</c> puts on, moves the same way. An
    /// entity that is gone, that has neither, or whose sprite shows no sheet is passed over, which
    /// the count answered says.
    /// </para>
    /// </remarks>
    /// <param name="sprites">The sprites to move.</param>
    /// <param name="frames">The frame each moves to, in the same order.</param>
    /// <returns>How many were moved.</returns>
    /// <exception cref="ArgumentException">The two runs are of different lengths.</exception>
    /// <exception cref="BevyNativeException">
    /// This build has no renderer, or no world is on loan, as outside a system.
    /// </exception>
    public static int SetSpriteFrames(ReadOnlySpan<Entity> sprites, ReadOnlySpan<uint> frames)
    {
        if (sprites.Length != frames.Length)
            throw new ArgumentException("There is a frame for each sprite.", nameof(frames));

        int moved;
        fixed (Entity* entities = sprites)
        fixed (uint* each = frames)
            moved = Native.bcs_sprite_frames((ulong*)entities, each, sprites.Length);

        if (moved == NativeStatus.Unsupported)
            throw new BevyNativeException(
                NativeStatus.Unsupported,
                "Moving sprites' frames failed, because this native build has no renderer. Rebuild "
                + "the bridge with build/build-native.sh --render.");

        return Native.Check(moved, $"moving {sprites.Length} sprites' frames");
    }
}
