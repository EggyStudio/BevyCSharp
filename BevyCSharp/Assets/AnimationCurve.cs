namespace Bevy;

/// <summary>How one property of an entity moves over a clip, for <see cref="Animation.AddCurve"/>.</summary>
/// <remarks>
/// <para>
/// Bevy's <c>AnimatableCurve</c>, a property and the curve it follows. A curve is either values
/// sampled at times, the property interpolated between them, a rotation along the shorter arc, or
/// two values eased between over a time, by one of Bevy's easing functions:
/// </para>
/// <code>
/// AnimationCurve.Translation([0f, 1f, 2f], [new Vec3(1f, 0f, 1f), new Vec3(-1f, 0f, 1f), new Vec3(1f, 0f, 1f)]);
/// AnimationCurve.Rotation(Quat.Identity, Quat.FromRotationY(MathF.PI), EaseFunction.ElasticInOut, 4f);
/// AnimationCurve.Translation(from, to, EaseFunction.CubicInOut, 3f).PingPong();
/// </code>
/// <para>
/// A clip lasts as long as its longest curve, and a curve holds its last value after it ends.
/// </para>
/// </remarks>
public sealed class AnimationCurve
{
    private AnimationCurve(AnimatedProperty property, float[]? times, float[] values, int count, EaseFunction? ease, float duration, bool pingPong, FieldCurve? field = null)
    {
        Property = property;
        Times = times;
        Values = values;
        Count = count;
        Ease = ease;
        Duration = duration;
        IsPingPong = pingPong;
        Moved = field;
    }

    /// <summary>What it moves.</summary>
    public AnimatedProperty Property { get; }

    /// <summary>The component a <see cref="AnimatedProperty.Field"/> curve moves a field of, by its type path, or null.</summary>
    public string? Component => Moved?.Component;

    /// <summary>The reflect path to the field a <see cref="AnimatedProperty.Field"/> curve moves, or null.</summary>
    public string? FieldPath => Moved?.Path;

    /// <summary>The field a <see cref="AnimatedProperty.Field"/> curve moves, with what its values are.</summary>
    internal FieldCurve? Moved { get; }

    /// <summary>When each value is reached, for sampled values, or null for eased ones.</summary>
    internal float[]? Times { get; }

    /// <summary>The values, each as many floats as the property takes.</summary>
    internal float[] Values { get; }

    /// <summary>How many values there are.</summary>
    internal int Count { get; }

    /// <summary>The easing between the two values, or null for sampled ones.</summary>
    public EaseFunction? Ease { get; }

    /// <summary>How long an eased curve takes, in seconds.</summary>
    public float Duration { get; }

    /// <summary>Whether an eased curve goes back to its start after reaching its end, taking twice its duration.</summary>
    public bool IsPingPong { get; }

    /// <summary>The same eased curve going back to its start after reaching its end, as Bevy's <c>ping_pong</c>.</summary>
    /// <exception cref="InvalidOperationException">The curve is sampled rather than eased.</exception>
    public AnimationCurve PingPong() => Ease is null
        ? throw new InvalidOperationException("Only an eased curve goes back and forth. A sampled one says each value it reaches.")
        : new AnimationCurve(Property, null, Values, Count, Ease, Duration, true, Moved);

    /// <summary>A translation through values at times.</summary>
    /// <param name="times">When each value is reached, in seconds, rising.</param>
    /// <param name="values">Where the entity is at each.</param>
    public static AnimationCurve Translation(ReadOnlySpan<float> times, ReadOnlySpan<Vec3> values) => Sampled(AnimatedProperty.Translation, times, values.Length, Flatten(values));

    /// <summary>A translation eased from one place to another.</summary>
    /// <param name="from">Where it starts.</param>
    /// <param name="to">Where it ends.</param>
    /// <param name="ease">How it eases between them.</param>
    /// <param name="duration">How long it takes, in seconds.</param>
    public static AnimationCurve Translation(Vec3 from, Vec3 to, EaseFunction ease, float duration) => Eased(AnimatedProperty.Translation, Flatten([from, to]), ease, duration);

    /// <summary>A rotation through values at times.</summary>
    /// <param name="times">When each value is reached, in seconds, rising.</param>
    /// <param name="values">How the entity is turned at each.</param>
    public static AnimationCurve Rotation(ReadOnlySpan<float> times, ReadOnlySpan<Quat> values) => Sampled(AnimatedProperty.Rotation, times, values.Length, Flatten(values));

    /// <summary>A rotation eased from one turn to another.</summary>
    /// <param name="from">How it is turned at the start.</param>
    /// <param name="to">How it is turned at the end.</param>
    /// <param name="ease">How it eases between them.</param>
    /// <param name="duration">How long it takes, in seconds.</param>
    public static AnimationCurve Rotation(Quat from, Quat to, EaseFunction ease, float duration) => Eased(AnimatedProperty.Rotation, Flatten([from, to]), ease, duration);

