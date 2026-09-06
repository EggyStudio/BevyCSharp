using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>What one line of an inspector stands for.</summary>
public enum InspectorLineKind
{
    /// <summary>Nothing.</summary>
    Empty,

    /// <summary>A component's name. It folds, and offers what can be done to it.</summary>
    Heading,

    /// <summary>The thing's own name, which is not a component's field.</summary>
    Subject,

    /// <summary>One row of one field, drawn by whichever drawer took it.</summary>
    Field,

    /// <summary>Something the component can be told to do.</summary>
    Method,

    /// <summary>A word over a group of fields, from a heading attribute.</summary>
    Note,

    /// <summary>A blank line, for a break without a word.</summary>
    Gap,

    /// <summary>A line something else contributed, which draws and reads itself.</summary>
    Custom,
}

/// <summary>
/// A line something outside the inspector put in.
/// </summary>
/// <remarks>
/// What makes the panel extensible past the fields of a component: a warning about a missing
/// asset, a button that only makes sense for one kind of thing, a readout of something worked out
/// rather than stored. It gets a row and the same pieces every other line has.
/// </remarks>
public interface IInspectorLine
{
    /// <summary>Fills the row.</summary>
    void Draw(InspectorRow row);

    /// <summary>Takes back whatever was typed or ticked.</summary>
    void Read(InspectorRow row)
    {
    }

    /// <summary>Answers a click on the row's button.</summary>
    void Press(InspectorRow row)
    {
    }
}

/// <summary>
/// One line of an inspector, before it is given a row.
/// </summary>
/// <param name="Kind">What the line stands for.</param>
/// <param name="Schema">The component it belongs to.</param>
/// <param name="Field">The field it edits.</param>
/// <param name="Drawer">What draws that field, and reads it back.</param>
/// <param name="Part">Which of the drawer's rows this is.</param>
/// <param name="Method">The method it runs.</param>
/// <param name="Component">The component id, for a heading with no schema.</param>
/// <param name="Text">What a note says.</param>
/// <param name="Line">What draws a line something else contributed.</param>
public readonly record struct InspectorLine(
    InspectorLineKind Kind,
    ComponentSchema? Schema = null,
    ComponentField? Field = null,
    IFieldDrawer? Drawer = null,
    int Part = 0,
    ComponentMethod? Method = null,
    int Component = 0,
    string Text = "",
    IInspectorLine? Line = null)
{
    /// <summary>What the line says while the pointer is over it.</summary>
    public string Tooltip => Kind switch
    {
        InspectorLineKind.Field => Field?.Hints.Tooltip ?? string.Empty,
        InspectorLineKind.Method => Method?.Hints.Tooltip ?? string.Empty,
        InspectorLineKind.Heading => Schema?.QualifiedName ?? string.Empty,
        _ => string.Empty,
    };
}

/// <summary>
/// What an inspector is building, while it is building it.
/// </summary>
/// <remarks>
/// Passed to each pass rather than kept in a static. A pass reads what is being inspected, adds
/// lines, and says which fields and methods it has taken care of, and nothing it does outlives the
/// build.
/// </remarks>
/// <param name="World">The world being inspected.</param>
/// <param name="Entity">The entity being inspected.</param>
public sealed record InspectorPlan(EcsWorld World, Entity Entity)
{
    /// <summary>The lines so far, in the order they will be drawn.</summary>
    public List<InspectorLine> Lines { get; } = [];

    /// <summary>Which components are folded shut, by id.</summary>
    public IReadOnlySet<int> Shut { get; init; } = new HashSet<int>();

    /// <summary>Everything selected, when more than one thing is.</summary>
    public IReadOnlyList<Entity> Others { get; init; } = [];

    /// <summary>The fields something has already dealt with.</summary>
    private readonly HashSet<ComponentField> _fields = [];

    /// <summary>The methods something has already dealt with.</summary>
    private readonly HashSet<ComponentMethod> _methods = [];

    /// <summary>The components something has already dealt with.</summary>
    private readonly HashSet<int> _components = [];

    /// <summary>Adds a line.</summary>
    public void Add(InspectorLine line) => Lines.Add(line);

    /// <summary>Adds a line of somebody's own.</summary>
    public void Add(IInspectorLine line) =>
        Lines.Add(new InspectorLine(InspectorLineKind.Custom, Line: line));

    /// <summary>Adds a word over whatever comes next.</summary>
    public void Note(string text) =>
        Lines.Add(new InspectorLine(InspectorLineKind.Note, Text: text));

    /// <summary>Says a field has been dealt with, so nothing else draws it.</summary>
    public void Claim(ComponentField field) => _fields.Add(field);

    /// <summary>The same for a method.</summary>
    public void Claim(ComponentMethod method) => _methods.Add(method);

    /// <summary>The same for a whole component.</summary>
    public void Claim(int component) => _components.Add(component);

    /// <summary>Whether a field has been dealt with already.</summary>
    public bool IsClaimed(ComponentField field) => _fields.Contains(field);

