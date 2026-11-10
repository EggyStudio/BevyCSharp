using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bevy.Tests;

/// <summary>
/// The members an assembly's extension blocks give the types they extend, read from what the
/// compiler emits for them, so a listing names each under the type a caller writes it on.
/// </summary>
/// <remarks>
/// <para>
/// A block compiles to a public type nested in the class that declares it, named by the compiler,
/// which holds each member the block declares as the extended type would hold it, every one
/// marked with the name of a marker type nested in the block, whose one method takes the extended
/// type. The declaring class holds a static method of its own for each, which carries the call and
/// which no caller names, so such a class is listed for whatever else it declares, and is left out
/// where that is nothing.
/// </para>
/// <para>
/// A block over a type parameter, as <see cref="ShaderValues"/> declares for every
/// <see cref="IShaderValues"/>, extends no type a listing can name, so it is read as none here and
/// listed as the compiler made it, its class with the methods that carry its calls.
/// </para>
/// </remarks>
internal static class ExtensionMembers
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <summary>Each block of an assembly, with the members it gives, by the type they extend.</summary>
    public static ILookup<Type, (Type Block, MemberInfo[] Members)> Of(Assembly assembly) =>
        assembly.GetTypes()
            .SelectMany(Blocks)
            .SelectMany(block => Markers(block).Select(marker => (Extended: Extended(marker), block, Members: Marked(block, marker.Name))))
            .ToLookup(found => found.Extended, found => (found.block, found.Members));

    /// <summary>Whether the type is a block of a named type or one of its markers, which the compiler names.</summary>
    public static bool Made(Type type) =>
        Named(type) || type.Name.StartsWith("<M>$", StringComparison.Ordinal) && type.DeclaringType is { } block && Named(block);

    /// <summary>Whether a method carries the call of a member a block of its class declares.</summary>
    public static bool Carries(MethodInfo method) =>
        method.DeclaringType is { } type
        && Blocks(type).SelectMany(block => block.GetMembers(Declared)).Any(member => member.Name == method.Name
            || member is PropertyInfo property && (property.GetMethod?.Name == method.Name || property.SetMethod?.Name == method.Name));

    /// <summary>Whether a class declares a block of a named type.</summary>
    public static bool Extends(Type type) => Blocks(type).Any();

    // The blocks of named types a class declares.
    private static IEnumerable<Type> Blocks(Type type) => type.GetNestedTypes(Declared).Where(Named);

    private static bool Named(Type block) =>
        block.Name.StartsWith("<G>$", StringComparison.Ordinal)
        && block.IsDefined(typeof(ExtensionAttribute))
        && Markers(block).All(marker => !Extended(marker).IsGenericParameter);

    private static IEnumerable<Type> Markers(Type block) =>
        block.GetNestedTypes(Declared).Where(marker => marker.Name.StartsWith("<M>$", StringComparison.Ordinal));

    // The type the marker's one method takes.
    private static Type Extended(Type marker) => marker.GetMethods(Declared).Single(method => method.Name == "<Extension>$").GetParameters()[0].ParameterType;

    private static MemberInfo[] Marked(Type block, string marker) =>
        [.. block.GetMembers(Declared).Where(member => member.CustomAttributes.Any(attribute =>
            attribute.AttributeType.Name == "ExtensionMarkerAttribute" && (string?)attribute.ConstructorArguments[0].Value == marker))];
}
