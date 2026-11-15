using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Bevy.Generator;

/// <summary>One step from a component to a handle into the managed store that it holds.</summary>
/// <param name="Name">The field's name, or for an auto-property the field the compiler keeps it in.</param>
/// <param name="Owner">The type declaring the field, fully qualified.</param>
/// <param name="Type">The field's type, fully qualified.</param>
/// <param name="Named">
/// Whether code outside the owner can name the field, where one it cannot is reached through an
/// accessor the runtime binds to it.
/// </param>
/// <param name="Items">Whether the field is an inline list whose items the step goes into.</param>
internal sealed record HandleStep(string Name, string Owner, string Type, bool Named, bool Items);

/// <summary>
/// The hooks that free the handles into the managed store a component holds as it leaves its
/// entity, and give a clone handles of its own, found from the component's whole layout.
/// </summary>
/// <remarks>
/// <para>
/// Every instance field is followed, whatever its access, through the structs it holds however
/// deep and into an inline list's items, so a handle in a private field, an auto-property, a struct
/// the inspector does not take apart or an inline list's item is freed and copied as one in a
/// public field is. The inspector's own list of fields leaves those out, and a hook built from it
/// would leave their slots taken for the life of the process and a clone sharing them.
/// </para>
/// <para>
/// A field code outside its type cannot name is reached through an accessor the runtime binds to
/// it by name, which reflects on nothing, so a game published trimmed or native keeps it. A handle
/// inside a type no code outside it can name, as a private struct declared in the component is,
/// cannot be reached, and is said as a warning (BCS010).
/// </para>
/// </remarks>
internal static class HandleHooks
{
    /// <summary>How many structs deep a handle is looked for, far past any component a person writes.</summary>
    private const int Depth = 16;

    /// <summary>The handles a component holds, each the path to it, and the first that no hook can reach.</summary>
    internal static (EquatableArray<EquatableArray<HandleStep>> Paths, string? Unreached) Of(INamedTypeSymbol type, Compilation compilation)
    {
        var found = new List<EquatableArray<HandleStep>>();
        var unreached = new List<string>();
        Walk(type, [], true, found, unreached, compilation, 0);
        return (new([.. found]), unreached.FirstOrDefault());
    }

    /// <summary>Follows each instance field of a type, adding the path to every handle it finds.</summary>
    private static void Walk(
        ITypeSymbol type,
        List<HandleStep> path,
        bool reachable,
        List<EquatableArray<HandleStep>> found,
        List<string> unreached,
        Compilation compilation,
        int depth)
    {
        if (depth > Depth) return;

        foreach (var field in type.GetMembers().OfType<IFieldSymbol>())
        {
            if (field.IsStatic || field.IsConst || field.IsFixedSizeBuffer) continue;

            var item = InlineItem(field.Type);
            var stored = BehaviorGenerator.IsStored(field.Type);
            if (!stored && !IsStruct(item ?? field.Type)) continue;

            var named = !field.IsImplicitlyDeclared && compilation.IsSymbolAccessibleWithin(field, compilation.Assembly);

            // An accessor names the type it reads from and the type it gives back, so both have to
            // be types the registration can name.
            var reached = reachable && (named || (Nameable(type, compilation) && Nameable(field.Type, compilation)));
            List<HandleStep> along = [.. path, new HandleStep(field.Name, Qualified(type), Qualified(field.Type), named, Items: item is not null)];

            if (stored)
            {
                if (reached) found.Add(new([.. along]));
                else unreached.Add(string.Join(".", along.Select(step => step.Name)));
                continue;
            }

            Walk(item ?? field.Type, along, reached, found, unreached, compilation, depth + 1);
        }
    }

    /// <summary>The item type of one of the engine's inline lists, or nothing for any other type.</summary>
    /// <remarks>An inline list keeps its items in a buffer of its own, which is gone into through its items alone.</remarks>
    private static ITypeSymbol? InlineItem(ITypeSymbol type) =>
        type is INamedTypeSymbol { Name: "InlineList4" or "InlineList8" or "InlineList16" or "InlineList32" or "InlineList64", TypeArguments.Length: 1 } named
        && named.ContainingNamespace?.ToDisplayString() == "Bevy"
            ? named.TypeArguments[0]
            : null;

