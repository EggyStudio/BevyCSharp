using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Bevy.Tests;

// What N 2.10 reads the library's code with, finding the methods native code calls by how each is
// handed over, and reading each one's instructions from its body for a call no catch guards.
public sealed partial class NormTests
{
    /// <summary>
    /// The methods of the library and the editor that native code calls, those marked to be called
    /// from it and those made into a delegate of a type marked to be handed to it.
    /// </summary>
    /// <remarks>
    /// A function pointer the bridge is given can only point at a method marked to be called from
    /// native code, so the mark finds every callback of the bridge, ImGui's among them. A delegate
    /// handed over by the marshaller is found by its type, which none is today and which 3DEngine's
    /// test finds the same way. A binding whose own callback calls a virtual method, which a class
    /// here overrides, reaches the override from native code as well, so the overrides of what each
    /// binding's callbacks call are taken too, the bindings being the assemblies a class here
    /// derives from. None is today, ImGui's binding calling no override of ours and BepuPhysics
    /// being managed throughout, and 3DEngine's test found Assimp's this way.
    /// </remarks>
    private static List<MethodBase> HandedToNativeCode()
    {
        Assembly[] ours = [typeof(App).Assembly, Assembly.Load("BevyCSharp.Editor")];
        var handed = new HashSet<MethodBase>();
        foreach (var assembly in ours)
            foreach (var method in Methods(assembly))
            {
                if (method.IsDefined(typeof(UnmanagedCallersOnlyAttribute))) handed.Add(method);
                foreach (var target in DelegatesHandedOver(method))
                    if (target.Module.Assembly == assembly) handed.Add(target);
            }

        var bindings = ours.SelectMany(assembly => assembly.GetTypes())
            .SelectMany(Bases)
            .Select(type => type.Assembly)
            .Where(assembly => !ours.Contains(assembly) && assembly != typeof(object).Assembly)
            .ToHashSet();
        var reached = bindings.SelectMany(VirtualsReachedFromNativeCode).ToHashSet();
        foreach (var method in ours.SelectMany(Methods))
            if (method is MethodInfo { IsVirtual: true } info && reached.Contains(info.GetBaseDefinition()))
                handed.Add(method);

        return [.. handed.OrderBy(Name, StringComparer.Ordinal)];
    }

    /// <summary>
    /// The virtual methods a binding's methods that native code calls reach, through the binding's
    /// own calls, which are followed where others' are not.
    /// </summary>
    private static IEnumerable<MethodBase> VirtualsReachedFromNativeCode(Assembly binding)
    {
        var pending = new Stack<MethodBase>(Methods(binding)
            .SelectMany(method => DelegatesHandedOver(method).Concat(method.IsDefined(typeof(UnmanagedCallersOnlyAttribute)) ? [method] : []))
            .Where(method => method.Module.Assembly == binding));
        var seen = new HashSet<MethodBase>();
        while (pending.TryPop(out var method))
        {
            if (!seen.Add(method)) continue;
            foreach (var (_, code, operand) in Instructions(method))
                if ((code == OpCodes.Call || code == OpCodes.Callvirt) && Resolve(method, operand) is { } called && called.Module.Assembly == binding)
                {
                    if (called.IsVirtual) yield return called is MethodInfo info ? info.GetBaseDefinition() : called;
                    pending.Push(called);
                }
        }
    }

    /// <summary>A type's bases, from its own up.</summary>
    private static IEnumerable<Type> Bases(Type type)
    {
        for (var at = type.BaseType; at is not null; at = at.BaseType) yield return at;
    }

    /// <summary>The methods a method makes into a delegate of a type marked to be handed to native code.</summary>
    private static IEnumerable<MethodBase> DelegatesHandedOver(MethodBase method)
    {
        MethodBase? pointed = null;
        foreach (var (_, code, operand) in Instructions(method))
        {
            if (code == OpCodes.Ldftn || code == OpCodes.Ldvirtftn)
                pointed = Resolve(method, operand);
            else if (code == OpCodes.Newobj && pointed is not null
                     && Resolve(method, operand)?.DeclaringType?.IsDefined(typeof(UnmanagedFunctionPointerAttribute)) == true)
                yield return pointed;
            else if (code != OpCodes.Dup && code != OpCodes.Ldarg_0 && code != OpCodes.Ldnull)
                pointed = null;
        }
    }

