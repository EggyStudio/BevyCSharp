namespace Bevy;

/// <summary>
/// Marks a struct as a behavior: a component and its systems declared as one type.
/// </summary>
/// <remarks>
/// <para>
/// The struct itself is registered with Bevy as a real component, so its fields are per-entity
/// state living in Bevy's tables. Its methods become systems:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///     A <b>static</b> stage method is a plain system, it runs once per frame, and is the
///     right shape for global logic that queries other components.
///     </description>
///   </item>
///   <item>
///     <description>
///     An <b>instance</b> stage method runs once per entity carrying this behavior, with
///     <c>this</c> bound by reference to that entity's component. Writes to fields land
///     straight in Bevy's storage.
///     </description>
///   </item>
/// </list>
/// <para>
/// The struct must be <c>partial</c>, so the generator can add the runner alongside it. Fields
/// must be blittable, since the layout is handed to Bevy verbatim.
/// </para>
/// </remarks>
/// <example>
/// A behavior with no state, driving other components:
/// <code>
/// [Behavior]
/// public partial struct Gravity
/// {
///     [OnUpdate]
///     public static void Apply(BehaviorContext ctx)
///     {
///         float dt = ctx.Time.Delta;
///         foreach (var row in ctx.Ecs.Query&lt;Velocity&gt;())
///             row.Component.Y -= 9.81f * dt;
///     }
/// }
/// </code>
/// A behavior that is itself the component, one instance per entity:
/// <code>
/// [Behavior]
/// public partial struct Spinner
/// {
///     public float Angle;
///     public float Speed;
///
///     [OnStartup]
///     public static void Spawn(BehaviorContext ctx)
///     {
///         var entity = ctx.Ecs.Spawn();
///         ctx.Ecs.Add(entity, new Spinner { Speed = 2f });
///     }
///
///     [OnUpdate]
///     public void Tick(BehaviorContext ctx) =&gt; Angle += Speed * ctx.Time.Delta;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Struct, Inherited = false)]
public sealed class BehaviorAttribute : Attribute;

/// <summary>
/// Marks a generated method that registers behaviors, so
/// <see cref="BehaviorsPlugin"/> can find it by reflection.
/// </summary>
/// <remarks>Emitted by the generator. There is no reason to apply it by hand.</remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class GeneratedBehaviorRegistrationAttribute : Attribute;

/// <summary>Runs once at startup, before the first frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class OnStartupAttribute : Attribute;

/// <summary>Runs at the top of every frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class OnFirstAttribute : Attribute;

/// <summary>Runs before <see cref="OnUpdateAttribute"/> each frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class OnPreUpdateAttribute : Attribute;

/// <summary>Runs in the main update stage each frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class OnUpdateAttribute : Attribute;

/// <summary>Runs after <see cref="OnUpdateAttribute"/>, before queued commands are applied.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class OnPostUpdateAttribute : Attribute;

/// <summary>Runs in the render stage each frame, before <see cref="OnLastAttribute"/>.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class OnRenderAttribute : Attribute;

/// <summary>Runs at the very end of every frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class OnLastAttribute : Attribute;

/// <summary>Runs once after the main loop exits.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class OnCleanupAttribute : Attribute;

/// <summary>
/// Restricts an instance method to entities that also carry all the listed components.
/// </summary>
/// <remarks>
/// Resolved per archetype rather than per entity, so the filter costs nothing in the loop.
/// </remarks>
/// <example>
/// <code>
/// [OnUpdate]
/// [With(typeof(Alive), typeof(Visible))]
/// public void Tick(BehaviorContext ctx) { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class WithAttribute : Attribute
{
    /// <summary>The components that must be present.</summary>
    public Type[] Types { get; }

    /// <summary>Requires all of <paramref name="types"/>.</summary>
    public WithAttribute(params Type[] types) => Types = types;
}

