namespace Bevy;

/// <summary>What an <see cref="AnimationCurve"/> moves on the entity it is aimed at.</summary>
public enum AnimatedProperty
{
    /// <summary>The transform's translation, as Bevy's <c>animated_field!(Transform::translation)</c>.</summary>
    Translation,

    /// <summary>The transform's rotation.</summary>
    Rotation,

    /// <summary>The transform's scale.</summary>
    Scale,

    /// <summary>An interface node's scale, its <c>UiTransform</c>'s, about its center.</summary>
    UiScale,

    /// <summary>An interface node's rotation, its <c>UiTransform</c>'s, in radians about its center.</summary>
    UiRotation,

    /// <summary>A text's color, as Bevy's animated_ui example animates it.</summary>
    TextColor,
}
