using System.Collections.Immutable;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Xunit;

namespace Bevy.Tests;

// What N 1.6 reads the library's layers with while the library is one project, the project each of
// its files stands in, its area's folder, and what each names of another, from the built library
// and its portable PDB, as the projects' references will hold it once the split makes them.
public sealed partial class NormTests
{
    /// <summary>
    /// The library's areas in their layers, the lowest first, each in the lowest layer what it
    /// names allows, so the list says what the graph of names does.
    /// </summary>
    private static readonly string[][] Layers =
    [
        ["Native"],
        ["Core"],
        ["Assets", "Audio", "Gizmos", "ImGui", "Cli"],
        ["Rendering"],
        ["Render2d", "Animation", "GlobalIllumination", "Scenes"],
        ["Ui", "Physics", "Sky"],
        ["Modeling", "Diagnostics"],
        ["Vegetation"],
        ["Levels", "BevyCSharp"],
    ];

    /// <summary>
    /// The project a file of the library stands in, its top folder's under BevyCSharp/, an area's,
    /// or BevyCSharp's for a file at the top, which composes the rest.
    /// </summary>
    private static string ProjectOf(string file) => file.Split('/') is { Length: > 2 } parts ? parts[1] : "BevyCSharp";

    /// <summary>
    /// Each project the built library's code names of another, with the first place that names it,
    /// a type placed by the files the PDB says it was compiled from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A method names what its instructions, its locals and its signature hold, and a type what its
    /// base, its interfaces, its fields and its attributes do, each read as the compiler resolved
    /// it, so a member of an extension block counts for the class that declares the block, and a
    /// name that is only a word in a comment or a string counts for nothing. A method stands in the
    /// files its sequence points name, and a type in its methods' files and those the PDB records for
    /// a type with no method to carry them, an enum or an interface, or its declaring type's where it
    /// has none. A file the generators write stands in no project and names nothing here, since each
    /// project runs the generators over its own files once it is one.
    /// </para>
    /// <para>
    /// A type standing in files of two projects is said too, since a partial type cannot span
    /// assemblies.
    /// </para>
    /// </remarks>
    private static Dictionary<(string From, string To), string> LibraryEdges(Func<string, string?> projectOf, List<string> found)
    {
        var library = typeof(App).Assembly.Location;
        var symbols = Path.ChangeExtension(library, ".pdb");
        Assert.True(File.Exists(symbols), $"N 1.6: the library's PDB is not beside it at {symbols}, and the layers are read from it");

        using var image = new PEReader(File.OpenRead(library));
        var metadata = image.GetMetadataReader();
        using var debugProvider = MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(symbols));
        var debug = debugProvider.GetMetadataReader();

        // A document of the PDB as a path from the checkout, wherever the library was built, a
        // checkout's folder or a deterministic build's mapped root.
        string? FileOf(DocumentHandle document)
        {
            var name = debug.GetString(debug.GetDocument(document).Name).Replace('\\', '/');
            var at = name.LastIndexOf("/BevyCSharp/", StringComparison.Ordinal);
            if (at < 0) return null;
            var file = "BevyCSharp/" + name[(at + "/BevyCSharp/".Length)..];
            return projectOf(file) is null ? null : file;
        }

        var typeFiles = new Dictionary<TypeDefinitionHandle, HashSet<string>>();
        var methodFiles = new Dictionary<MethodDefinitionHandle, HashSet<string>>();
        foreach (var method in metadata.MethodDefinitions)
        {
            var points = debug.GetMethodDebugInformation(method.ToDebugInformationHandle());
            var files = new HashSet<string>(StringComparer.Ordinal);
            if (!points.SequencePointsBlob.IsNil)
                foreach (var point in points.GetSequencePoints())
                    if (FileOf(point.Document) is { } file) files.Add(file);
            methodFiles[method] = files;
            var type = metadata.GetMethodDefinition(method).GetDeclaringType();
            if (!typeFiles.TryGetValue(type, out var known)) typeFiles[type] = known = new(StringComparer.Ordinal);
            known.UnionWith(files);
        }

        Assert.True(methodFiles.Values.Any(files => files.Count > 0), $"N 1.6: the PDB at {symbols} names none of the library's files, so no edge could be read");

        // The documents the compiler records for a type with no method whose sequence points name
        // them, by the kind's GUID the portable PDB format gives it.
        var typeDocuments = new Guid("932E74BC-DBA9-4478-8D46-0F32A7BAB3D3");
        foreach (var handle in debug.CustomDebugInformation)
        {
            var information = debug.GetCustomDebugInformation(handle);
            if (information.Parent.Kind != HandleKind.TypeDefinition || debug.GetGuid(information.Kind) != typeDocuments) continue;
            var type = (TypeDefinitionHandle)information.Parent;
            if (!typeFiles.TryGetValue(type, out var known)) typeFiles[type] = known = new(StringComparer.Ordinal);
            var blob = debug.GetBlobReader(information.Value);
            while (blob.RemainingBytes > 0)
                if (FileOf(MetadataTokens.DocumentHandle(blob.ReadCompressedInteger())) is { } file) known.Add(file);
        }

        HashSet<string> FilesOf(TypeDefinitionHandle type)
        {
            for (var at = type; !at.IsNil; at = metadata.GetTypeDefinition(at).GetDeclaringType())
                if (typeFiles.TryGetValue(at, out var files) && files.Count > 0) return files;
            return [];
        }

        var edges = new Dictionary<(string From, string To), string>();
        var named = new NamedTypes();

