using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

// Writes CHEATSHEET.md from the built library and its XML documentation, every public method of
// every public type in Bevy and Bevy.Physics a line each, grouped as the guide is, and its line
// the first sentence of its summary. CheatsheetTests holds the page to the library by the same
// rules, so a type or method this leaves out the test leaves out too.
//
//   dotnet build BevyCSharp && dotnet run --project build/cheatsheet -- .
//
// A type not named in a group below is listed under "Everything else", so a new one is never left
// off the page, and naming it in its group moves it there.
var root = args[0];
var asm = typeof(Bevy.App).Assembly;
var xml = XDocument.Load(Path.Combine(root, "BevyCSharp/bin/Debug/net10.0/BevyCSharp.xml"));
var docs = xml.Descendants("member").ToDictionary(m => (string)m.Attribute("name")!, m => m);

var groups = new (string Title, string Page, string[] Types)[]
{
    ("Running an app", "running-a-game.md", ["BevyApp", "App", "Config", "World", "IPlugin", "IPluginGroup", "DefaultPlugins", "EnginePlugin", "BehaviorsPlugin", "Time", "FrameProfile"]),
    ("Behaviors and systems", "behaviors.md", ["BehaviorContext", "BehaviorConditions", "BehaviorRegistry", "BehaviorRunners", "SystemDescriptor", "StageOrder", "SystemToggleRegistry", "SystemRegistrationSourceScope", "EcsWorld", "EcsCommands", "ChunkSet`1", "ReloadedComponents"]),
    ("States", "states.md", ["StateRegistry"]),
    ("Messages", "messages-and-hierarchy.md", ["MessageBus", "On`1"]),
    ("Components", "components.md", ["ComponentHooks", "ComponentSchema", "ComponentSchemas", "ComponentField", "ItemFields", "EcsList`1", "EcsMap`2", "IInlineList`1", "InlineList4`1", "InlineList8`1", "InlineList16`1", "InlineList32`1", "InlineList64`1", "ListValue", "MapValue", "IReflectedComponent`1"]),
    ("Scenes and saves", "scenes-and-saves.md", ["DataAssets", "SceneFile", "SceneInstances", "SceneReferences", "SceneValue", "SaveGame", "SaveId", "Persistent`1", "IPersistentValue", "ProjectSettings", "UserData"]),
    ("Assets and models", "assets-and-models.md", ["AssetServer", "AssetFiles", "AssetIds", "AssetPack", "Streaming", "GltfContents", "GltfPart", "Animation", "AnimationCurve", "AnimationTarget", "IAnimationEvent", "MeshFiles", "MaterialFiles"]),
    ("Drawing", "drawing.md", ["Render", "MeshShape", "Render2d", "CapturedImage", "CapturedTexels", "EffectSettings", "Picking"]),
    ("Shaders", "shaders.md", ["Shaders", "ShaderValues", "ShaderMaterial", "ShaderProgram", "ShaderStage", "ViewDispatch", "ViewDraw", "ComponentArray`1"]),
    ("Gizmos", "gizmos.md", ["Gizmos", "Gizmos+BatchScope", "GizmoSegment"]),
    ("The interface", "ui.md", ["Ui", "UiGrid", "Length", "Sides", "Corners", "Track", "ImGuiRuntime", "ImGuiTextures"]),
    ("Audio", "audio.md", ["Audio"]),
    ("Physics", "physics.md", ["Physics.PhysicsWorld", "Physics.PhysicsPlugin", "Physics.PhysicsShape", "Physics.Joint", "Physics.Colliders"]),
    ("Input", "input.md", ["Input", "LogicalKey", "Gamepad", "SyntheticInput", "KeyTable"]),
    ("The window", "window.md", ["Window"]),
    ("Math", "", ["Vec3", "Quat", "Color", "Transform", "GlobalTransform", "EaseFunction"]),
    ("The tools", "tools.md", ["ConsoleCommands", "ConsoleHost", "ConsoleHost+Scope", "ConsoleLog", "CliClient", "CliJson", "CliPlugin", "CliSessionFile"]),
};

