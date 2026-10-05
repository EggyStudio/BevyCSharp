using Bevy.Interop;

namespace Bevy;

public static unsafe partial class Render
{
    /// <summary>
    /// Asks for a picture to be read back into memory rather than written to a file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same capture <see cref="Screenshot(string)"/> takes, delivered as bytes instead of as a
    /// PNG. A file is for a person to look at; this is for a program to inspect, so a test can
    /// assert on what was drawn.
    /// </para>
    /// <para>
    /// The picture arrives a frame or two later, because it has to come back off the GPU, so this
    /// answers with a ticket rather than with pixels. Poll
    /// <see cref="TryReadCapture(Capture, out CapturedImage?)"/> until it says yes, from a later
    /// frame rather than in a loop, because the picture waits on the frames.
    /// </para>
    /// <para>
    /// A capture of the first frames of a run is a picture of a window that has been cleared and
    /// not yet drawn into. A material's render pipeline is compiled the first time something asks
    /// to be drawn with it, and until it is ready the renderer skips the mesh and clears the frame
    /// anyway, so an early capture shows the clear color with nothing in it. That is indistinguishable
    /// from a mesh that never arrived, so give a fresh run a hundred frames before believing an
    /// empty picture.
    /// </para>
    /// </remarks>
    /// <param name="target">
    /// The image to capture, from <see cref="CreateTarget"/>, or
    /// <see cref="AssetHandle.None"/> for whatever this run is drawing into.
    /// </param>
    /// <returns>A ticket naming the capture.</returns>
    /// <exception cref="BevyNativeException">
    /// This build has no renderer, or the handle names no image.
    /// </exception>
    public static Capture BeginCapture(AssetHandle target)
    {
        var id = Native.bcs_render_capture(target.Key);
        if (id == NativeStatus.Unsupported) throw NoRenderer("Capturing a picture");

        Native.Check(id, $"asking for a capture of {target}");
        return new Capture(id);
    }

    /// <summary>Asks for a picture of whatever this run is drawing into.</summary>
    public static Capture BeginCapture() => BeginCapture(AssetHandle.None);

    /// <summary>
    /// Reads a capture once it has arrived, and forgets it.
    /// </summary>
    /// <remarks>
    /// False means the picture is still on its way, which is the ordinary answer for the first
    /// frame or two. True hands it over and drops the engine's copy, because it is a megabyte or
    /// two and nothing on that side knows when the caller would be done with it.
    /// </remarks>
    /// <param name="capture">The ticket from <see cref="BeginCapture(AssetHandle)"/>.</param>
    /// <param name="picture">The pixels, when this returns true.</param>
    /// <returns>Whether the picture had arrived.</returns>
    /// <exception cref="BevyNativeException">The ticket names no capture.</exception>
    public static bool TryReadCapture(Capture capture, out CapturedImage? picture)
    {
        picture = null;

        uint width;
        uint height;

        var needed = Native.bcs_render_capture_read(capture.Id, &width, &height, null, 0);

        // Still coming back off the GPU. Asking again next frame is the whole of the protocol.
        if (needed == NativeStatus.InvalidState) return false;
        if (needed == NativeStatus.Unsupported) throw NoRenderer("Reading a capture");

        Native.Check(needed, $"asking how large {capture} is");

        var pixels = new byte[needed];

        fixed (byte* buffer = pixels)
        {
            Native.Check(
                Native.bcs_render_capture_read(capture.Id, &width, &height, buffer, needed),
                $"reading {capture}");
        }

        picture = new CapturedImage(width, height, pixels);
        return true;
    }