    /// <summary>A scale through values at times.</summary>
    /// <param name="times">When each value is reached, in seconds, rising.</param>
    /// <param name="values">The entity's scale at each.</param>
    public static AnimationCurve Scale(ReadOnlySpan<float> times, ReadOnlySpan<Vec3> values) => Sampled(AnimatedProperty.Scale, times, values.Length, Flatten(values));

    /// <summary>A scale eased from one size to another.</summary>
    /// <param name="from">The scale at the start.</param>
    /// <param name="to">The scale at the end.</param>
    /// <param name="ease">How it eases between them.</param>
    /// <param name="duration">How long it takes, in seconds.</param>
    public static AnimationCurve Scale(Vec3 from, Vec3 to, EaseFunction ease, float duration) => Eased(AnimatedProperty.Scale, Flatten([from, to]), ease, duration);

    /// <summary>An interface node's scale through values at times.</summary>
    /// <param name="times">When each value is reached, in seconds, rising.</param>
    /// <param name="values">The node's scale across and down at each.</param>
    public static AnimationCurve UiScale(ReadOnlySpan<float> times, ReadOnlySpan<Vec2> values)
    {
        var flat = new float[values.Length * 2];
        for (var i = 0; i < values.Length; i++) (flat[i * 2], flat[i * 2 + 1]) = (values[i].X, values[i].Y);
        return Sampled(AnimatedProperty.UiScale, times, values.Length, flat);
    }

    /// <summary>An interface node's rotation through values at times.</summary>
    /// <param name="times">When each value is reached, in seconds, rising.</param>
    /// <param name="radians">The node's rotation at each, in radians.</param>
    public static AnimationCurve UiRotation(ReadOnlySpan<float> times, ReadOnlySpan<float> radians) => Sampled(AnimatedProperty.UiRotation, times, radians.Length, radians.ToArray());

    /// <summary>A text's color through values at times, interpolated in sRGB as Bevy's example does.</summary>
    /// <param name="times">When each value is reached, in seconds, rising.</param>
    /// <param name="values">The text's color at each.</param>
    public static AnimationCurve TextColor(ReadOnlySpan<float> times, ReadOnlySpan<Color> values)
    {
        var flat = new float[values.Length * 4];
        for (var i = 0; i < values.Length; i++)
        {
            var srgb = values[i].ToSrgb();
            (flat[i * 4], flat[i * 4 + 1], flat[i * 4 + 2], flat[i * 4 + 3]) = (srgb.X, srgb.Y, srgb.Z, srgb.W);
        }
        return Sampled(AnimatedProperty.TextColor, times, values.Length, flat);
    }

    /// <summary>A number in a field of a reflected component through values at times.</summary>
    /// <param name="component">The component's type path, as <c>bevy_light::point_light::PointLight</c>.</param>
    /// <param name="path">Bevy's reflect path to the field from the component's root, as <c>intensity</c>.</param>
    /// <param name="times">When each value is reached, in seconds, rising.</param>
    /// <param name="values">The field's value at each.</param>
    /// <remarks>
    /// <para>
    /// For a field no other curve names, a light's intensity, a camera's field of view, a
    /// material's value on a component of the game's own that Bevy reflects, as Bevy's
    /// <c>animated_field!</c> reaches a field it is compiled against. The component and the path
    /// are checked when the curve is added, against a default of the component where Bevy can make
    /// one, so a path that leads nowhere or to a field of another type is refused then rather than
    /// ignored as the clip plays.
    /// </para>
    /// <para>
    /// A color's channels are reached through the variant it holds, as <c>color.0.red</c> while a
    /// <c>bevy_color::color::Color</c> holds sRGB, each a number of its own, and a field inside a
    /// variant moves only while the component holds that variant.
    /// </para>
    /// </remarks>
    public static AnimationCurve Field(string component, string path, ReadOnlySpan<float> times, ReadOnlySpan<float> values) =>
        Sampled(AnimatedProperty.Field, times, values.Length, values.ToArray(), new FieldCurve(component, path, FieldValue.Number));

    /// <summary>A number in a field of a reflected component eased from one value to another.</summary>
    /// <param name="component">The component's type path, as <c>bevy_light::point_light::PointLight</c>.</param>
    /// <param name="path">Bevy's reflect path to the field from the component's root, as <c>intensity</c>.</param>
    /// <param name="from">The value at the start.</param>
    /// <param name="to">The value at the end.</param>
    /// <param name="ease">How it eases between them.</param>
    /// <param name="duration">How long it takes, in seconds.</param>
    public static AnimationCurve Field(string component, string path, float from, float to, EaseFunction ease, float duration) =>
        Eased(AnimatedProperty.Field, [from, to], ease, duration, new FieldCurve(component, path, FieldValue.Number));