bool Listed(Type t) => t.Namespace is "Bevy" or "Bevy.Physics" && !t.IsEnum && !typeof(Delegate).IsAssignableFrom(t)
    && !t.Name.Contains('<') && t.Name != "Enumerator" && Methods(t).Any();

IEnumerable<MethodInfo> Methods(Type t) =>
    t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .Where(m => !m.IsSpecialName && !m.Name.Contains('<') && !m.Name.StartsWith("get_") && !m.Name.StartsWith("set_")
            && m.Name is not ("Equals" or "GetHashCode" or "ToString" or "Deconstruct" or "PrintMembers" or "GetEnumerator"))
        .OrderBy(m => m.MetadataToken);

string Key(Type t) => (t.Namespace == "Bevy.Physics" ? "Physics." : "") + (t.DeclaringType is { } outer ? outer.Name + "+" + t.Name : t.Name);

string Heading(Type t) => Regex.Replace(Key(t).Replace('+', '.'), @"`(\d)", m => "<" + string.Join(", ", (t.GetGenericArguments().Select(a => a.Name))) + ">");

var all = asm.GetExportedTypes().Where(Listed).ToDictionary(Key);
var placed = groups.SelectMany(g => g.Types).ToHashSet();
var rest = all.Keys.Where(k => !placed.Contains(k)).OrderBy(k => k).ToArray();
var missing = placed.Where(k => !all.ContainsKey(k)).ToArray();
if (missing.Length > 0) Console.Error.WriteLine("named but not found: " + string.Join(", ", missing));

string Name(Type t)
{
    if (t.IsByRef) return Name(t.GetElementType()!);
    if (t.IsArray) return Name(t.GetElementType()!) + "[]";
    if (Nullable.GetUnderlyingType(t) is { } inner) return Name(inner) + "?";
    if (t.IsGenericParameter) return t.Name;
    var simple = t.FullName switch
    {
        "System.Void" => "void", "System.Int32" => "int", "System.UInt32" => "uint", "System.Int64" => "long", "System.UInt64" => "ulong",
        "System.Single" => "float", "System.Double" => "double", "System.Boolean" => "bool", "System.String" => "string", "System.Byte" => "byte",
        "System.Object" => "object", "System.Int16" => "short", "System.UInt16" => "ushort", "System.SByte" => "sbyte", "System.Char" => "char",
        _ => null,
    };
    if (simple is not null) return simple;
    if (t.IsGenericType && t.FullName?.StartsWith("System.ValueTuple") == true || (t.IsGenericType && t.Name.StartsWith("ValueTuple")))
        return "(" + string.Join(", ", t.GetGenericArguments().Select(Name)) + ")";
    var name = t.Name;
    if (t.IsGenericType)
        name = name[..name.IndexOf('`')] + "<" + string.Join(", ", t.GetGenericArguments().Select(Name)) + ">";
    return t.DeclaringType is { } outer && !t.IsGenericParameter ? Name(outer).Split('<')[0] + "." + name : name;
}

// A tuple's elements with the names the source gave them, which the compiler keeps in an attribute.
string Named(Type t, System.Runtime.CompilerServices.TupleElementNamesAttribute? names)
{
    var inner = Nullable.GetUnderlyingType(t) ?? t;
    if (names is null || !inner.IsGenericType || !inner.Name.StartsWith("ValueTuple")) return Name(t);
    var parts = inner.GetGenericArguments().Select((a, i) => Name(a) + (i < names.TransformNames.Count && names.TransformNames[i] is { } n ? " " + n : ""));
    return "(" + string.Join(", ", parts) + ")" + (inner != t ? "?" : "");
}