    /// <summary>
    /// Reads a capture once it has arrived, in the format it was drawn in, and forgets it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="TryReadCapture(Capture, out CapturedImage?)"/> reads every picture as eight-bit
    /// sRGB, which is how a person would see it, so a half-float target's light brighter than white
    /// reads as white. This hands the pixels over as they are, a half-float target as half floats,
    /// for a program measuring light rather than looking at it, such as an exposure meter or a test
    /// of a bloom's threshold.
    /// </para>
    /// <para>
    /// The picture's format is one a shader image is made in (<see cref="ShaderImageFormat"/>). An
    /// eight-bit picture is <see cref="ShaderImageFormat.Rgba8"/>, red, green, blue then alpha,
    /// whichever order the GPU held it in, and its bytes are the encoded colors. A capture is read
    /// once, either way.
    /// </para>
    /// </remarks>
    /// <param name="capture">The ticket from <see cref="BeginCapture(AssetHandle)"/>.</param>
    /// <param name="picture">The pixels, when this returns true.</param>
    /// <returns>Whether the picture had arrived.</returns>
    /// <exception cref="BevyNativeException">
    /// The ticket names no capture, or the picture is in a format no shader image is made in, which
    /// leaves it for the eight-bit read.
    /// </exception>
    public static bool TryReadCaptureAsItIs(Capture capture, out CapturedTexels? picture)
    {
        picture = null;

        uint width;
        uint height;
        int format;

        var needed = Native.bcs_render_capture_read_raw(capture.Id, &width, &height, &format, null, 0);

        if (needed == NativeStatus.InvalidState) return false;
        if (needed == NativeStatus.Unsupported) throw NoRenderer("Reading a capture");

        Native.Check(needed, $"asking how large {capture} is as it was drawn");

        var bytes = new byte[needed];

        fixed (byte* buffer = bytes)
        {
            Native.Check(
                Native.bcs_render_capture_read_raw(capture.Id, &width, &height, &format, buffer, needed),
                $"reading {capture} as it was drawn");
        }

        picture = new CapturedTexels(width, height, (ShaderImageFormat)format, bytes);
        return true;
    }

    /// <summary>Forgets a capture that will not be read.</summary>
    /// <remarks>
    /// For a caller that stopped waiting. A capture that has arrived holds its pixels until
    /// something drops them, and one that never arrives holds nothing.
    /// </remarks>
    public static void ReleaseCapture(Capture capture) =>
        Native.Check(Native.bcs_render_capture_release(capture.Id), $"releasing {capture}");

