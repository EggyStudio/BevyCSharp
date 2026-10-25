using System.Numerics;

namespace Bevy;

/// <summary>The fog's froxels on each camera that asked for them, their images and their dispatches.</summary>
internal static partial class SkyFog
{
    /// <summary>How many boxes the froxels thicken the fog inside at most, as the shader's array holds them.</summary>
    internal const int MostBoxes = 8;

    // A camera's froxels, the two images of scattered light that trade places every frame, the one
    // they are added up into, and the frames they have run, which the history waits a frame for.
    private sealed class Froxels
    {
        internal required (uint X, uint Y, uint Z) Grid { get; init; }
        internal required AssetHandle[] Scatter { get; init; }
        internal required AssetHandle Volume { get; init; }
        internal required ShaderInstance Inject { get; init; }
        internal required ShaderInstance Integrate { get; init; }
        internal required FroxelFogSettings Settings { get; set; }
        internal int Frames { get; set; }
        internal Vec3 Carried { get; set; }
        internal bool Dispatched { get; set; }
    }

    private static readonly Dictionary<ulong, Froxels> Froxeled = [];
    private static AssetHandle _noVolume = AssetHandle.None;
    private static AssetHandle _noShadow = AssetHandle.None;

    private static (uint X, uint Y, uint Z) GridOf(FroxelQuality quality) => quality switch
    {
        FroxelQuality.Low => (80u, 45u, 64u),
        FroxelQuality.High => (160u, 90u, 128u),
        _ => (120u, 68u, 96u),
    };

    /// <summary>
    /// Gives a camera's fog draw its froxels and the camera the dispatches that fill them, or takes
    /// both away where the settings name none, the draw then working the fog out in closed form.
    /// </summary>
    internal static void SetFroxels(Entity camera, ShaderInstance draw, SkyFogSettings settings)
    {
        if (settings.Froxels is not { } asked)
        {
            if (Froxeled.Remove(camera.Bits)) Shaders.SetEngineViewDispatches(camera, Owner);
            draw.Set("froxels", Vector4.Zero).Set("boxes", new Vector4[MostBoxes * 2])
                .SetNumbers("counts", 4, (ReadOnlySpan<uint>)[0u, 0u, 0u, 0u])
                .SetTexture("fog_volume", NoVolume()).SetSampler("fog_volume_sampler", SamplerSettings.Clamped);
            return;
        }

        var grid = GridOf(asked.Quality);
        if (!Froxeled.TryGetValue(camera.Bits, out var froxels) || froxels.Grid != grid)
        {
            AssetHandle Image() => Shaders.CreateImage(grid.X, grid.Y, ShaderImageFormat.Rgba16Float, depth: grid.Z);
            froxels = new Froxels
            {
                Grid = grid,
                Scatter = [Image(), Image()],
                Volume = Image(),
                Inject = Shaders.CreateInstance(SceneField.KernelProgram("fog_froxels.slang")),
                Integrate = Shaders.CreateInstance(SceneField.KernelProgram("fog_integrate.slang")),
                Settings = asked,
            };
            Froxeled[camera.Bits] = froxels;
        }
        froxels.Settings = asked;

        var reach = new Vector4(grid.X, grid.Y, grid.Z, asked.Distance);
        var (r, g, b) = settings.Color ?? (1f, 1f, 1f);
        var boxes = new Vector4[MostBoxes * 2];
        for (var i = 0; i < asked.Boxes.Count; i++)
        {
            var box = asked.Boxes[i];
            boxes[i * 2] = new Vector4(box.Center.X, box.Center.Y, box.Center.Z, 3f / box.Visibility);
            boxes[(i * 2) + 1] = new Vector4(box.Size.X / 2f, box.Size.Y / 2f, box.Size.Z / 2f, Math.Max(box.Edge, 0.01f));
        }

        froxels.Inject.Set("grid", reach)
            .Set("layer", new Vector4(3f / settings.Visibility, settings.Height, settings.Base, 0f))
            .Set("horizon", new Vector4(Math.Clamp(settings.HorizonDegrees, 0f, 89f), 0f, 0f, settings.Color is null ? 0f : 1f))
            .Set("color", new Vector4(r * settings.Brightness, g * settings.Brightness, b * settings.Brightness, 1f))
            .Set("boxes", boxes)
            .SetNumbers("counts", 4, (ReadOnlySpan<uint>)[(uint)asked.Boxes.Count, 0u, 0u, 0u])
            .SetSampler("fog_sampler", SamplerSettings.Clamped);
        froxels.Integrate.Set("grid", reach);
        Turn(camera, froxels, 0f);

        draw.Set("froxels", new Vector4(1f, grid.Z, asked.Distance, 0f))
            .Set("boxes", boxes)
            .SetNumbers("counts", 4, (ReadOnlySpan<uint>)[(uint)asked.Boxes.Count, 0u, 0u, 0u])
            .SetTexture("fog_volume", froxels.Volume)
            .SetSampler("fog_volume_sampler", SamplerSettings.Clamped);

        if (froxels.Dispatched) return;
        Shaders.SetEngineViewDispatches(camera, Owner,
            ViewDispatch.Fixed(froxels.Inject, FramePoint.AfterPrepass, (grid.X + 3) / 4, (grid.Y + 3) / 4, (grid.Z + 3) / 4),
            ViewDispatch.Fixed(froxels.Integrate, FramePoint.AfterPrepass, (grid.X + 7) / 8, (grid.Y + 7) / 8));
        froxels.Dispatched = true;
    }

