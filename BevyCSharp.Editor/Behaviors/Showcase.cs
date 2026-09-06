using Bevy;

namespace BevyCSharp.Editor.Behaviors;

/// <summary>
/// A component that asks for one of everything, so the inspector can be seen doing it.
/// </summary>
/// <remarks>
/// <para>
/// Nothing uses it and nothing spawns with it: it is added by hand from the inspector's own list,
/// and it is here so that every hint an attribute can leave has somewhere to be looked at. A game
/// writing its first component can read this file and copy the two or three lines it needs.
/// </para>
/// <para>
/// It is also what the editor's own drawers are checked against. A drawer that stops working is
/// visible here on the first entity somebody puts this on.
/// </para>
/// </remarks>
[Behavior]
public partial struct Showcase
{
    /// <summary>A bar between two ends, in the units the label says.</summary>
    [Header("Numbers")]
    [Range(0d, 20d)]
    [Unit("m/s")]
    [Tooltip("How fast the thing goes. Dragged on the bar, or typed exactly.")]
    [Label("Speed")]
    public float Speed;

    /// <summary>A plain number with a step of its own.</summary>
    [Step(0.01d)]
    [Unit("kg")]
    [Tooltip("Dragged a hundredth at a time, because a gram matters here.")]
    public float Mass;

    /// <summary>A whole number.</summary>
    [Range(0d, 10d)]
    [Tooltip("Whole numbers only, however finely the bar is dragged.")]
    public int Count;

    /// <summary>Three numbers that are a colour.</summary>
    [Header("Looks")]
    [Space]
    [Colour]
    [Tooltip("The patch shows what the three numbers come to.")]
    public Vec3 Tint;

    /// <summary>Three numbers that are a place.</summary>
    [Unit("m")]
    public Vec3 Offset;

    /// <summary>Something that is on or off, and which the row below answers to.</summary>
    [Header("Behaviour")]
    [Space]
    public bool Enabled;

    /// <summary>Shown only while the one above is off.</summary>
    [ShowIf(nameof(Enabled), Not = true)]
    [Tooltip("Only worth setting while the thing is switched off.")]
    public float Fallback;

    /// <summary>One of a fixed set of names.</summary>
    public ShowcaseMode Mode;

    /// <summary>Any number of a fixed set of names at once.</summary>
    [Tooltip("Each one is on or off, and any number of them can be on.")]
    public ShowcaseParts Parts;

    /// <summary>Worked out rather than set, so it is shown and not edited.</summary>
    [ReadOnly]
    [Tooltip("Counted by the component itself.")]
    public int Ticks;

    /// <summary>
    /// A property, which is read and written through itself.
    /// </summary>
    /// <remarks>
    /// Miles an hour over metres a second, kept in one unit and shown in another. A tool that went
    /// round the property and wrote the field would show a number nothing else in the program
    /// agrees with, which is why a property is described rather than what is behind it.
    /// </remarks>
    [Unit("mph")]
    [Tooltip("The same speed in the other unit. Typing here changes the one above.")]
    public float Miles
    {
        readonly get => Speed * 2.237f;
        set => Speed = value / 2.237f;
    }

    /// <summary>A property with no setter, which is a row that can be read and not changed.</summary>
    [Tooltip("Worked out from the numbers above.")]
    public readonly float Momentum => Speed * Mass;

    /// <summary>The component's own state, which is nobody else's business.</summary>
    [Hidden]
    public float Working;

    /// <summary>A picture, chosen from what is under the asset root.</summary>
    [Header("Assets")]
    [Space]
    [Asset(AssetKind.Image)]
    [Tooltip("Any picture under the asset root. Pressing the row offers them.")]
    public AssetHandle Picture;

    /// <summary>A model, chosen the same way.</summary>
    [Asset(AssetKind.Mesh)]
    [Tooltip("A model file. What a handle points at is shown by name rather than by number.")]
    public AssetHandle Shape;

    /// <summary>A bar with no box beside it, since the number means nothing on its own.</summary>
    [Header("Bars")]
    [Space]
    [Range(0d, 1d, Readout = SliderReadout.None)]
    [Tooltip("A bar and nothing else. There is no number worth typing here.")]
    public float Blend;

    /// <summary>A bar with the number beside it, which cannot be typed into.</summary>
    [Range(0d, 100d, Readout = SliderReadout.Number)]
    [Unit("%")]
    public float Fill;

    /// <summary>Three numbers on one line rather than three.</summary>
    [Inline]
    [Tooltip("One value read left to right, in the room one row costs.")]
    public Vec3 Corner;

    /// <summary>A line of text with no name beside it, across the whole panel.</summary>
    [Wide]
    [Tooltip("The name column has nothing to add to a sentence.")]
    public int Seed;

    /// <summary>Something rebuilt when the radius changes.</summary>
    [Separator]
    [Foldout("Advanced")]
    [Info("Changing the radius rebuilds the shape.", Kind = NoteKind.Warning)]
    [OnValueChanged(nameof(Rebuild))]
    [Unit("m")]
    public float Radius;

    /// <summary>How many times that has happened, which the button above changes.</summary>
    [Foldout("Advanced")]
    [ReadOnly]
    public int Rebuilds;

    /// <summary>Shown only while the mode is the one that is going.</summary>
    [Foldout("Advanced")]
    [ShowIf(nameof(Mode), ShowcaseMode.Running)]
    [Tooltip("Only there while the mode above says Running.")]
    public float WhileRunning;

    /// <summary>Inside a fold inside a fold.</summary>
    [Foldout("Advanced/Debug")]
    public bool Noisy;

    /// <summary>The same, so there is more than one row in the inner fold.</summary>
    [Foldout("Advanced/Debug")]
    public int Every;

    /// <summary>Counts a tick, so the read-only row has something to say.</summary>
    [Button("Count one")]
    [Tooltip("Adds one to the count above.")]
    public void Tick() => Ticks++;

    /// <summary>The first of three buttons on one line.</summary>
    [Button("Save", Line = ButtonLine.Start, Weight = 2d)]
    [Tooltip("Twice as wide as the two beside it, because it is the one being asked for.")]
    public void Save() => Working = Speed;

    /// <summary>The second.</summary>
    [Button("Load", Line = ButtonLine.Middle)]
    public void Load() => Speed = Working;

    /// <summary>The third, after which the line is closed.</summary>
    [Button("Clear", Line = ButtonLine.End)]
    public void Clear() => Working = 0f;

    /// <summary>What a change to the radius calls.</summary>
    [Hidden]
    public void Rebuild() => Rebuilds++;

    /// <summary>Puts the numbers back where they started.</summary>
    [Button("Put it back")]
    public void Reset()
    {
        Speed = 0f;
        Mass = 1f;
        Count = 0;
        Ticks = 0;
    }
}

/// <summary>The pieces <see cref="Showcase"/> can have, any number at a time.</summary>
[Flags]
public enum ShowcaseParts
{
    /// <summary>None of them.</summary>
    None = 0,

    /// <summary>The first.</summary>
    Head = 1,

    /// <summary>The second.</summary>
    Body = 2,

    /// <summary>The third.</summary>
    Tail = 4,
}

/// <summary>The modes <see cref="Showcase"/> can be in.</summary>
public enum ShowcaseMode
{
    /// <summary>Doing nothing.</summary>
    Idle,

    /// <summary>Going.</summary>
    Running,

    /// <summary>Stopped on purpose.</summary>
    Paused,
}
