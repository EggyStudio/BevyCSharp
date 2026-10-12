namespace Bevy;

/// <summary>The field an <see cref="AnimatedProperty.Field"/> curve moves, and what its values are.</summary>
internal sealed record FieldCurve
{
    public FieldCurve(string component, string path, FieldValue value)
    {
        ArgumentException.ThrowIfNullOrEmpty(component);
        ArgumentException.ThrowIfNullOrEmpty(path);
        (Component, Path, Value) = (component, path, value);
    }

    /// <summary>The component's type path.</summary>
    public string Component { get; }

    /// <summary>Bevy's reflect path to the field from the component's root.</summary>
    public string Path { get; }

    /// <summary>What the field holds, as the bridge numbers it.</summary>
    public FieldValue Value { get; }
}
