namespace Bevy;

/// <summary>One state of an <see cref="AnimationStateMachine"/>, a clip and how it plays.</summary>
public sealed class AnimationMachineState
{
    /// <summary>What transitions call it, unique in its machine.</summary>
    public string Name = string.Empty;

    /// <summary>The model's clip it plays, by the name the file gives it.</summary>
    public string Clip = string.Empty;

    /// <summary>Whether the clip plays over and over, as a walk does, rather than once and holding.</summary>
    public bool Repeat = true;

    /// <summary>How fast the clip plays, one being as it was made.</summary>
    public float Speed = 1f;
}
