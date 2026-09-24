namespace Bevy;

using Bevy.Interop;

/// <summary>
/// Materials drawn by a shader the game wrote, rather than by the one the engine ships.
/// </summary>
/// <remarks>
/// <para>
/// Bevy decides a material's shader from its Rust type, through a function that is asked of the
/// type rather than of the material, so one type draws with one shader. C# cannot declare a Rust
/// type, which is the same wall the state slots hit, so the bridge declares a fixed set of material
/// slots and an app says what each one is for before it runs.
/// </para>
/// <para>
/// What a slot carries is deliberately plain. Sixteen floats and one picture cover a great deal (a
/// color, a scroll, a threshold, a mask), and anything past that is a bind group layout the caller
/// would have to describe, which is a second language to learn rather than a shader to write.
/// </para>
/// </remarks>
public static unsafe class Shaders
{
    /// <summary>How many floats one material carries.</summary>
    /// <remarks>
    /// They reach the shader as four <c>vec4</c> in the order they were given, because a uniform is
    /// laid out in sixteen-byte rows and a shader reading vectors reads exactly what was written.
    /// </remarks>
    public const int ParameterCount = 16;

    /// <summary>How many shader material slots this bridge provides.</summary>
    public static int SlotCount => Native.bcs_shader_slots();

    /// <summary>
    /// Makes a material drawn by one of the slots.
    /// </summary>
    /// <remarks>
    /// The slot has to have been given a shader by <see cref="App.UseShader"/> before the app ran,
    /// because what draws it is a plugin added then. A material made for a slot nothing was said
    /// about would be a handle nothing draws, so it is refused.
    /// </remarks>
    /// <param name="slot">Which slot, below <see cref="SlotCount"/>.</param>
    /// <param name="parameters">
    /// Up to <see cref="ParameterCount"/> floats. Anything past what is passed is zero.
    /// </param>
    /// <param name="texture">
    /// The picture the shader samples, or <see cref="AssetHandle.None"/> to leave it unbound, which
    /// a shader that does not sample one does not notice.
    /// </param>
    /// <param name="alpha">
    /// What the renderer does where this material is not opaque. A shader writing anything but one
    /// in its alpha channel wants <see cref="AlphaMode.Blend"/> or <see cref="AlphaMode.Add"/>,
    /// since an opaque material's alpha is not read at all.
    /// </param>
    /// <returns>A handle to give <see cref="Render.SetMaterial"/>.</returns>
    /// <exception cref="ArgumentException">Too many parameters were given.</exception>
    /// <exception cref="BevyNativeException">
    /// The slot has no shader, the texture names no image, or this build has no renderer.
    /// </exception>
    /// <example>
    /// <code>
    /// // Once, before the app runs.
    /// app.UseShader(0, "shaders/ripple.wgsl");
    ///
    /// // Then as many materials as the game wants, each with its own numbers.
    /// var water = Shaders.CreateMaterial(0, [0.1f, 0.4f, 0.8f, 1f, speed]);
    /// Render.SetMaterial(ctx.Ecs, pond, water);
    /// </code>
    /// </example>
    public static AssetHandle CreateMaterial(
        int slot,
        ReadOnlySpan<float> parameters = default,
        AssetHandle texture = default,
        AlphaMode alpha = AlphaMode.Opaque)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);

        if (parameters.Length > ParameterCount)
        {
            throw new ArgumentException(
                $"A shader material carries {ParameterCount} floats and {parameters.Length} were "
                + "given. What needs more than that needs a bind group of its own, which the "
                + "bridge does not describe.",
                nameof(parameters));
        }

        int key;

        fixed (float* at = parameters)
        {
            key = Native.bcs_shader_material_create(
                slot,
                at,
                parameters.Length,
                texture.Key,
                (int)alpha);
        }

        if (key < 0)
        {
            Native.Check(key, $"making a material for shader slot {slot}");

            throw new BevyNativeException(
                NativeStatus.InvalidState,
                $"Shader slot {slot} has no shader, so a material made for it would be a handle "
                + "nothing draws. Call app.UseShader before the app runs.");
        }

        return new AssetHandle(key);
    }
}
