using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Draws in two dimensions: a camera that measures in pixels, and sprites to put under it.
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
public static unsafe class Render2d
{
    /// <summary>
    /// Spawns a 2D camera and returns it.
    /// </summary>
    /// <remarks>
    /// One world unit is one pixel, and the origin is the middle of the window, so a sprite at
    /// <c>(100, 50)</c> sits a hundred pixels right and fifty up from the centre.
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
    /// The layout is a list of rectangles and nothing else: it says where each frame sits, while
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
    /// <param name="padding">Gap between neighbouring frames, in pixels.</param>
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
            SliceLeft = settings.SliceBorder.Left,
            SliceTop = settings.SliceBorder.Top,
            SliceRight = settings.SliceBorder.Right,
            SliceBottom = settings.SliceBorder.Bottom,
            CornerScale = settings.CornerScale,
            TileX = settings.TileX ? 1 : 0,
            TileY = settings.TileY ? 1 : 0,
            TileStretch = settings.TileStretch,
        };

        var status = Native.bcs_render_set_sprite(entity.Bits, &native);
        if (status == NativeStatus.Unsupported)
            throw new BevyNativeException(
                NativeStatus.Unsupported,
                "Attaching a sprite failed: this native build has no renderer. Rebuild the "
                + "bridge with build/build-native.sh --render.");

        Native.Check(status, $"attaching a sprite to {entity}");
    }
}
