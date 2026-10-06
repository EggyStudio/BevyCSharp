namespace Bevy;

/// <summary>
/// A distance in a UI layout.
/// </summary>
/// <remarks>
/// A number alone cannot say whether it means pixels, a share of the parent, or "work it out", so
/// the unit travels with it.
/// </remarks>
/// <param name="Value">The magnitude, ignored when the unit is <see cref="LengthUnit.Auto"/>.</param>
/// <param name="Unit">What the magnitude is measured in.</param>
public readonly record struct Length(float Value, LengthUnit Unit)
{
    /// <summary>Left to the layout.</summary>
    public static Length Auto => new(0f, LengthUnit.Auto);

    /// <summary>No distance at all.</summary>
    /// <remarks>
    /// Different from <see cref="Auto"/> where the layout has room to spare, because an automatic
    /// margin takes that room and a zero one does not.
    /// </remarks>
    public static Length Zero => new(0f, LengthUnit.Px);

    /// <summary>A distance in logical pixels.</summary>
    public static Length Px(float value) => new(value, LengthUnit.Px);

    /// <summary>A share of the parent, where 100 is all of it.</summary>
    public static Length Percent(float value) => new(value, LengthUnit.Percent);

    /// <inheritdoc/>
    public override string ToString() => Unit switch
    {
        LengthUnit.Px => $"{Value}px",
        LengthUnit.Percent => $"{Value}%",
        _ => "auto",
    };
}