string Default(ParameterInfo p)
{
    if (!p.HasDefaultValue) return "";
    var v = p.DefaultValue;
    var text = v switch
    {
        null => p.ParameterType.IsValueType && Nullable.GetUnderlyingType(p.ParameterType) is null ? "default" : "null",
        bool b => b ? "true" : "false",
        string s => "\"" + s + "\"",
        float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture) + "f",
        double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
        Enum e => p.ParameterType.IsEnum ? Name(p.ParameterType) + "." + e : e.ToString(),
        _ => Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? "default",
    };
    if (p.ParameterType.IsEnum && v is int i) text = Name(p.ParameterType) + "." + Enum.ToObject(p.ParameterType, i);
    return " = " + text;
}

// A parameter's or a return's type as the source wrote it, a reference that may be null marked as
// it was, which only the compiler's nullability attributes say, the type itself being the same.
var nullability = new NullabilityInfoContext();
string Typed(ParameterInfo p)
{
    var type = p.ParameterType.IsByRef ? p.ParameterType.GetElementType()! : p.ParameterType;
    var text = Named(type, p.GetCustomAttribute<System.Runtime.CompilerServices.TupleElementNamesAttribute>());
    // A type parameter is marked only where it is held to classes, since reflection reads any other
    // the same whether its source marked it or not.
    var reference = type.IsGenericParameter
        ? type.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint)
        : !type.IsValueType && type != typeof(void);
    var mayBeNull = reference && nullability.Create(p).ReadState == NullabilityState.Nullable;
    return mayBeNull && !text.EndsWith('?') ? text + "?" : text;
}

string Parameter(ParameterInfo p)
{
    var prefix = p.ParameterType.IsByRef ? (p.IsOut ? "out " : p.IsIn ? "in " : "ref ") : "";
    if (p.GetCustomAttribute<ParamArrayAttribute>() is not null) prefix = "params ";
    return prefix + Typed(p) + " " + p.Name + Default(p);
}

string XmlType(Type t)
{
    if (t.IsByRef) return XmlType(t.GetElementType()!) + "@";
    if (t.IsArray) return XmlType(t.GetElementType()!) + "[]";
    if (t.IsGenericParameter) return (t.DeclaringMethod is not null ? "``" : "`") + t.GenericParameterPosition;
    if (t.IsGenericType)
    {
        var def = t.GetGenericTypeDefinition().FullName!;
        return def[..def.IndexOf('`')].Replace('+', '.') + "{" + string.Join(",", t.GetGenericArguments().Select(XmlType)) + "}";
    }
    return (t.FullName ?? t.Name).Replace('+', '.');
}

