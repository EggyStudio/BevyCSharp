// Bevy's 2d_screen_shake example, examples/camera/2d_screen_shake.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Cameras;

// Shakes a 2D camera by trauma, after Squirrel Eiserloh's talk on juicing a game: Space adds
// trauma, which decays over time, and the camera is moved and turned by Perlin noise scaled by
// the trauma squared, so a little shakes little and a lot shakes hard.
internal static class ScreenShake2d
{
    private const float TraumaDecayPerSecond = 0.5f;
    private const float TraumaExponent = 2f;
    private const float MaxAngle = 10f * MathF.PI / 180f;
    private const float MaxTranslation = 20f;
    private const float NoiseSpeed = 20f;
    private const float TraumaPerPress = 0.4f;

    private static Entity _camera;
    private static float _trauma;

    public static void Build(App app)
    {
        app.Startup(Setup, "2d_screen_shake.Setup");
        app.Update(IncreaseTrauma, "2d_screen_shake.IncreaseTrauma");
        app.Update(ShakeCamera, "2d_screen_shake.ShakeCamera");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _trauma = 0f;
        _camera = Render2d.SpawnCamera2d();

        foreach (var (width, height, color, x, y, z) in new[]
        {
            (1000f, 700f, Color.FromSrgb(0.2f, 0.2f, 0.3f), 0f, 0f, 0f),
            (50f, 100f, Color.FromSrgb(0.25f, 0.94f, 0.91f), 0f, 0f, 2f),
            (50f, 50f, Color.FromSrgb(0.85f, 0f, 0.2f), -450f, 200f, 2f),
            (70f, 50f, Color.FromSrgb(0.5f, 0.8f, 0.2f), 450f, -150f, 2f),
        })
        {
            var entity = ecs.Spawn();
            ecs.Add(entity, Transform.At(x, y, z));
            Render2d.SetMesh(ecs, entity, Render.CreateMesh(MeshShape.Rectangle, width, height));
            Render2d.SetMaterial(ecs, entity, Render2d.CreateMaterial(new ColorMaterialSettings { Color = color }));
        }

        Ui.SpawnText("Press space repeatedly to trigger a progressively stronger screen shake",
            new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });
    }

    private static void IncreaseTrauma(BehaviorContext ctx)
    {
        if (ctx.Input.KeyPressed(Key.Space)) _trauma = Math.Clamp(_trauma + TraumaPerPress, 0f, 1f);
    }

    // Bevy keeps the camera's own transform aside and puts it back before anything else runs, so
    // the shake never moves the camera for good. Nothing else moves this camera, so the shake is
    // laid over where it stands, the identity, each frame instead.
    private static void ShakeCamera(BehaviorContext ctx)
    {
        var t = ctx.Time.Elapsed * NoiseSpeed;
        var shake = MathF.Pow(_trauma, TraumaExponent);
        var roll = PerlinNoise(t) * shake * MaxAngle;
        var x = PerlinNoise(t + 100f) * shake * MaxTranslation;
        var y = PerlinNoise(t + 200f) * shake * MaxTranslation;
        ctx.Ecs.Set(_camera, new Transform(new Vec3(x, y, 0f), Quat.FromRotationZ(roll), Vec3.One));

        _trauma = Math.Clamp(_trauma - TraumaDecayPerSecond * ctx.Time.Delta, 0f, 1f);
    }

    // One-dimensional Perlin noise as Bevy's example works it out, over Ken Perlin's table.
    private static float PerlinNoise(float x)
    {
        var floor = (int)MathF.Floor(x);
        var (xf0, xf1) = (x - floor, x - floor - 1f);
        var t = Math.Clamp(xf0 * xf0 * xf0 * (xf0 * (xf0 * 6f - 15f) + 10f), 0f, 1f);
        float Grad(int index, float at) => (Permutation[index & 0xFF] & 1) != 0 ? at : -at;
        var (a, b) = (Grad(floor, xf0), Grad(floor + 1, xf1));
        return a + (b - a) * t;
    }

    private static readonly byte[] Permutation =
    [
        0x97, 0xA0, 0x89, 0x5B, 0x5A, 0x0F, 0x83, 0x0D, 0xC9, 0x5F, 0x60, 0x35, 0xC2, 0xE9, 0x07,
        0xE1, 0x8C, 0x24, 0x67, 0x1E, 0x45, 0x8E, 0x08, 0x63, 0x25, 0xF0, 0x15, 0x0A, 0x17, 0xBE,
        0x06, 0x94, 0xF7, 0x78, 0xEA, 0x4B, 0x00, 0x1A, 0xC5, 0x3E, 0x5E, 0xFC, 0xDB, 0xCB, 0x75,
        0x23, 0x0B, 0x20, 0x39, 0xB1, 0x21, 0x58, 0xED, 0x95, 0x38, 0x57, 0xAE, 0x14, 0x7D, 0x88,
        0xAB, 0xA8, 0x44, 0xAF, 0x4A, 0xA5, 0x47, 0x86, 0x8B, 0x30, 0x1B, 0xA6, 0x4D, 0x92, 0x9E,
        0xE7, 0x53, 0x6F, 0xE5, 0x7A, 0x3C, 0xD3, 0x85, 0xE6, 0xDC, 0x69, 0x5C, 0x29, 0x37, 0x2E,
        0xF5, 0x28, 0xF4, 0x66, 0x8F, 0x36, 0x41, 0x19, 0x3F, 0xA1, 0x01, 0xD8, 0x50, 0x49, 0xD1,
        0x4C, 0x84, 0xBB, 0xD0, 0x59, 0x12, 0xA9, 0xC8, 0xC4, 0x87, 0x82, 0x74, 0xBC, 0x9F, 0x56,
        0xA4, 0x64, 0x6D, 0xC6, 0xAD, 0xBA, 0x03, 0x40, 0x34, 0xD9, 0xE2, 0xFA, 0x7C, 0x7B, 0x05,
        0xCA, 0x26, 0x93, 0x76, 0x7E, 0xFF, 0x52, 0x55, 0xD4, 0xCF, 0xCE, 0x3B, 0xE3, 0x2F, 0x10,
        0x3A, 0x11, 0xB6, 0xBD, 0x1C, 0x2A, 0xDF, 0xB7, 0xAA, 0xD5, 0x77, 0xF8, 0x98, 0x02, 0x2C,
        0x9A, 0xA3, 0x46, 0xDD, 0x99, 0x65, 0x9B, 0xA7, 0x2B, 0xAC, 0x09, 0x81, 0x16, 0x27, 0xFD,
        0x13, 0x62, 0x6C, 0x6E, 0x4F, 0x71, 0xE0, 0xE8, 0xB2, 0xB9, 0x70, 0x68, 0xDA, 0xF6, 0x61,
        0xE4, 0xFB, 0x22, 0xF2, 0xC1, 0xEE, 0xD2, 0x90, 0x0C, 0xBF, 0xB3, 0xA2, 0xF1, 0x51, 0x33,
        0x91, 0xEB, 0xF9, 0x0E, 0xEF, 0x6B, 0x31, 0xC0, 0xD6, 0x1F, 0xB5, 0xC7, 0x6A, 0x9D, 0xB8,
        0x54, 0xCC, 0xB0, 0x73, 0x79, 0x32, 0x2D, 0x7F, 0x04, 0x96, 0xFE, 0x8A, 0xEC, 0xCD, 0x5D,
        0xDE, 0x72, 0x43, 0x1D, 0x18, 0x48, 0xF3, 0x8D, 0x80, 0xC3, 0x4E, 0x42, 0xD7, 0x3D, 0x9C,
        0xB4,
    ];
}