    /// <inheritdoc cref="IsClaimed(ComponentField)"/>
    public bool IsClaimed(ComponentMethod method) => _methods.Contains(method);

    /// <inheritdoc cref="IsClaimed(ComponentField)"/>
    public bool IsClaimed(int component) => _components.Contains(component);
}

/// <summary>
/// How an inspector is built, and where anything else joins in.
/// </summary>
/// <remarks>
/// <para>
/// The panel asks for a list of lines and draws them. Everything about which lines there are lives
/// here: the components an entity carries, the fields of each, what the attributes on those fields
/// asked for, and whatever anything else wants to add.
/// </para>
/// <para>
/// A pass is a function and a priority. Passes run highest first, and a pass that takes a field
/// says so, which stops the ordinary drawing of it. That is enough to add a row, replace a row,
/// hide a row, or take over a whole component, without any of those being a separate mechanism.
/// </para>
/// </remarks>
public static class EditorInspector
{
    private static readonly List<(Action<InspectorPlan> Pass, int Priority)> Before = [];
    private static readonly List<(Action<InspectorPlan> Pass, int Priority)> After = [];

    private static readonly List<(Func<InspectorPlan, ComponentSchema, bool> Pass, int Priority)>
        Components = [];

    private static readonly List<(Func<InspectorPlan, ComponentField, bool> Pass, int Priority)>
        Fields = [];

    private static readonly List<(Func<InspectorPlan, ComponentMethod, bool> Pass, int Priority)>
        Methods = [];

    /// <summary>Runs before any component is drawn.</summary>
    public static void OnBefore(Action<InspectorPlan> pass, int priority = 0) =>
        Sorted(Before, pass, priority);

    /// <summary>Runs after everything else has been drawn.</summary>
    public static void OnAfter(Action<InspectorPlan> pass, int priority = 0) =>
        Sorted(After, pass, priority);

    /// <summary>Offered each component, and may take it over by answering true.</summary>
    public static void OnComponent(
        Func<InspectorPlan, ComponentSchema, bool> pass, int priority = 0) =>
        Sorted(Components, pass, priority);

    /// <summary>Offered each field, and may take it over by answering true.</summary>
    public static void OnField(Func<InspectorPlan, ComponentField, bool> pass, int priority = 0) =>
        Sorted(Fields, pass, priority);

    /// <summary>Offered each method, and may take it over by answering true.</summary>
    public static void OnMethod(
        Func<InspectorPlan, ComponentMethod, bool> pass, int priority = 0) =>
        Sorted(Methods, pass, priority);

    /// <summary>Takes a field pass back out.</summary>
    /// <remarks>
    /// For anything that adds a pass while something of its own is open, and for a test that has
    /// to leave the table as it found it.
    /// </remarks>
    public static bool RemoveField(Func<InspectorPlan, ComponentField, bool> pass) =>
        Fields.RemoveAll(entry => entry.Pass == pass) > 0;

    /// <inheritdoc cref="RemoveField"/>
    public static bool RemoveMethod(Func<InspectorPlan, ComponentMethod, bool> pass) =>
        Methods.RemoveAll(entry => entry.Pass == pass) > 0;

    /// <inheritdoc cref="RemoveField"/>
    public static bool RemoveComponent(Func<InspectorPlan, ComponentSchema, bool> pass) =>
        Components.RemoveAll(entry => entry.Pass == pass) > 0;

    /// <inheritdoc cref="RemoveField"/>
    public static bool RemoveBefore(Action<InspectorPlan> pass) =>
        Before.RemoveAll(entry => entry.Pass == pass) > 0;

    /// <inheritdoc cref="RemoveField"/>
    public static bool RemoveAfter(Action<InspectorPlan> pass) =>
        After.RemoveAll(entry => entry.Pass == pass) > 0;