string Summary(MethodInfo m)
{
    var generic = m.IsGenericMethod ? "``" + m.GetGenericArguments().Length : "";
    var parameters = m.GetParameters();
    var id = $"M:{m.DeclaringType!.FullName!.Replace('+', '.')}.{m.Name}{generic}" + (parameters.Length > 0 ? "(" + string.Join(",", parameters.Select(p => XmlType(p.ParameterType))) + ")" : "");
    if (!docs.TryGetValue(id, out var member))
    {
        // An inherited doc, or an id spelled another way, is looked for by name and count.
        member = docs.Where(d => d.Key.StartsWith($"M:{m.DeclaringType!.FullName!.Replace('+', '.')}.{m.Name}"))
            .Select(d => d.Value).FirstOrDefault(d => Regex.Matches((string)d.Attribute("name")!, ",").Count + 1 == Math.Max(parameters.Length, 1) || parameters.Length == 0);
    }
    var summary = member?.Element("summary");

    // An implementation documented as its interface's takes the interface's line, and a member of
    // an extension block, whose documentation is filed under another spelling, the line of the
    // same name on its type.
    if (summary is null)
    {
        foreach (var face in m.DeclaringType!.GetInterfaces())
        {
            var name = face.IsGenericType ? face.GetGenericTypeDefinition().FullName! : face.FullName!;
            summary = docs.Where(d => d.Key.StartsWith($"M:{name.Replace('+', '.')}.{m.Name}")).Select(d => d.Value.Element("summary")).FirstOrDefault(e => e is not null);
            if (summary is not null) break;
        }
    }
    summary ??= docs.Where(d => Regex.IsMatch(d.Key, $@"^M:{Regex.Escape(m.DeclaringType!.FullName!.Replace('+', '.'))}(\.[^.(]+)*\.{Regex.Escape(m.Name)}(`|\(|$)"))
        .Select(d => d.Value.Element("summary")).FirstOrDefault(e => e is not null);
    if (summary is null)
    {
        // Build and Dispose say the same thing wherever they are.
        return m.Name switch
        {
            "Dispose" => "Releases what it holds",
            "Build" when m.GetParameters() is [{ ParameterType.Name: "App" }] => "Adds what it brings to the app",
            _ => "",
        };
    }
    var text = string.Concat(summary.Nodes().Select(n => n switch
    {
        XText x => x.Value,
        // A generic type's arity follows one backtick and a generic method's two, so both go whole.
        XElement e when e.Name == "see" || e.Name == "seealso" => Regex.Replace(((string?)e.Attribute("cref") ?? (string?)e.Attribute("langword") ?? e.Value).Split(':').Last().Split('(')[0].Split('.').Last(), @"`+\d+", ""),
        XElement e when e.Name == "paramref" || e.Name == "typeparamref" => (string)e.Attribute("name")!,
        XElement e when e.Name == "c" => e.Value,
        XElement e => e.Value,
        _ => "",
    }));
    text = Regex.Replace(text, @"\s+", " ").Trim();
    // The first sentence, since a line is a reminder and the page is the explanation.
    var end = Regex.Match(text, @"(?<=[a-z0-9)\]`'])\. ");
    if (end.Success) text = text[..end.Index];
    return text.TrimEnd('.');
}

var output = new StringBuilder();
output.AppendLine("# Cheatsheet");
output.AppendLine();
output.AppendLine("Every public method of the library, a line each, grouped as the guide is, with the first sentence of");
output.AppendLine("what its documentation says. A line is a reminder, and the guide page each group links to is the");
output.AppendLine("explanation. `CheatsheetTests` holds this page to the library, so a method added without its line,");
output.AppendLine("or a line left for one that is gone, fails the suite.");

void Write(string title, string page, IEnumerable<string> keys)
{
    output.AppendLine();
    output.AppendLine($"## {title}");
    if (page != "")
    {
        output.AppendLine();
        output.AppendLine($"The guide's page is [{page}](https://github.com/EggyStudio/BevyCSharp/blob/main/docs/{page}).");
    }

    foreach (var key in keys)
    {
        var type = all[key];
        output.AppendLine();
        output.AppendLine($"### `{Heading(type)}`");
        output.AppendLine();
        output.AppendLine("```csharp");
        foreach (var m in Methods(type))
        {
            var generic = m.IsGenericMethod ? "<" + string.Join(", ", m.GetGenericArguments().Select(a => a.Name)) + ">" : "";
            var declaration = $"{(m.IsStatic && !type.IsInterface ? "static " : "")}{Typed(m.ReturnParameter)} {m.Name}{generic}({string.Join(", ", m.GetParameters().Select(Parameter))});";
            var summary = Summary(m);
            output.AppendLine(summary == "" ? declaration : declaration.PadRight(Math.Max(64, declaration.Length + 2)) + "// " + summary);
        }
        output.AppendLine("```");
    }
}

foreach (var (title, page, types) in groups) Write(title, page, types.Where(all.ContainsKey));
if (rest.Length > 0) Write("Everything else", "", rest);

File.WriteAllText(Path.Combine(root, "CHEATSHEET.md"), output.ToString());
Console.WriteLine($"{all.Values.Sum(t => Methods(t).Count())} methods in {all.Count} types, {rest.Length} in Everything else: {string.Join(", ", rest)}");