        void Name(IEnumerable<string> from, IEnumerable<TypeDefinitionHandle> types)
        {
            foreach (var type in types)
                foreach (var target in FilesOf(type))
                    foreach (var source in from)
                    {
                        var (a, b) = (projectOf(source)!, projectOf(target)!);
                        if (a != b) edges.TryAdd((a, b), $"{source} naming {Named(type)} of {target}");
                    }
        }

        string Named(TypeDefinitionHandle type)
        {
            var definition = metadata.GetTypeDefinition(type);
            var outer = definition.GetDeclaringType();
            return (outer.IsNil ? "" : Named(outer) + ".") + metadata.GetString(definition.Name);
        }

        // The types of the library a token names, a type, a member's declaring type and those its
        // generic arguments hold.
        List<TypeDefinitionHandle> Types(EntityHandle handle)
        {
            named.Found.Clear();
            switch (handle.Kind)
            {
                case HandleKind.TypeDefinition:
                    named.Found.Add((TypeDefinitionHandle)handle);
                    break;
                case HandleKind.TypeSpecification:
                    metadata.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(named, null);
                    break;
                case HandleKind.MethodDefinition:
                    named.Found.Add(metadata.GetMethodDefinition((MethodDefinitionHandle)handle).GetDeclaringType());
                    break;
                case HandleKind.FieldDefinition:
                    named.Found.Add(metadata.GetFieldDefinition((FieldDefinitionHandle)handle).GetDeclaringType());
                    break;
                case HandleKind.MemberReference:
                    var parent = metadata.GetMemberReference((MemberReferenceHandle)handle).Parent;
                    if (parent.Kind is HandleKind.TypeSpecification or HandleKind.TypeDefinition or HandleKind.MethodDefinition)
                        return Types((EntityHandle)parent);
                    break;
                case HandleKind.MethodSpecification:
                    var specification = metadata.GetMethodSpecification((MethodSpecificationHandle)handle);
                    var method = Types(specification.Method);
                    named.Found.UnionWith(method);
                    specification.DecodeSignature(named, null);
                    break;
            }

            return [.. named.Found];
        }

        foreach (var type in metadata.TypeDefinitions)
        {
            var files = FilesOf(type);
            if (files.Select(file => projectOf(file)).Distinct().Count() > 1)
                found.Add($"{Named(type)} stands in files of more than one project, {string.Join(", ", files.Order(StringComparer.Ordinal))}");
            if (files.Count == 0) continue;

            var definition = metadata.GetTypeDefinition(type);
            if (!definition.BaseType.IsNil) Name(files, Types(definition.BaseType));
            foreach (var face in definition.GetInterfaceImplementations()) Name(files, Types(metadata.GetInterfaceImplementation(face).Interface));
            foreach (var attribute in definition.GetCustomAttributes()) Name(files, Types(metadata.GetCustomAttribute(attribute).Constructor));
            foreach (var field in definition.GetFields())
            {
                named.Found.Clear();
                metadata.GetFieldDefinition(field).DecodeSignature(named, null);
                Name(files, [.. named.Found]);
            }

            foreach (var handle in definition.GetMethods())
            {
                var method = metadata.GetMethodDefinition(handle);
                var from = methodFiles[handle].Count > 0 ? methodFiles[handle] : files;
                named.Found.Clear();
                method.DecodeSignature(named, null);
                Name(from, [.. named.Found]);
                foreach (var attribute in method.GetCustomAttributes()) Name(from, Types(metadata.GetCustomAttribute(attribute).Constructor));
                if (method.RelativeVirtualAddress == 0) continue;

                var body = image.GetMethodBody(method.RelativeVirtualAddress);
                if (!body.LocalSignature.IsNil)
                {
                    named.Found.Clear();
                    metadata.GetStandaloneSignature(body.LocalSignature).DecodeLocalSignature(named, null);
                    Name(from, [.. named.Found]);
                }

                // A token of a standalone signature, which calli takes, names no type here.
                foreach (var (_, code, operand) in Instructions(body.GetILBytes()!))
                    if (code.OperandType is OperandType.InlineMethod or OperandType.InlineField or OperandType.InlineType or OperandType.InlineTok && operand >> 24 != 0x11)
                        Name(from, Types(MetadataTokens.EntityHandle(operand)));
            }
        }

        return edges;
    }

    /// <summary>Collects every type of the library a signature names.</summary>
    private sealed class NamedTypes : ISignatureTypeProvider<object?, object?>
    {
        public HashSet<TypeDefinitionHandle> Found { get; } = [];

        public object? GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        {
            Found.Add(handle);
            return null;
        }

        public object? GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
            reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

        public object? GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => null;
        public object? GetSZArrayType(object? elementType) => null;
        public object? GetArrayType(object? elementType, ArrayShape shape) => null;
        public object? GetByReferenceType(object? elementType) => null;
        public object? GetPointerType(object? elementType) => null;
        public object? GetPinnedType(object? elementType) => null;
        public object? GetGenericInstantiation(object? genericType, ImmutableArray<object?> typeArguments) => null;
        public object? GetGenericMethodParameter(object? genericContext, int index) => null;
        public object? GetGenericTypeParameter(object? genericContext, int index) => null;
        public object? GetFunctionPointerType(MethodSignature<object?> signature) => null;
        public object? GetModifiedType(object? modifier, object? unmodifiedType, bool isRequired) => null;
        public object? GetPrimitiveType(PrimitiveTypeCode typeCode) => null;
    }
}