    /// <summary>Adds a pass and keeps the list in the order they are run.</summary>
    private static void Sorted<T>(List<(T Pass, int Priority)> passes, T pass, int priority)
    {
        ArgumentNullException.ThrowIfNull(pass);

        passes.Add((pass, priority));
        passes.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    /// <summary>
    /// Works out every line for an entity.
    /// </summary>
    /// <param name="world">The world it lives in.</param>
    /// <param name="entity">The entity.</param>
    /// <param name="shut">Which components are folded shut, by id.</param>
    /// <param name="tags">Filled with the components that have nothing to show.</param>
    /// <param name="others">
    /// The rest of the selection, when more than one thing is selected. A component only some of
    /// them carry is left out: an inspector showing a field that half the selection has no room
    /// for is one where an edit does something to some of them and nothing to the others.
    /// </param>
    public static IReadOnlyList<InspectorLine> Build(
        EcsWorld world,
        Entity entity,
        IReadOnlySet<int> shut,
        List<(ComponentSchema? Schema, int Component)> tags,
        IReadOnlyList<Entity>? others = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(tags);

        var plan = new InspectorPlan(world, entity) { Shut = shut, Others = others ?? [] };

        foreach (var (pass, _) in Before) pass(plan);

        foreach (var id in world.ComponentsOf(entity))
        {
            if (!Shared(world, entity, id, others)) continue;

            var schema = ComponentSchemas.For(id);

            // A component with nothing to show is a tag, and a tag belongs on a chip rather than
            // under a heading with an empty space beneath it. Two things end up here: a marker
            // with no fields, and one this side has no description of, and to somebody reading
            // the panel they are the same statement.
            if (schema is null || schema.Fields.Count == 0)
            {
                if (schema is null && EditorEntity.IsDerived(world, id)) continue;

                tags.Add((schema, id));
                continue;
            }

            var taken = false;
            foreach (var (pass, _) in Components)
            {
                if (!pass(plan, schema)) continue;

                taken = true;
                break;
            }

            if (taken || plan.IsClaimed(id)) continue;

            plan.Add(new InspectorLine(InspectorLineKind.Heading, schema, Component: id));

            // Shut is shut: what a component block is for is being able to put away the ones you
            // are not working on, and an inspector where you cannot is a column of scrolling.
            if (shut.Contains(id)) continue;

            Members(plan, schema, world, entity);
        }

        foreach (var (pass, _) in After) pass(plan);

        return plan.Lines;
    }

    /// <summary>
    /// Whether every selected entity carries a component.
    /// </summary>
    /// <remarks>
    /// A component that cannot resolve an id on this build is skipped rather than allowed to take
    /// the inspector down with it, which is the same care the list of what can be added takes.
    /// </remarks>
    private static bool Shared(
        EcsWorld world, Entity entity, int component, IReadOnlyList<Entity>? others)
    {
        if (others is not { Count: > 1 }) return true;

        foreach (var other in others)
        {
            if (other == entity) continue;

            try
            {
                if (!world.HasById(other, component)) return false;
            }
            catch (Bevy.Interop.BevyNativeException)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Puts one component's fields and methods into the plan.</summary>
    private static void Members(
        InspectorPlan plan, ComponentSchema schema, EcsWorld world, Entity entity)
    {
        foreach (var field in Ordered(schema.Fields))
        {
            if (field.Hints.Hidden) continue;
            if (!Shows(field, schema, world, entity)) continue;

            var taken = false;
            foreach (var (pass, _) in Fields)
            {
                if (!pass(plan, field)) continue;

                taken = true;
                break;
            }

            if (taken || plan.IsClaimed(field)) continue;

            if (field.Hints.Space) plan.Add(new InspectorLine(InspectorLineKind.Gap));
            if (field.Hints.Header is { Length: > 0 } header) plan.Note(header);

            var drawer = EditorDrawers.For(field);

            for (var part = 0; part < drawer.Lines(field); part++)
                plan.Add(new InspectorLine(InspectorLineKind.Field, schema, field, drawer, part));
        }

        foreach (var method in Ordered(schema.Methods))
        {
            if (method.Hints.Hidden) continue;

            var taken = false;
            foreach (var (pass, _) in Methods)
            {
                if (!pass(plan, method)) continue;

                taken = true;
                break;
            }

            if (taken || plan.IsClaimed(method)) continue;

            plan.Add(new InspectorLine(InspectorLineKind.Method, schema, Method: method));
        }
    }

    /// <summary>
    /// Whether a field asked to be shown only under a condition, and whether that holds.
    /// </summary>
    /// <remarks>
    /// The condition is another field of the same component that reads as true or false. A
    /// condition naming a field that is not there, or one that is not a flag, shows the row: a row
    /// that disappears because an attribute has a typo in it is worse than one that should not
    /// have been there.
    /// </remarks>
    private static bool Shows(
        ComponentField field, ComponentSchema schema, EcsWorld world, Entity entity)
    {
        if (field.Hints.ShowIf is not { Length: > 0 } named) return true;

        foreach (var other in schema.Fields)
        {
            if (other.Name != named) continue;
            if (other.Read(world, entity) is not bool on) return true;

            return field.Hints.ShowIfNot ? !on : on;
        }

        return true;
    }

    /// <summary>The fields in the order they asked to be in, and otherwise as declared.</summary>
    private static IEnumerable<ComponentField> Ordered(IReadOnlyList<ComponentField> fields) =>
        fields.Select((field, index) => (field, index))
            .OrderBy(pair => pair.field.Hints.Order)
            .ThenBy(pair => pair.index)
            .Select(pair => pair.field);

    /// <inheritdoc cref="Ordered(IReadOnlyList{ComponentField})"/>
    private static IEnumerable<ComponentMethod> Ordered(IReadOnlyList<ComponentMethod> methods) =>
        methods.Select((method, index) => (method, index))
            .OrderBy(pair => pair.method.Hints.Order)
            .ThenBy(pair => pair.index)
            .Select(pair => pair.method);
}