    /// <summary>A two-component vector in a field of a reflected component through values at times.</summary>
    /// <inheritdoc cref="Field(string, string, ReadOnlySpan{float}, ReadOnlySpan{float})" path="/param"/>
    public static AnimationCurve Field(string component, string path, ReadOnlySpan<float> times, ReadOnlySpan<Vec2> values)
    {
        var flat = new float[values.Length * 2];
        for (var i = 0; i < values.Length; i++) (flat[i * 2], flat[i * 2 + 1]) = (values[i].X, values[i].Y);
        return Sampled(AnimatedProperty.Field, times, values.Length, flat, new FieldCurve(component, path, FieldValue.Vec2));
    }

    /// <summary>A three-component vector in a field of a reflected component through values at times.</summary>
    /// <inheritdoc cref="Field(string, string, ReadOnlySpan{float}, ReadOnlySpan{float})" path="/param"/>
    public static AnimationCurve Field(string component, string path, ReadOnlySpan<float> times, ReadOnlySpan<Vec3> values) =>
        Sampled(AnimatedProperty.Field, times, values.Length, Flatten(values), new FieldCurve(component, path, FieldValue.Vec3));

    /// <summary>A three-component vector in a field of a reflected component eased from one value to another.</summary>
    /// <inheritdoc cref="Field(string, string, float, float, EaseFunction, float)" path="/param"/>
    public static AnimationCurve Field(string component, string path, Vec3 from, Vec3 to, EaseFunction ease, float duration) =>
        Eased(AnimatedProperty.Field, Flatten([from, to]), ease, duration, new FieldCurve(component, path, FieldValue.Vec3));

    /// <summary>A four-component vector in a field of a reflected component through values at times.</summary>
    /// <inheritdoc cref="Field(string, string, ReadOnlySpan{float}, ReadOnlySpan{float})" path="/param"/>
    public static AnimationCurve Field(string component, string path, ReadOnlySpan<float> times, ReadOnlySpan<Vec4> values)
    {
        var flat = new float[values.Length * 4];
        for (var i = 0; i < values.Length; i++) (flat[i * 4], flat[i * 4 + 1], flat[i * 4 + 2], flat[i * 4 + 3]) = (values[i].X, values[i].Y, values[i].Z, values[i].W);
        return Sampled(AnimatedProperty.Field, times, values.Length, flat, new FieldCurve(component, path, FieldValue.Vec4));
    }

    /// <summary>A rotation in a field of a reflected component through values at times, along the shorter arc.</summary>
    /// <inheritdoc cref="Field(string, string, ReadOnlySpan{float}, ReadOnlySpan{float})" path="/param"/>
    public static AnimationCurve Field(string component, string path, ReadOnlySpan<float> times, ReadOnlySpan<Quat> values) =>
        Sampled(AnimatedProperty.Field, times, values.Length, Flatten(values), new FieldCurve(component, path, FieldValue.Rotation));

    /// <summary>A rotation in a field of a reflected component eased from one turn to another.</summary>
    /// <inheritdoc cref="Field(string, string, float, float, EaseFunction, float)" path="/param"/>
    public static AnimationCurve Field(string component, string path, Quat from, Quat to, EaseFunction ease, float duration) =>
        Eased(AnimatedProperty.Field, Flatten([from, to]), ease, duration, new FieldCurve(component, path, FieldValue.Rotation));

    private static AnimationCurve Sampled(AnimatedProperty property, ReadOnlySpan<float> times, int count, float[] values, FieldCurve? field = null)
    {
        if (count == 0) throw new ArgumentException("A curve has at least one value.", nameof(times));
        if (times.Length != count) throw new ArgumentException($"A curve has a time for each value, and {times.Length} times were given for {count} values.", nameof(times));
        for (var i = 1; i < times.Length; i++)
            if (!(times[i] > times[i - 1])) throw new ArgumentException($"A curve's times rise, and {times[i]} follows {times[i - 1]}.", nameof(times));

        return new AnimationCurve(property, times.ToArray(), values, count, null, 0f, false, field);
    }

    private static AnimationCurve Eased(AnimatedProperty property, float[] values, EaseFunction ease, float duration, FieldCurve? field = null) =>
        duration > 0f
            ? new AnimationCurve(property, null, values, 2, ease, duration, false, field)
            : throw new ArgumentOutOfRangeException(nameof(duration), duration, "An eased curve takes some time.");

    private static float[] Flatten(ReadOnlySpan<Vec3> values)
    {
        var flat = new float[values.Length * 3];
        for (var i = 0; i < values.Length; i++) (flat[i * 3], flat[i * 3 + 1], flat[i * 3 + 2]) = (values[i].X, values[i].Y, values[i].Z);
        return flat;
    }

    private static float[] Flatten(ReadOnlySpan<Quat> values)
    {
        var flat = new float[values.Length * 4];
        for (var i = 0; i < values.Length; i++) (flat[i * 4], flat[i * 4 + 1], flat[i * 4 + 2], flat[i * 4 + 3]) = (values[i].X, values[i].Y, values[i].Z, values[i].W);
        return flat;
    }
}
