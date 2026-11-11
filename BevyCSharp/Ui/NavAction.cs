namespace Bevy;

/// <summary>Where the focus goes within a group, as Bevy's <c>NavAction</c>.</summary>
public enum NavAction
{
    /// <summary>To the next, wrapping round to the first.</summary>
    Next,

    /// <summary>To the previous, wrapping round to the last.</summary>
    Previous,

    /// <summary>To the first.</summary>
    First,

    /// <summary>To the last.</summary>
    Last,
}