    /// <summary>
    /// Creates an empty image a camera can draw into.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What a portal, a security monitor, a minimap or a second viewport is built from. Point a
    /// camera at it with <see cref="SetCameraTarget(Entity, AssetHandle)"/>, and give the same handle to a material as
    /// its <see cref="MaterialSettings.BaseColorTexture"/>, and the surface carrying that material
    /// shows what the camera sees.
    /// </para>
    /// <para>
    /// The image is empty until something draws into it, and nothing loads, so the handle is
    /// usable on the frame it is returned. It is sized in pixels rather than in logical units,
    /// because nothing about it is scaled by a desktop.
    /// </para>
    /// </remarks>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="format">
    /// Eight-bit sRGB unless set. <see cref="TargetFormat.Rgba16Float"/> keeps light brighter than
    /// white as it was drawn, for a reflection or a picture a shader reads on, which a camera with
    /// <see cref="PostSettings.Hdr"/> and no tonemapping draws into as it is. A capture of it reads
    /// as eight-bit sRGB clamped at white, while a shader sampling it reads the full range.
    /// </param>
    /// <param name="layers">
    /// One unless set. More makes an image cameras draw into a layer at a time with
    /// <see cref="SetCameraTarget(Entity, AssetHandle, int)"/>. Six layers of a square image are
    /// read as a cube, which a probe or a material's cube slot takes, and any other count as an
    /// array.
    /// </param>
    /// <returns>A handle to the image.</returns>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static AssetHandle CreateTarget(
        uint width,
        uint height,
        TargetFormat format = TargetFormat.Rgba8,
        uint layers = 1)
    {
        var key = Native.bcs_render_create_target(width, height, (int)format, layers);
        if (key == NativeStatus.Unsupported) throw NoRenderer("Creating a render target");

        Native.Check(key, $"creating a {width}x{height} render target");
        return new AssetHandle(key);
    }

    /// <summary>
    /// Gives an image how it repeats past its edges and how it is filtered.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For an image made in code, which no loader's settings reached, as a small pattern meant to
    /// repeat across a large floor is, and for one already loaded. An image still loading is not
    /// there to change, and one meant to be read a particular way from the start is loaded with
    /// <see cref="AssetServer.LoadImage"/> instead.
    /// </para>
    /// <para>
    /// <see cref="TextureSettings.Srgb"/> is ignored here, since how an image's bytes are read is
    /// decided when it is made.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var checker = Render.CreateImage(pixels, 8, 8);
    /// Render.SetSampler(checker, new TextureSettings { Wrap = TextureWrap.Repeat });
    /// </code>
    /// </example>
    /// <exception cref="BevyNativeException">
    /// The handle names no image, the image is still loading, or this build has no renderer.
    /// </exception>
    public static void SetSampler(AssetHandle image, TextureSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeImageConfig
        {
            AddressU = (int)settings.Wrap,
            AddressV = (int)settings.Wrap,
            MagFilter = (int)settings.MagFilter,
            MinFilter = (int)settings.MinFilter,
            MipmapFilter = (int)settings.MipmapFilter,
            Anisotropy = settings.Anisotropy,
            Srgb = settings.Srgb ? 1 : 0,
        };

        var status = Native.bcs_render_set_sampler(image.Key, &native);
        if (status == NativeStatus.Unsupported) throw NoRenderer("Setting an image's sampler");
        Native.Check(status, $"setting the sampler of {image}");
    }

    /// <summary>
    /// Makes an image out of pixels held here, and hands back a handle to it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The other end of <see cref="TryReadCapture"/>. Reading gives back what was drawn; this takes
    /// a picture that was never in a file, for a texture worked out at startup, a mask built from a
    /// heightmap, or a capture handed on to a material.
    /// </para>
    /// <para>
    /// Nothing loads, so the handle is usable on the frame it is returned, and the pixels are
    /// copied rather than kept, so the array is the caller's again afterwards.
    /// </para>
    /// </remarks>
    /// <param name="pixels">
    /// <paramref name="width"/> by <paramref name="height"/> pixels of RGBA, a row at a time from
    /// the top, which is the layout <see cref="CapturedImage.Pixels"/> comes back in.
    /// </param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="srgb">
    /// Whether the numbers are a color somebody chose, as a picture usually is. False reads them as
    /// they are, for a picture whose numbers mean something else, such as a normal map or a
    /// roughness mask.
    /// </param>
    /// <returns>A handle to the image.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="pixels"/> is not four bytes per pixel of the size given.
    /// </exception>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static AssetHandle CreateImage(
        ReadOnlySpan<byte> pixels,
        uint width,
        uint height,
        bool srgb = true)
    {
        var wanted = (long)width * height * 4;

        if (width == 0 || height == 0)
            throw new ArgumentException("An image needs a width and a height.", nameof(width));

        if (pixels.Length != wanted)
        {
            throw new ArgumentException(
                $"A {width}x{height} image is {wanted} bytes of RGBA, and {pixels.Length} were "
                + "given.",
                nameof(pixels));
        }

        int key;

        fixed (byte* at = pixels)
        {
            key = Native.bcs_render_create_image(at, width, height, srgb ? 1 : 0);
        }

        if (key < 0) throw NoRenderer($"Creating a {width}x{height} image");

        return new AssetHandle(key);
    }

    /// <summary>
    /// Has an image of six square faces treated as a cubemap.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For a shader's <c>TextureCube</c>, and what <see cref="SetSkybox"/> takes. The faces are a
    /// column, top to bottom in the order +X, -X, +Y, -Y, +Z, -Z, or the same six in a row, or a
    /// cross: four wide and three tall with +Y over +Z, -Y under it and -X, +Z, +X, -Z across the
    /// middle, or three wide and four tall with -Z under -Y, drawn half a turn round. The shape says
    /// which, and a cross or a row is laid out as a column when its pixels arrive. A compressed
    /// image is taken as a column only.
    /// </para>
    /// <para>
    /// Applied when the pixels arrive, since the shape of a picture is not known until it has been
    /// decoded, so the handle can be passed on at once. An image of any other shape is left as it
    /// is, with a warning in the log.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">The handle names no image, or there is no renderer.</exception>
    public static void MakeCubemap(AssetHandle image) =>
        Native.Check(Native.bcs_render_make_cubemap(image.Key), "making an image a cubemap");

    /// <summary>
    /// Makes a cubemap out of six images, one a face, as a cubemap shipped as six files is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The handle comes back at once and can be given to <see cref="SetSkybox"/> or a material
    /// straight away. It holds a black pixel until all six images have loaded, then their pixels
    /// as a column, then a cube. The faces have to be square, one size and one format, or the
    /// cubemap is dropped with a warning in the log.
    /// </para>
    /// </remarks>
    /// <param name="positiveX">The face +X looks at.</param>
    /// <param name="negativeX">The face -X looks at.</param>
    /// <param name="positiveY">The face up looks at.</param>
    /// <param name="negativeY">The face down looks at.</param>
    /// <param name="positiveZ">The face +Z looks at.</param>
    /// <param name="negativeZ">The face -Z looks at.</param>
    /// <returns>The cubemap.</returns>
    /// <exception cref="BevyNativeException">A handle names no image, or there is no renderer.</exception>
    public static AssetHandle CubemapFromFaces(
        AssetHandle positiveX,
        AssetHandle negativeX,
        AssetHandle positiveY,
        AssetHandle negativeY,
        AssetHandle positiveZ,
        AssetHandle negativeZ)
    {
        var faces = stackalloc int[6] { positiveX.Key, negativeX.Key, positiveY.Key, negativeY.Key, positiveZ.Key, negativeZ.Key };
        var key = Native.Check(Native.bcs_render_cubemap_from_faces(faces), "making a cubemap of six images");
        return new AssetHandle(key);
    }

    /// <summary>
    /// Has an image of <paramref name="layers"/> equal pictures stacked from top to bottom treated
    /// as an array of them.
    /// </summary>
    /// <remarks>
    /// For a shader's <c>Texture2DArray</c>, which holds many pictures of one size behind one
    /// binding, such as a terrain's ground types or a sprite's frames. Applied when the pixels
    /// arrive, like <see cref="MakeCubemap"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="layers"/> is less than one.</exception>
    /// <exception cref="BevyNativeException">The handle names no image, or there is no renderer.</exception>
    public static void MakeTextureArray(AssetHandle image, int layers)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(layers, 1);
        Native.Check(
            Native.bcs_render_reshape_image(image.Key, layers, 0),
            $"cutting an image into {layers} layers");
    }

    /// <summary>
    /// Has an image of <paramref name="slices"/> equal pictures stacked from top to bottom treated
    /// as a 3D texture that many deep.
    /// </summary>
    /// <remarks>
    /// For a shader's <c>Texture3D</c>, such as fog, clouds, a color grading table or anything else
    /// sampled at a point in space. The first slice is the front. Applied when the pixels arrive,
    /// like <see cref="MakeCubemap"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="slices"/> is less than one.</exception>
    /// <exception cref="BevyNativeException">The handle names no image, or there is no renderer.</exception>
    public static void MakeVolume(AssetHandle image, int slices)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(slices, 1);
        Native.Check(
            Native.bcs_render_reshape_image(image.Key, slices, 1),
            $"cutting an image into {slices} slices");
    }

    /// <summary>
    /// Points a camera at an image instead of at the window.
    /// </summary>
    /// <remarks>
    /// <see cref="AssetHandle.None"/> puts it back on the window, which is where a camera starts.
    /// The camera keeps everything else it was given, because its projection, its layers, its
    /// order and its post-processing are about what it draws rather than about where the result
    /// goes.
    /// </remarks>
    /// <param name="camera">The camera, from <see cref="SpawnCamera3d(CameraSettings)"/>.</param>
    /// <param name="target">The image to draw into, or <see cref="AssetHandle.None"/> for the window.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is not a camera, or the handle names no image.
    /// </exception>
    public static void SetCameraTarget(Entity camera, AssetHandle target) => SetCameraTarget(camera, target, -1);

    /// <summary>
    /// Points a camera at one layer of an image with several, such as a face of a cube.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bevy draws a camera into a whole image, so the camera draws into an ordinary image of the
    /// layer's size and format, and that is copied into the layer after the cameras have drawn.
    /// Six cameras at one point, each a quarter turn apart with a field of view of ninety degrees
    /// and a square picture, pointed at the six layers of a cube target, capture a cube map: a
    /// probe of a package's own, a sky, or what a light sees. The layers are in the order a cube
    /// is: plus and minus X, plus and minus Y, plus and minus Z.
    /// </para>
    /// <para>
    /// A negative <paramref name="layer"/> draws into the whole image, as the overload without one
    /// does.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera.</param>
    /// <param name="target">An image from <see cref="CreateTarget"/> with layers.</param>
    /// <param name="layer">Which layer, counted from zero.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is not a camera, the handle names no image, or the image has no such layer.
    /// </exception>
    public static void SetCameraTarget(Entity camera, AssetHandle target, int layer) => Native.Check(
        Native.bcs_render_set_camera_target(camera.Bits, target.Key, layer),
        $"pointing {camera} at {target}");

    /// <summary>
    /// Where a world point lands on a camera's viewport, in logical pixels.
    /// </summary>
    /// <remarks>
    /// The same coordinates the cursor is reported in, so something drawn in the world can be
    /// hit-tested against the pointer. A point behind the camera answers <see langword="false"/>
    /// rather than a number that would be off the screen in the wrong direction.
    /// </remarks>
    public static bool TryProject(Entity camera, Vec3 point, out float x, out float y)
    {
        var screen = stackalloc float[2];

        if (Native.bcs_render_world_to_viewport(camera.Bits, point.X, point.Y, point.Z, screen) < 0)
        {
            x = 0f;
            y = 0f;
            return false;
        }

        x = screen[0];
        y = screen[1];
        return true;
    }

    /// <summary>
    /// The ray through a point on a camera's viewport.
    /// </summary>
    /// <remarks>
    /// The other half of <see cref="TryProject"/>. Where a drag is taking something is a question
    /// about this ray and the axis being dragged along, rather than about pixels.
    /// </remarks>
    public static bool TryRay(Entity camera, float x, float y, out Vec3 origin, out Vec3 direction)
    {
        var ray = stackalloc float[6];

        if (Native.bcs_render_viewport_to_world(camera.Bits, x, y, ray) < 0)
        {
            origin = default;
            direction = default;
            return false;
        }

        origin = new Vec3(ray[0], ray[1], ray[2]);
        direction = new Vec3(ray[3], ray[4], ray[5]);
        return true;
    }

    /// <summary>
    /// The box an entity occupies in the world, or <see langword="false"/> when it has none.
    /// </summary>
    /// <remarks>
    /// Bevy computes bounds for everything it draws, in the mesh's own space; what comes back
    /// here is those bounds put through the entity's global transform, so a rotated object gets
    /// the box around its corners rather than its corners moved. Anything not drawn, a camera or
    /// a bare entity, has no bounds and answers <see langword="false"/>.
    /// </remarks>
    public static bool TryGetBounds(Entity entity, out Vec3 min, out Vec3 max)
    {
        var bounds = stackalloc float[6];

        if (Native.bcs_render_bounds(entity.Bits, bounds) < 0)
        {
            min = default;
            max = default;
            return false;
        }

        min = new Vec3(bounds[0], bounds[1], bounds[2]);
        max = new Vec3(bounds[3], bounds[4], bounds[5]);
        return true;
    }

    /// <summary>Whether the running app asked for wireframes, from its <see cref="Config.Wireframes"/>.</summary>
    internal static bool Wireframes;

    /// <summary>
    /// Draws an entity's mesh as its own edges in an app that asked for wireframes with
    /// <see cref="Config.Wireframes"/>, or stops drawing them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The shape itself rather than a box round it, which an editor outlines a selection with when
    /// the box is not enough. The line pipeline it needs is a desktop one. Where a backend cannot
    /// draw lines, this is accepted and nothing appears.
    /// </para>
    /// <para>
    /// The app has to have asked for wireframes with <see cref="Config.Wireframes"/>, which adds
    /// the plugins that draw them, and turning one on is refused where it did not. Turning one off
    /// is accepted either way, since there is nothing to stop.
    /// </para>
    /// </remarks>
    /// <param name="entity">What to draw, or stop drawing.</param>
    /// <param name="on">Whether to draw it.</param>
    /// <param name="color">Linear RGBA for the lines.</param>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    /// <exception cref="InvalidOperationException">
    /// A wireframe is turned on in an app whose config did not ask for wireframes.
    /// </exception>
    public static void SetWireframe(
        Entity entity, bool on, (float R, float G, float B, float A) color = default)
    {
        if (on && !Wireframes)
        {
            throw new InvalidOperationException(
                "Drawing a mesh as its edges needs the app's Config.Wireframes, which adds Bevy's wireframe plugins.");
        }

        var status = Native.bcs_render_wireframe(
            entity.Bits, on ? 1 : 0, color.R, color.G, color.B, color.A);

        if (status == NativeStatus.Unsupported) throw NoRenderer("Drawing a wireframe");

        // An entity that has gone is not an error to stop drawing, because a selection outlives
        // what it pointed at by a frame, and asking after that is how it is cleaned up.
        if (status == NativeStatus.NoEntity) return;

        Native.Check(status, "drawing a wireframe");
    }

    /// <summary>
    /// Sets how large a shadow map each kind of light gets, in pixels on a side.
    /// </summary>
    /// <remarks>
    /// One size for every directional light and one for every point and spot light, because Bevy
    /// keeps these globally rather than per light. Larger is sharper and costs memory and fill
    /// rate on every shadow-casting light at once. Bevy's defaults are 2048 and 1024. Zero leaves
    /// that kind as it is, so one can be changed without knowing the other.
    /// </remarks>
    /// <param name="directional">Size for directional lights, or 0 to leave it.</param>
    /// <param name="point">Size for point and spot lights, or 0 to leave it.</param>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static void SetShadowMapSize(uint directional = 0, uint point = 0)
    {
        var status = Native.bcs_render_set_shadow_maps(directional, point);
        if (status == NativeStatus.Unsupported) throw NoRenderer("Setting the shadow map size");

        Native.Check(status, "setting the shadow map size");
    }

    /// <summary>
    /// Puts an entity on a set of render layers, as a bit per layer.
    /// </summary>
    /// <remarks>
    /// A camera draws an entity only where their layers overlap. Zero takes the entity back to
    /// Bevy's default layer, which every camera sees unless it says otherwise.
    /// </remarks>
    /// <example>
    /// <code>
    /// const uint Minimap = 1u &lt;&lt; 1;
    ///
    /// Render.SetLayers(ctx.Ecs, marker, Minimap);          // only the minimap camera sees it
    /// Render.SetLayers(ctx.Ecs, player, 1u | Minimap);     // both cameras do
    /// </code>
    /// </example>
    /// <exception cref="BevyNativeException">The entity is gone, or this build has no renderer.</exception>
    public static void SetLayers(EcsWorld world, Entity entity, uint layers)
    {
        ArgumentNullException.ThrowIfNull(world);

        var status = Native.bcs_render_set_layers(entity.Bits, layers);
        if (status == NativeStatus.Unsupported) throw NoRenderer("Setting render layers");

        Native.Check(status, $"setting the render layers of {entity}");
    }

    /// <summary>Attaches a handle through one of the components that carry one.</summary>
    internal static void Attach(
        EcsWorld world,
        Entity entity,
        string component,
        AssetHandle handle,
        string described)
    {
        ArgumentNullException.ThrowIfNull(world);

        var status = Native.bcs_ecs_insert_asset(entity.Bits, component, handle.Key);
        if (status == NativeStatus.Unsupported) throw NoRenderer($"Attaching {described}");
        if (status == NativeStatus.NoEntity)
            throw new BevyNativeException(
                NativeStatus.NoEntity,
                $"Cannot attach {described} to {entity}: either the entity is no longer alive, "
                + "or the handle has been released.");
        if (status == NativeStatus.NoComponent)
            throw new BevyNativeException(
                NativeStatus.NoComponent,
                $"That handle does not point at {described}. Check that the handle came from the "
                + "matching Create call.");

        Native.Check(status, $"attaching {described}");
    }

    /// <summary>The error for asking a headless build to do something graphical.</summary>
    internal static BevyNativeException NoRenderer(string attempted) =>
        new(NativeStatus.Unsupported,
            $"{attempted} needs a native build with the renderer compiled in. Rebuild the bridge "
            + "with build/build-native.sh --render, or guard the call with App.HasRenderer.");
}