/// <summary>Skips entities carrying any of the listed components.</summary>
/// <remarks>Resolved per archetype, like <see cref="WithAttribute"/>.</remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class WithoutAttribute : Attribute
{
    /// <summary>The components that must be absent.</summary>
    public Type[] Types { get; }

    /// <summary>Excludes any of <paramref name="types"/>.</summary>
    public WithoutAttribute(params Type[] types) => Types = types;
}

/// <summary>
/// Skips entities where none of the listed components changed since the previous frame.
/// </summary>
/// <remarks>
/// Unlike <see cref="WithAttribute"/>, this is a per-entity test against Bevy's change ticks,
/// so a method carrying it is iterated on the main thread rather than fanned out in parallel.
/// That is usually the better trade anyway: the filter exists to make the set small.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ChangedAttribute : Attribute
{
    /// <summary>The components to watch.</summary>
    public Type[] Types { get; }

    /// <summary>Requires a change in any of <paramref name="types"/>.</summary>
    public ChangedAttribute(params Type[] types) => Types = types;
}

/// <summary>
/// Skips the system for a frame unless a static <see cref="bool"/> member says otherwise.
/// </summary>
/// <remarks>
/// The member must be declared on the same behavior struct and be a static bool field,
/// property, or a method taking a <see cref="World"/>. Use <c>nameof</c> so a rename cannot
/// silently break it.
/// </remarks>
/// <example>
/// <code>
/// [OnUpdate]
/// [RunIf(nameof(IsPlaying))]
/// public static void Tick(BehaviorContext ctx) { }
///
/// public static bool IsPlaying(World world) =&gt;
///     world.TryGetResource&lt;GameState&gt;(out var state) &amp;&amp; state.Playing;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class RunIfAttribute : Attribute
{
    /// <summary>Name of the static bool member on the same struct.</summary>
    public string MemberName { get; }

    /// <summary>References a condition member by name.</summary>
    public RunIfAttribute(string memberName) => MemberName = memberName;
}

/// <summary>
/// Binds a shortcut that switches the system on and off, with no boilerplate.
/// </summary>
/// <remarks>
/// <para>
/// Each press of the shortcut flips the state, which lives in
/// <see cref="SystemToggleRegistry"/>. This is what debug overlays want: one attribute instead
/// of a resource, a run condition and a key handler.
/// </para>
/// <para>
/// <see cref="KeyModifier"/> is a flags enum, so a shortcut can require any number of modifiers
/// at once. Each flag is side-agnostic, which is what a shortcut normally means; to pin one
/// side, or to build a chord out of an ordinary key, write the check yourself with
/// <see cref="Input.AllKeysDown"/> behind a <see cref="RunIfAttribute"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [ToggleKey(Key.F3)]                                       // F3
/// [ToggleKey(Key.F3, KeyModifier.Ctrl)]                     // Ctrl + F3, either Ctrl
/// [ToggleKey(Key.F3, KeyModifier.Ctrl | KeyModifier.Shift)] // Ctrl + Shift + F3
/// [ToggleKey(Key.F3, DefaultEnabled = false)]               // starts off
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ToggleKeyAttribute : Attribute
{
    /// <summary>The key whose press flips the system on or off.</summary>
    public Key Key { get; }

    /// <summary>Modifiers that must be held with <see cref="Key"/>. Combine with <c>|</c>.</summary>
    public KeyModifier Modifiers { get; }

    /// <summary>Whether the system starts enabled. Defaults to <see langword="true"/>.</summary>
    public bool DefaultEnabled { get; init; } = true;

    /// <summary>Binds <paramref name="key"/>, optionally with modifiers held.</summary>
    /// <param name="key">The key whose press flips the system.</param>
    /// <param name="modifiers">Modifiers that must be held; combine them with <c>|</c>.</param>
    public ToggleKeyAttribute(Key key, KeyModifier modifiers = KeyModifier.None)
    {
        Key = key;
        Modifiers = modifiers;
    }
}