    /// <summary>
    /// Moves every camera's froxels on a frame, the two images of scattered light trading places,
    /// each froxel's point moving through its slice and the lumps carried on by the wind.
    /// </summary>
    internal static void Tick(World world)
    {
        ForgetOtherApps();
        if (Froxeled.Count == 0) return;
        var delta = world.Resource<Time>().Delta;
        foreach (var (camera, froxels) in Froxeled) Turn(new Entity(camera), froxels, delta);
    }

    private static void Turn(Entity camera, Froxels froxels, float delta)
    {
        froxels.Frames++;
        froxels.Carried += froxels.Settings.Wind * delta;
        var now = froxels.Frames % 2;
        var settings = froxels.Settings;

        // The golden ratio's fraction moves the point through its slice so no two frames in a row
        // land near each other and every depth of the slice is visited evenly.
        var within = (float)((froxels.Frames * 0.6180339887) % 1.0);
        froxels.Inject.SetTexture("fog_scatter", froxels.Scatter[now])
            .SetTexture("fog_scatter_previous", froxels.Scatter[1 - now])
            .Set("lumps", new Vector4(Math.Clamp(settings.Lumps, 0f, 1f), 1f / settings.LumpSize, within, froxels.Frames > 2 ? 1f : 0f))
            .Set("wind", new Vector4(froxels.Carried.X, froxels.Carried.Y, froxels.Carried.Z, Math.Max(0, settings.Lights)));
        froxels.Integrate.SetTexture("fog_scatter", froxels.Scatter[now]).SetTexture("fog_volume", froxels.Volume);

        // The clouds' shadow where the camera's clouds cast one, which darkens the sun's share.
        if (Clouds.ShadowFor(camera) is { } shadow)
        {
            froxels.Inject.SetTexture("cloud_shadow", shadow.Image)
                .Set("cloud_frame", new Vector4(shadow.Middle.X, shadow.Middle.Y, shadow.Middle.Z, shadow.Half))
                .Set("cloud_across", new Vector4(shadow.Across.X, shadow.Across.Y, shadow.Across.Z, 1f))
                .Set("cloud_up", new Vector4(shadow.Up.X, shadow.Up.Y, shadow.Up.Z, 0f));
        }
        else
        {
            froxels.Inject.SetTexture("cloud_shadow", NoShadow()).Set("cloud_across", Vector4.Zero);
        }
        froxels.Inject.SetSampler("cloud_shadow_sampler", SamplerSettings.Linear);
    }

    // The image froxels with no clouds' shadow are bound, which they never read.
    private static AssetHandle NoShadow()
    {
        if (!_noShadow.IsValid) _noShadow = Shaders.CreateImage(1, 1, ShaderImageFormat.Rgba8);
        return _noShadow;
    }

    /// <summary>Whether every camera's froxel programs have compiled, so a frame draws them.</summary>
    internal static bool FroxelsReady() =>
        Froxeled.Values.All(f => f.Inject.Program.State == ShaderProgramState.Ready && f.Integrate.Program.State == ShaderProgramState.Ready);

    // The image a fog draw with no froxels is bound, which it never reads.
    private static AssetHandle NoVolume()
    {
        if (!_noVolume.IsValid) _noVolume = Shaders.CreateImage(1, 1, ShaderImageFormat.Rgba16Float, depth: 1);
        return _noVolume;
    }
}
