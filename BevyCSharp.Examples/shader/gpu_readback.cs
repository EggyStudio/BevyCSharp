// Bevy's gpu_readback example, examples/shader/gpu_readback.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Shading;

// Reads back what a compute shader wrote, a buffer of sixteen numbers it adds one to each frame
// and an image it copies them into, printing the whole buffer, eight numbers of it from the fifth,
// and the image, as each copy arrives, and the buffer once more each time Space is pressed.
//
// Bevy's readback reads the part of the buffer it is asked for. Here the whole buffer is read and
// the part taken from it, and a read is asked for again as each one arrives, where Bevy's reads
// every frame it is kept.
internal static class GpuReadback
{
    private const int BufferLength = 16;

    private static AssetHandle _buffer, _image;
    private static ShaderInstance _compute;
    private static BufferRead? _whole, _range, _texels, _once;

    public static void Build(App app)
    {
        app.Startup(_ =>
        {
            Render.SetClearColor((0f, 0f, 0f, 1f));
            _buffer = Shaders.CreateBuffer<uint>(Enumerable.Range(0, BufferLength).Select(i => (uint)i).ToArray());
            _image = Shaders.CreateImage(BufferLength, 1, ShaderImageFormat.R32UInt);
            _compute = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Compute = "shaders/gpu_readback.slang" }))
                .SetBuffer("data", _buffer)
                .SetTexture("texture", _image);
            (_whole, _range, _texels, _once) = (null, null, null, null);
        }, "gpu_readback.Setup");

        app.Update(ctx =>
        {
            if (_compute.Program.State != ShaderProgramState.Ready) return;
            Shaders.Dispatch(_compute, BufferLength);

            _whole ??= Shaders.BeginBufferRead(_buffer);
            _range ??= Shaders.BeginBufferRead(_buffer);
            _texels ??= Shaders.BeginImageRead(_image);

            if (Shaders.TryReadBuffer<uint>(_whole.Value, out var whole))
            {
                Console.WriteLine($"Buffer [{string.Join(", ", whole!)}]");
                _whole = null;
            }

            if (Shaders.TryReadBuffer<uint>(_range.Value, out var range))
            {
                Console.WriteLine($"Buffer range [{string.Join(", ", range!.Skip(4).Take(8))}]");
                _range = null;
            }

            if (Shaders.TryReadBuffer<uint>(_texels.Value, out var texels))
            {
                Console.WriteLine($"Image [{string.Join(", ", texels!)}]");
                _texels = null;
            }

            // Bevy's trigger_once_readback, Space asking for the buffer a single time.
            if (ctx.Input.KeyPressed(Key.Space)) _once ??= Shaders.BeginBufferRead(_buffer);
            if (_once is { } once && Shaders.TryReadBuffer<uint>(once, out var onceData))
            {
                Console.WriteLine($"Buffer (once) [{string.Join(", ", onceData!)}]");
                _once = null;
            }
        }, "gpu_readback.ComputeAndRead");
    }
}
