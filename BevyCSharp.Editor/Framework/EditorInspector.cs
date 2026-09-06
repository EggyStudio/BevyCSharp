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

    /// <summary>Several of those, sharing a line.</summary>
    Buttons,

    /// <summary>A fold inside a component, which opens and shuts on its own.</summary>
    Group,

    /// <summary>A word over a group of fields, from a heading attribute.</summary>
    Note,

    /// <summary>A sentence in the panel: something worth knowing, or a warning.</summary>
    Info,

    /// <summary>A line across the panel, for a break with no word to it.</summary>
    Separator,

    /// <summary>A blank line, for a break without a line either.</summary>
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
/// <param name="Text">What a note, a heading or a fold says.</param>
/// <param name="Line">What draws a line something else contributed.</param>
/// <param name="Depth">How many folds deep the line sits.</param>
/// <param name="Key">What a fold is remembered by, when the line is one.</param>
/// <param name="Note">How loudly a sentence is said.</param>
/// <param name="Buttons">The methods sharing a line, when several do.</param>
public readonly record struct InspectorLine(
    InspectorLineKind Kind,
    ComponentSchema? Schema = null,
    ComponentField? Field = null,
    IFieldDrawer? Drawer = null,
    int Part = 0,
    ComponentMethod? Method = null,
    int Component = 0,
    string Text = "",
    IInspectorLine? Line = null,
    int Depth = 0,
    string Key = "",
    NoteKind Note = NoteKind.Heading,
    IReadOnlyList<ComponentMethod>? Buttons = null)
{
    /// <summary>What the line says while the pointer is over it.</summary>
    public string Tooltip => Kind switch
    {
        InspectorLineKind.Field => Field?.Hints.Tooltip ?? string.Empty,
        InspectorLineKind.Method => Method?.Hints.Tooltip ?? string.Empty,
        InspectorLineKind.Heading => Schema?.QualifiedName ?? string.Empty,
        _ => string.Empty,
    };

    /// <summary>
    /// Whether two lines stand for the same thing.
    /// </summary>
    /// <remarks>
    /// Written out rather than left to the compiler because of the list. A row that is redrawn
    /// every frame is compared against what it held last frame to notice when it has turned into
    /// something else, and a list compared by reference is a fresh one every frame, which would
    /// make every button row look like it had just turned.
    /// </remarks>
    public bool Equals(InspectorLine other) =>
        Kind == other.Kind
        && ReferenceEquals(Schema, other.Schema)
        && ReferenceEquals(Field, other.Field)
        && ReferenceEquals(Drawer, other.Drawer)
        && Part == other.Part
        && Equals(Method, other.Method)
        && Component == other.Component
        && Text == other.Text
        && ReferenceEquals(Line, other.Line)
        && Depth == other.Depth
        && Key == other.Key
        && Note == other.Note
        && Same(Buttons, other.Buttons);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(Kind, Schema, Field, Part, Component, Text, Depth, Key);

    /// <summary>Whether two lists of buttons hold the same methods in the same order.</summary>
    private static bool Same(
        IReadOnlyList<ComponentMethod>? left, IReadOnlyList<ComponentMethod>? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        if (left.Count != right.Count) return false;

        for (var i = 0; i < left.Count; i++)
        {
            if (!Equals(left[i], right[i])) return false;
        }

        return true;
    }
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

    /// <summary>Which folds inside components are shut, by key.</summary>
    public IReadOnlySet<string> Folds { get; init; } = new HashSet<string>();

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

    /// <summary>Adds a sentence in the panel, said as loudly as asked.</summary>
    public void Say(string text, NoteKind kind = NoteKind.Info) =>
        Lines.Add(new InspectorLine(InspectorLineKind.Info, Text: text, Note: kind));

    /// <summary>Adds a line across the panel.</summary>
    public void Rule() => Lines.Add(new InspectorLine(InspectorLineKind.Separator));

    /// <summary>Adds a blank line.</summary>
    public void Gap() => Lines.Add(new InspectorLine(InspectorLineKind.Gap));

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
    /// <param name="folds">Which folds inside components are shut, by key.</param>
    public static IReadOnlyList<InspectorLine> Build(
        EcsWorld world,
        Entity entity,
        IReadOnlySet<int> shut,
        List<(ComponentSchema? Schema, int Component)> tags,
        IReadOnlyList<Entity>? others = null,
        IReadOnlySet<string>? folds = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(tags);

        var plan = new InspectorPlan(world, entity)
        {
            Shut = shut,
            Others = others ?? [],
            Folds = folds ?? new HashSet<string>(),
        };

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

    /// <summary>
    /// Puts one component's fields and methods into the plan.
    /// </summary>
    /// <remarks>
    /// The order is what the component declared, adjusted by whatever asked to be moved, and the
    /// folds are read off the members as they go past. A fold is opened when the first member
    /// inside it is reached and everything after it that names the same fold falls inside; a shut
    /// fold swallows its members and its inner folds both.
    /// </remarks>
    private static void Members(
        InspectorPlan plan, ComponentSchema schema, EcsWorld world, Entity entity)
    {
        // Where the reader is, as a path of fold names. It only ever changes at a member that asked
        // for a different one, which is what makes consecutive fields share a fold without anything
        // having to say where one ends.
        var open = System.Array.Empty<string>();
        var group = new List<ComponentMethod>();

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

            var depth = Enter(plan, schema, ref open, field.Hints.Foldout);
            if (Buried(plan, schema, open)) continue;

            Above(
                plan,
                field.Hints.Space,
                field.Hints.Separator,
                field.Hints.Header,
                field.Hints.Note,
                field.Hints.NoteKind);

            var drawer = EditorDrawers.For(field);

            for (var part = 0; part < drawer.Lines(field); part++)
            {
                plan.Add(new InspectorLine(
                    InspectorLineKind.Field, schema, field, drawer, part, Depth: depth));
            }
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

            var depth = Enter(plan, schema, ref open, method.Hints.Foldout);
            if (Buried(plan, schema, open)) continue;

            // A row of buttons is written as a start, some middles and an end. Anything that
            // arrives while a row is open joins it, and the row is closed by its end, by a button
            // that says it is on its own, or by there being no more room on the line.
            switch (method.Hints.Line)
            {
                case ButtonLine.Start:
                    Flush(plan, schema, group, depth);
                    Above(
                        plan,
                        method.Hints.Space,
                        method.Hints.Separator,
                        method.Hints.Header,
                        method.Hints.Note,
                        method.Hints.NoteKind);

                    group.Add(method);
                    break;

                case ButtonLine.Middle or ButtonLine.End when group.Count > 0:
                    group.Add(method);
                    if (method.Hints.Line == ButtonLine.End || group.Count == InspectorRow.Slots)
                        Flush(plan, schema, group, depth);

                    break;

                default:
                    Flush(plan, schema, group, depth);
                    Above(
                        plan,
                        method.Hints.Space,
                        method.Hints.Separator,
                        method.Hints.Header,
                        method.Hints.Note,
                        method.Hints.NoteKind);

                    plan.Add(new InspectorLine(
                        InspectorLineKind.Method, schema, Method: method, Depth: depth));

                    break;
            }
        }

        Flush(plan, schema, group, open.Length);
    }

    /// <summary>Puts whatever a member asked to have above it above it.</summary>
    /// <remarks>
    /// In the order somebody reading down the panel would want them: the blank line, then the rule,
    /// then the word, then the sentence, then the field itself.
    /// </remarks>
    private static void Above(
        InspectorPlan plan,
        bool space,
        bool rule,
        string? header,
        string? note,
        NoteKind kind)
    {
        if (space) plan.Gap();
        if (rule) plan.Rule();
        if (header is { Length: > 0 } word) plan.Note(word);
        if (note is { Length: > 0 } said) plan.Say(said, kind);
    }

    /// <summary>Closes a row of buttons, if one is open.</summary>
    private static void Flush(
        InspectorPlan plan, ComponentSchema schema, List<ComponentMethod> group, int depth)
    {
        if (group.Count == 0) return;

        // One button on a line is a line with one button on it, whatever it was written as. There
        // is no reason for a different kind of row, and one of them can be dragged into a menu.
        plan.Add(group.Count == 1
            ? new InspectorLine(
                InspectorLineKind.Method, schema, Method: group[0], Depth: depth)
            : new InspectorLine(
                InspectorLineKind.Buttons, schema, Depth: depth, Buttons: [.. group]));

        group.Clear();
    }

    /// <summary>
    /// Moves the reader into the fold a member asked for, adding whatever headings that opens.
    /// </summary>
    /// <returns>How many folds deep the member itself sits.</returns>
    private static int Enter(
        InspectorPlan plan, ComponentSchema schema, ref string[] open, string? path)
    {
        var wanted = Path(path);

        // Only the part of the path that is new gets a heading. Going from Advanced/Debug back to
        // Advanced is not a new fold, and drawing one there would be a second Advanced under the
        // first.
        var shared = 0;
        while (shared < open.Length
            && shared < wanted.Length
            && open[shared] == wanted[shared]) shared++;

        for (var level = shared; level < wanted.Length; level++)
        {
            // A fold inside a shut fold is not drawn at all, its own heading included: it is a row
            // inside something that is closed. A shut fold's own heading is drawn, since that is
            // what somebody opens it again with.
            if (level > 0 && Shut(plan, schema, wanted, level - 1)) break;

            var key = Fold(schema, wanted, level);

            plan.Add(new InspectorLine(
                InspectorLineKind.Group,
                schema,
                Component: schema.Id,
                Text: wanted[level],
                Depth: level,
                Key: key));

            if (plan.Folds.Contains(key)) break;
        }

        open = wanted;
        return wanted.Length;
    }

    /// <summary>Whether the fold a member sits in, or any fold above it, is shut.</summary>
    private static bool Buried(InspectorPlan plan, ComponentSchema schema, string[] open)
    {
        for (var level = 0; level < open.Length; level++)
        {
            if (Shut(plan, schema, open, level)) return true;
        }

        return false;
    }

    /// <summary>Whether one level of a path is shut, or anything above it is.</summary>
    private static bool Shut(
        InspectorPlan plan, ComponentSchema schema, string[] path, int level)
    {
        for (var above = 0; above <= level; above++)
        {
            if (plan.Folds.Contains(Fold(schema, path, above))) return true;
        }

        return false;
    }

    /// <summary>What a fold is remembered by: which component, and which levels.</summary>
    /// <remarks>
    /// By name rather than by id, because a fold outlives the world an id belongs to. Somebody who
    /// shut the advanced settings of a light meant it about lights.
    /// </remarks>
    public static string Fold(ComponentSchema schema, IReadOnlyList<string> path, int level)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(path);

        return schema.Name + "/" + string.Join("/", path.Take(level + 1));
    }

    /// <summary>A fold path split into its levels, with the empty parts thrown away.</summary>
    private static string[] Path(string? path) =>
        path is { Length: > 0 }
            ? path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];

    /// <summary>
    /// Whether every condition a field asked for holds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A condition names another field of the same component and, when it has one, what that field
    /// has to read as. Without a value it asks whether the field is on, which is the same question
    /// of a flag and the one nearly every condition is.
    /// </para>
    /// <para>
    /// A condition naming a field that is not there shows the row. A row that disappears because an
    /// attribute has a typo in it is worse than one that should not have been there, and there is
    /// no way to report the typo from inside a panel.
    /// </para>
    /// </remarks>
    private static bool Shows(
        ComponentField field, ComponentSchema schema, EcsWorld world, Entity entity)
    {
        foreach (var condition in field.Hints.Conditions)
        {
            if (schema.Field(condition.Field) is not { } other) continue;
            if (other.Read(world, entity) is not { } value) continue;

            var holds = condition.Value is { } wanted ? Reads(value, wanted) : Truthy(value);
            if (holds == condition.Not) return false;
        }

        return true;
    }

    /// <summary>Whether a value reads as what a condition asked for.</summary>
    /// <remarks>
    /// Compared as written rather than as typed. What the attribute carries is the word somebody
    /// wrote, and what the field answers with is a number, a flag or one of an enum's names, so the
    /// comparison that works for all of them is the one done in words. An enum arrives as its name
    /// already, since the number behind a name is not what the field reads as.
    /// </remarks>
    private static bool Reads(object value, string wanted)
    {
        var written = value switch
        {
            bool flag => flag ? "true" : "false",
            IFormattable number => number.ToString(
                null, System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

        return string.Equals(written, wanted, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Whether a value counts as on.</summary>
    /// <remarks>
    /// A flag is what a condition without a value nearly always names. A number counts as on when
    /// it is not nought, and a handle or a name when it is there at all, which is what somebody
    /// writing the condition meant either way.
    /// </remarks>
    private static bool Truthy(object value) => value switch
    {
        bool flag => flag,
        int number => number != 0,
        uint number => number != 0u,
        long number => number != 0L,
        float number => number != 0f,
        double number => number != 0d,
        string text => text.Length > 0,
        Entity entity => !entity.IsNone,
        _ => true,
    };

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
