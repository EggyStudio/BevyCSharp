namespace Bevy;

/// <summary>
/// Marks a struct as a behavior, a component and its systems declared as one type.
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
/// [OnUpdate] public void Tick(BehaviorContext ctx) =&gt; Angle += Speed * ctx.Time.Delta; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Struct, Inherited = false)]
public sealed class BehaviorAttribute : Attribute;
