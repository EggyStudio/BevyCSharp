// Bevy's cpu_draw example, examples/2d/cpu_draw.rs at v0.19.1, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Draws into an image from the CPU, a pixel a step at 1024 steps a second along a spiral, the
// color changing whenever the pen comes back to a pixel it has already drawn in that color.
internal static class CpuDraw
{
    private const int Size = 256;

    private static readonly byte[] Pixels = new byte[Size * Size * 4];
    private static AssetHandle _image;
    private static Random _random = new(19878367);
    private static (byte R, byte G, byte B) _color;
    private static int _step;

    public static void Configure(Config config) => config.FixedHz = 1024;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            (_random, _step) = (new Random(19878367), 0);
            Render2d.SpawnCamera2d();

            // Beige, fading out from the middle to nothing at the edge. The image holds linear
            // values, so the color is beige as Bevy's sRGB image shows it.
            var beige = Color.FromSrgb(245f / 255f, 245f / 255f, 220f / 255f);
            for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
            {
                var r = MathF.Sqrt((x - Size / 2f) * (x - Size / 2f) + (y - Size / 2f) * (y - Size / 2f));
                var i = (y * Size + x) * 4;
                (Pixels[i], Pixels[i + 1], Pixels[i + 2]) = ((byte)(beige.R * 255f), (byte)(beige.G * 255f), (byte)(beige.B * 255f));
                Pixels[i + 3] = (byte)((1f - Math.Clamp(r / (Size / 2f), 0f, 1f)) * 255f);
            }

            _image = Shaders.CreateImage<byte>(Size, Size, ShaderImageFormat.Rgba8, Pixels);
            var sprite = ctx.Ecs.Spawn();
            ctx.Ecs.Add(sprite, Transform.Identity);
            Render2d.SetSprite(ctx.Ecs, sprite, _image);
        }, "cpu_draw.Setup");

        app.On(Stage.FixedUpdate, _ =>
        {
            if (_step == 0) _color = RandomColor();

            // A point on a spiral whose radius swings in and out as it turns.
            var r = MathF.Sin(_step * 0.12345f) * (Size / 2f);
            var angle = _step * 0.0123f;
            var (x, y) = ((int)(MathF.Cos(angle) * r + Size / 2f), (int)(MathF.Sin(angle) * r + Size / 2f));
            x = Math.Clamp(x, 0, Size - 1);
            y = Math.Clamp(y, 0, Size - 1);

            var i = (y * Size + x) * 4;
            if (Pixels[i] == _color.R && Pixels[i + 1] == _color.G && Pixels[i + 2] == _color.B) _color = RandomColor();
            (Pixels[i], Pixels[i + 1], Pixels[i + 2]) = _color;

            Shaders.WriteImage<byte>(_image, Pixels.AsSpan(i, 4), (uint)x, (uint)y, 1, 1);
            _step++;
        }, "cpu_draw.Draw");
    }

    private static (byte, byte, byte) RandomColor() => ((byte)_random.Next(256), (byte)_random.Next(256), (byte)_random.Next(256));
}
