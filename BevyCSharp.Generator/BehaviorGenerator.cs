using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bevy.Generator;

/// <summary>
/// Turns <c>[Behavior]</c> structs into Bevy systems.
/// </summary>
/// <remarks>
/// <para>
/// For each behavior the generator emits a runner class with one system method per stage, and
/// a <c>Register(App)</c> that adds them to the schedule. It then emits a single per-assembly
/// entry point tagged <c>[GeneratedBehaviorRegistration]</c>, which
/// <c>BehaviorsPlugin</c> finds reflectively at startup. That is the whole reason a consuming
/// project needs no registration code: the wiring is generated and then discovered.
/// </para>
/// <para>
/// The emitted code stays deliberately thin. Iteration, filtering and parallel partitioning
/// all live in <c>BehaviorRunners</c> in the runtime library, so the generated file is a
/// handful of readable, verifiable lines that a consumer can step through, and so fixing the
/// iteration strategy does not mean regenerating anyone's code.
/// </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class BehaviorGenerator : IIncrementalGenerator
{
    private const string AttributeNamespace = "Bevy";
    private const string BehaviorAttribute = "Bevy.BehaviorAttribute";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var behaviors = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                BehaviorAttribute,
                predicate: static (node, _) => node is StructDeclarationSyntax,
                transform: static (ctx, token) => Extract(ctx, token))
            .Where(static result => result is not null)
            .Collect();

        context.RegisterSourceOutput(behaviors, static (spc, results) =>
        {
            var models = new List<BehaviorModel>();

            foreach (var result in results)
            {
                if (result is null) continue;

                foreach (var diagnostic in result.Diagnostics)
                    spc.ReportDiagnostic(diagnostic);

                if (result.Model is { } model && model.Methods.Count > 0)
                    models.Add(model);
            }

            foreach (var model in models)
                spc.AddSource($"{model.UniqueKey}.Behavior.g.cs", BehaviorEmitter.Emit(model));

            if (models.Count > 0)
                spc.AddSource("BehaviorRegistration.g.cs", BehaviorEmitter.EmitRegistration(models));
        });
    }

    /// <summary>The model for one struct, plus anything wrong with it.</summary>
    private sealed record ExtractResult(BehaviorModel? Model, ImmutableArray<Diagnostic> Diagnostics);

    /// <summary>Reads one <c>[Behavior]</c> struct into a model, validating as it goes.</summary>
    private static ExtractResult? Extract(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        if (context.TargetSymbol is not INamedTypeSymbol type) return null;
        if (context.TargetNode is not StructDeclarationSyntax declaration) return null;

        token.ThrowIfCancellationRequested();

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        var isPartial = declaration.Modifiers.Any(m => m.ValueText == "partial");
        if (!isPartial)
        {
            diagnostics.Add(Diagnostic.Create(
                BehaviorDiagnostics.NotPartial, declaration.Identifier.GetLocation(), type.Name));
        }

        var methods = new List<StageMethod>();
        var hasInstanceMethod = false;

        foreach (var member in type.GetMembers().OfType<IMethodSymbol>())
        {
            token.ThrowIfCancellationRequested();

            var stages = GetStages(member);
            var edge = GetStateEdge(member);

            if (stages.Count == 0 && edge is null) continue;

            // A transition is not a stage, so asking for both says two different things about
            // when the method runs.
            if (stages.Count > 1 || (stages.Count > 0 && edge is not null))
            {
                diagnostics.Add(Diagnostic.Create(
                    BehaviorDiagnostics.MultipleStages, member.Locations.FirstOrDefault(), member.Name));
                continue;
            }

            if (!HasSystemSignature(member))
            {
                diagnostics.Add(Diagnostic.Create(
                    BehaviorDiagnostics.BadSignature, member.Locations.FirstOrDefault(), member.Name));
                continue;
            }

            if (!member.IsStatic) hasInstanceMethod = true;

            methods.Add(new StageMethod
            {
                Stage = stages.Count > 0 ? stages[0] : BehaviorStage.Startup,
                Edge = edge,
                Name = member.Name,
                IsStatic = member.IsStatic,
                Filters = GetFilters(member, diagnostics),
                Condition = GetCondition(member, type, diagnostics),
                Toggle = GetToggle(member),
                InState = GetInState(member),
            });
        }

        if (methods.Count == 0)
        {
            diagnostics.Add(Diagnostic.Create(
                BehaviorDiagnostics.NoStageMethods, declaration.Identifier.GetLocation(), type.Name));
        }

        // Only instance methods force the struct into Bevy's storage, so only they require it
        // to be blittable. A behavior with static methods alone is a system holder.
        if (hasInstanceMethod && !type.IsUnmanagedType)
        {
            diagnostics.Add(Diagnostic.Create(
                BehaviorDiagnostics.BehaviorNotUnmanaged,
                declaration.Identifier.GetLocation(),
                type.Name));
        }

        var model = new BehaviorModel
        {
            Namespace = type.ContainingNamespace.IsGlobalNamespace
                ? null
                : type.ContainingNamespace.ToDisplayString(),
            Name = type.Name,
            QualifiedName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Methods = methods,
        };

        return new ExtractResult(model, diagnostics.ToImmutable());
    }

    /// <summary>The stages named by a method's attributes.</summary>
    private static List<BehaviorStage> GetStages(IMethodSymbol method)
    {
        var stages = new List<BehaviorStage>();

        foreach (var attribute in method.GetAttributes())
        {
            var stage = attribute.AttributeClass?.ToDisplayString() switch
            {
                $"{AttributeNamespace}.OnStartupAttribute" => BehaviorStage.Startup,
                $"{AttributeNamespace}.OnFirstAttribute" => BehaviorStage.First,
                $"{AttributeNamespace}.OnPreUpdateAttribute" => BehaviorStage.PreUpdate,
                $"{AttributeNamespace}.OnFixedUpdateAttribute" => BehaviorStage.FixedUpdate,
                $"{AttributeNamespace}.OnUpdateAttribute" => BehaviorStage.Update,
                $"{AttributeNamespace}.OnPostUpdateAttribute" => BehaviorStage.PostUpdate,
                $"{AttributeNamespace}.OnRenderAttribute" => BehaviorStage.Render,
                $"{AttributeNamespace}.OnLastAttribute" => BehaviorStage.Last,
                $"{AttributeNamespace}.OnCleanupAttribute" => BehaviorStage.Cleanup,
                _ => (BehaviorStage?)null,
            };

            if (stage is { } value) stages.Add(value);
        }

        return stages;
    }

    /// <summary>True when the method looks like <c>void M(BehaviorContext ctx)</c>.</summary>
    private static bool HasSystemSignature(IMethodSymbol method) =>
        method.ReturnsVoid
        && method.Parameters.Length == 1
        && method.Parameters[0].Type.ToDisplayString() == $"{AttributeNamespace}.BehaviorContext";

    /// <summary>Reads the With/Without/Changed filters off a method.</summary>
    private static BehaviorFilters GetFilters(
        IMethodSymbol method,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var with = new List<string>();
        var without = new List<string>();
        var changed = new List<string>();

        foreach (var attribute in method.GetAttributes())
        {
            var bucket = attribute.AttributeClass?.ToDisplayString() switch
            {
                $"{AttributeNamespace}.WithAttribute" => with,
                $"{AttributeNamespace}.WithoutAttribute" => without,
                $"{AttributeNamespace}.ChangedAttribute" => changed,
                _ => null,
            };

            if (bucket is null || attribute.ConstructorArguments.Length == 0) continue;

            foreach (var value in attribute.ConstructorArguments[0].Values)
            {
                if (value.Value is not ITypeSymbol typeSymbol) continue;

                if (!typeSymbol.IsUnmanagedType)
                {
                    diagnostics.Add(Diagnostic.Create(
                        BehaviorDiagnostics.FilterNotUnmanaged,
                        method.Locations.FirstOrDefault(),
                        typeSymbol.Name));
                    continue;
                }

                bucket.Add(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }

        return with.Count == 0 && without.Count == 0 && changed.Count == 0
            ? BehaviorFilters.None
            : new BehaviorFilters(with, without, changed);
    }

    /// <summary>Reads an <c>[OnEnter]</c> or <c>[OnExit]</c> attribute.</summary>
    private static StateEdgeInfo? GetStateEdge(IMethodSymbol method)
    {
        foreach (var attribute in method.GetAttributes())
        {
            var entering = attribute.AttributeClass?.ToDisplayString() switch
            {
                $"{AttributeNamespace}.OnEnterAttribute" => true,
                $"{AttributeNamespace}.OnExitAttribute" => false,
                _ => (bool?)null,
            };

            if (entering is not { } edge || attribute.ConstructorArguments.Length == 0) continue;

            var argument = attribute.ConstructorArguments[0];
            if (argument.Type is not INamedTypeSymbol { EnumUnderlyingType: not null } enumType)
                continue;
            if (argument.Value is null) continue;

            return new StateEdgeInfo(
                enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                argument.Value.ToString(),
                edge);
        }

        return null;
    }

    /// <summary>Reads an <c>[InState]</c> attribute into the enum type and value it names.</summary>
    /// <remarks>
    /// The argument is typed as <c>object</c> so any enum can be passed, which means the enum
    /// type arrives on the constant rather than on the parameter. Emitting a cast back to that
    /// type is what lets the condition infer its type parameter.
    /// </remarks>
    private static InStateInfo? GetInState(IMethodSymbol method)
    {
        foreach (var attribute in method.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != $"{AttributeNamespace}.InStateAttribute")
                continue;

            if (attribute.ConstructorArguments.Length == 0) continue;

            var argument = attribute.ConstructorArguments[0];
            if (argument.Type is not INamedTypeSymbol { EnumUnderlyingType: not null } enumType)
                continue;
            if (argument.Value is null) continue;

            return new InStateInfo(
                enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                argument.Value.ToString());
        }

        return null;
    }

    /// <summary>Resolves a <c>[RunIf]</c> attribute against the behavior's own members.</summary>
    private static ConditionInfo? GetCondition(
        IMethodSymbol method,
        INamedTypeSymbol type,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        string? name = null;

        foreach (var attribute in method.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != $"{AttributeNamespace}.RunIfAttribute")
                continue;
            if (attribute.ConstructorArguments.Length == 0) continue;
            if (attribute.ConstructorArguments[0].Value is string value) name = value;
            break;
        }

        if (name is null) return null;

        foreach (var member in type.GetMembers(name))
        {
            if (!member.IsStatic) continue;

            switch (member)
            {
                case IMethodSymbol: return new ConditionInfo(name, ConditionKind.Method);
                case IPropertySymbol: return new ConditionInfo(name, ConditionKind.Property);
                case IFieldSymbol: return new ConditionInfo(name, ConditionKind.Field);
            }
        }

        diagnostics.Add(Diagnostic.Create(
            BehaviorDiagnostics.UnknownCondition,
            method.Locations.FirstOrDefault(),
            name,
            method.Name));

        return null;
    }

    /// <summary>Reads a <c>[ToggleKey]</c> attribute off a method.</summary>
    /// <remarks>
    /// The modifier argument is a flags enum, so a combination such as
    /// <c>KeyModifier.Ctrl | KeyModifier.Shift</c> arrives as a single folded constant. There is
    /// nothing extra to do here to support several modifiers at once.
    /// </remarks>
    private static ToggleKeyInfo? GetToggle(IMethodSymbol method)
    {
        foreach (var attribute in method.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != $"{AttributeNamespace}.ToggleKeyAttribute")
                continue;

            var arguments = attribute.ConstructorArguments;
            var key = arguments.Length > 0 && arguments[0].Value is int k ? k : 0;
            var modifiers = arguments.Length > 1 && arguments[1].Value is int m ? m : 0;

            var defaultEnabled = true;
            foreach (var named in attribute.NamedArguments)
                if (named.Key == "DefaultEnabled" && named.Value.Value is bool enabled)
                    defaultEnabled = enabled;

            return new ToggleKeyInfo(key, modifiers, defaultEnabled);
        }

        return null;
    }
}