    /// <summary>
    /// The first call, allocation or throw of a method that no catch of every exception covers, or
    /// none where there is none.
    /// </summary>
    /// <remarks>
    /// A catch's own body counts as covered, since what it does to answer native code, and that it
    /// cannot throw in turn, is read by review.
    /// </remarks>
    private static string? UnguardedCall(MethodBase method)
    {
        var body = method.GetMethodBody();
        if (body is null) return null;
        var guarded = body.ExceptionHandlingClauses
            .Where(clause => clause.Flags == ExceptionHandlingClauseOptions.Clause && (clause.CatchType == typeof(Exception) || clause.CatchType == typeof(object)))
            .SelectMany(clause => new[] { (clause.TryOffset, clause.TryOffset + clause.TryLength), (clause.HandlerOffset, clause.HandlerOffset + clause.HandlerLength) })
            .ToList();
        foreach (var (offset, code, operand) in Instructions(method))
        {
            if (!Throwing.Contains(code) || guarded.Any(range => offset >= range.Item1 && offset < range.Item2)) continue;
            return code == OpCodes.Throw ? "throw" : Resolve(method, operand) is { } called ? Name(called) : code.Name;
        }

        return null;
    }

    private static readonly HashSet<OpCode> Throwing =
        [OpCodes.Call, OpCodes.Callvirt, OpCodes.Calli, OpCodes.Newobj, OpCodes.Newarr, OpCodes.Throw, OpCodes.Castclass, OpCodes.Unbox, OpCodes.Unbox_Any];

    private static IEnumerable<MethodBase> Methods(Assembly assembly)
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException error)
        {
            types = [.. error.Types.OfType<Type>()];
        }

        return types.SelectMany(type => type.GetMethods(all).Cast<MethodBase>().Concat(type.GetConstructors(all)));
    }

    private static MethodBase? Resolve(MethodBase within, int token)
    {
        try
        {
            return within.Module.ResolveMethod(token,
                within.DeclaringType is { IsGenericType: true } type ? type.GetGenericArguments() : null,
                within.IsGenericMethod ? within.GetGenericArguments() : null);
        }
        catch (Exception error) when (error is ArgumentException or BadImageFormatException or TypeLoadException or FileNotFoundException)
        {
            return null;
        }
    }

    private static string Name(MethodBase method) => $"{method.DeclaringType?.FullName}.{method.Name}";

    /// <summary>A method's instructions, each with where it starts and its operand where that is a token.</summary>
    private static IEnumerable<(int Offset, OpCode Code, int Operand)> Instructions(MethodBase method)
    {
        byte[]? il;
        try
        {
            il = method.GetMethodBody()?.GetILAsByteArray();
        }
        catch (Exception error) when (error is InvalidOperationException or BadImageFormatException)
        {
            il = null;
        }

        if (il is null) yield break;
        for (var at = 0; at < il.Length;)
        {
            var offset = at;
            var code = il[at] == 0xFE ? TwoByteCodes[il[at + 1]] : OneByteCodes[il[at]];
            at += code.Size;
            var operand = 0;
            switch (code.OperandType)
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar:
                    at += 1;
                    break;
                case OperandType.InlineVar:
                    at += 2;
                    break;
                case OperandType.InlineI8 or OperandType.InlineR:
                    at += 8;
                    break;
                case OperandType.InlineSwitch:
                    at += 4 + 4 * BitConverter.ToInt32(il, at);
                    break;
                default:
                    operand = BitConverter.ToInt32(il, at);
                    at += 4;
                    break;
            }

            yield return (offset, code, operand);
        }
    }

    private static readonly OpCode[] OneByteCodes = Codes(twoBytes: false);
    private static readonly OpCode[] TwoByteCodes = Codes(twoBytes: true);

    /// <summary>Every opcode of one byte or of two, by its last byte.</summary>
    private static OpCode[] Codes(bool twoBytes)
    {
        var codes = new OpCode[256];
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            if (field.GetValue(null) is OpCode code && (code.Size == 2) == twoBytes)
                codes[(ushort)code.Value & 0xFF] = code;
        return codes;
    }
}