    /// <summary>Whether a field's type is a struct of fields that may hold a handle.</summary>
    private static bool IsStruct(ITypeSymbol type) =>
        type.TypeKind == TypeKind.Struct
        && type.SpecialType == SpecialType.None
        && type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T;

    /// <summary>Whether code in the assembly can name a type, each type it is declared in and its type arguments.</summary>
    private static bool Nameable(ITypeSymbol type, Compilation compilation) =>
        compilation.IsSymbolAccessibleWithin(type, compilation.Assembly)
        && (type is not INamedTypeSymbol named || named.TypeArguments.All(argument => Nameable(argument, compilation)));

    private static string Qualified(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    /// <summary>
    /// Emits, into the registration's initializer, the remove and clone hooks of each component
    /// holding a handle.
    /// </summary>
    /// <returns>The accessors the hooks call, for <see cref="EmitAccessors"/>.</returns>
    internal static List<HandleStep> Emit(StringBuilder source, IReadOnlyList<BehaviorModel> models)
    {
        var accessors = new List<HandleStep>();
        // A component holding a stored list frees it as it leaves its entity, or every despawn
        // leaks the list. The value the hook is handed is read-only, so each handle is copied out
        // before it is freed.
        foreach (var model in models)
        {
            var paths = model.Handles.Items;
            if (paths.Count == 0) continue;

            source.Append("        global::Bevy.ComponentHooks.OnRemove<").Append(model.QualifiedName)
                .Append(">(static (in ").Append(model.QualifiedName).Append(" component) =>\n")
                .Append("        {\n")
                .Append("            ref var self = ref global::System.Runtime.CompilerServices.Unsafe.AsRef(in component);\n");

            foreach (var path in paths) Statement(source, path.Items, "self", clone: false, accessors);

            source.Append("        });\n");

            // And a clone gets copies of its own, or the clone and the original would share a
            // list the first of them to go frees for both.
            source.Append("        global::Bevy.ComponentHooks.OnClone<").Append(model.QualifiedName)
                .Append(">(static (ref ").Append(model.QualifiedName).Append(" component) =>\n")
                .Append("        {\n");

            foreach (var path in paths) Statement(source, path.Items, "component", clone: true, accessors);

            source.Append("        });\n");
        }

        return accessors;
    }

    /// <summary>
    /// Emits the accessors the hooks call, each bound by the runtime to the field it names, as
    /// members of the registration.
    /// </summary>
    internal static void EmitAccessors(StringBuilder source, List<HandleStep> accessors)
    {
        for (var i = 0; i < accessors.Count; i++)
        {
            var step = accessors[i];
            source.Append('\n')
                .Append("    /// <summary>The field ").Append(step.Name.Replace("<", "&lt;").Replace(">", "&gt;"))
                .Append(" of a component's handles, which code outside its type cannot name.</summary>\n")
                .Append("    [global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = \"")
                .Append(step.Name).Append("\")]\n")
                .Append("    private static extern ref ").Append(step.Type).Append(' ').Append(AccessorName(i))
                .Append("(ref ").Append(step.Owner).Append(" owner);\n");
        }
    }

    /// <summary>Frees or copies the handle at the end of one path, going into each inline list on the way.</summary>
    private static void Statement(StringBuilder source, IReadOnlyList<HandleStep> path, string root, bool clone, List<HandleStep> accessors)
    {
        var at = root;
        var indent = "            ";
        var items = 0;

        foreach (var step in path)
        {
            var reach = step.Named ? at + "." + step.Name : AccessorName(Accessor(step, accessors)) + "(ref " + at + ")";
            if (!step.Items)
            {
                at = reach;
                continue;
            }

            at = "item" + items++;
            source.Append(indent).Append("foreach (ref var ").Append(at).Append(" in ").Append(reach).Append(".AsSpan())\n");
            indent += "    ";
        }

        source.Append(indent).Append(clone
            ? "{ ref var held = ref " + at + "; held = held.Copy(); }\n"
            : "{ var held = " + at + "; held.Free(); }\n");
    }

    /// <summary>The index of the accessor for a step, gathered once however many paths go through it.</summary>
    private static int Accessor(HandleStep step, List<HandleStep> accessors)
    {
        var plain = step with { Items = false };
        var index = accessors.IndexOf(plain);
        if (index >= 0) return index;

        accessors.Add(plain);
        return accessors.Count - 1;
    }

    private static string AccessorName(int index) => "Field" + index;
}
