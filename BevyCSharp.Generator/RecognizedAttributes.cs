using System.Collections.Generic;

namespace Bevy.Generator;

/// <summary>
/// Every attribute the generators act on, by its class's name, the names their matching is written
/// against.
/// </summary>
/// <remarks>
/// <para>
/// One table rather than names written into each generator where it reads them, so the set the
/// generators accept is a list something can read. The test suite reads it and fails for an
/// attribute that no case of its compiles and runs, which is how an attribute that stopped
/// compiling, or never did, is found before a game is the first to use it.
/// </para>
/// <para>
/// The names are the classes' own, in the <c>Bevy</c> namespace but for
/// <see cref="Flags"/>, which is .NET's. A generator matching by full name puts
/// <see cref="Namespace"/> in front.
/// </para>
/// </remarks>
public static class RecognizedAttributes
{
    /// <summary>The namespace the attributes are declared in.</summary>
    public const string Namespace = "Bevy";

    // What a type is.

    /// <summary>A struct that is a component and the systems that act on it.</summary>
    public const string Behavior = "BehaviorAttribute";

    /// <summary>A type kept in a data asset file of its own.</summary>
    public const string DataAsset = "DataAssetAttribute";

    /// <summary>Which version of its shape a type's files are written at.</summary>
    public const string DataVersion = "DataVersionAttribute";

    /// <summary>A name a type or a field had before, which its files may still use.</summary>
    public const string FormerName = "FormerNameAttribute";

    /// <summary>A component a save game writes.</summary>
    public const string Persist = "PersistAttribute";

    /// <summary>An enum that is a state, and the value it starts at.</summary>
    public const string InitialState = "InitialStateAttribute";

    /// <summary>A static method that is a console command.</summary>
    public const string Command = "CommandAttribute";

    // When a method runs.

    /// <summary>Once, as the app starts.</summary>
    public const string OnStartup = "OnStartupAttribute";

    /// <summary>Every frame, at its top.</summary>
    public const string OnFirst = "OnFirstAttribute";

    /// <summary>Every frame, before the update.</summary>
    public const string OnPreUpdate = "OnPreUpdateAttribute";

    /// <summary>Every fixed step.</summary>
    public const string OnFixedUpdate = "OnFixedUpdateAttribute";

    /// <summary>Every frame.</summary>
    public const string OnUpdate = "OnUpdateAttribute";

    /// <summary>Every frame, after the update.</summary>
    public const string OnPostUpdate = "OnPostUpdateAttribute";

    /// <summary>Every frame, where drawing is prepared.</summary>
    public const string OnRender = "OnRenderAttribute";

    /// <summary>Every frame, at its end.</summary>
    public const string OnLast = "OnLastAttribute";

    /// <summary>Once, as the app ends.</summary>
    public const string OnCleanup = "OnCleanupAttribute";

    /// <summary>As a state takes a value.</summary>
    public const string OnEnter = "OnEnterAttribute";

    /// <summary>As a state leaves a value.</summary>
    public const string OnExit = "OnExitAttribute";

    /// <summary>As a state moves from one value to a particular other.</summary>
    public const string OnTransition = "OnTransitionAttribute";

    // Whether and over what it runs.

    /// <summary>Only for entities carrying a component.</summary>
    public const string With = "WithAttribute";

    /// <summary>Only for entities not carrying a component.</summary>
    public const string Without = "WithoutAttribute";

    /// <summary>Only for entities whose component changed this frame.</summary>
    public const string Changed = "ChangedAttribute";

    /// <summary>Only while a state holds a value.</summary>
    public const string InState = "InStateAttribute";

    /// <summary>Only while a condition holds.</summary>
    public const string RunIf = "RunIfAttribute";

    /// <summary>Only while a key has toggled it on.</summary>
    public const string ToggleKey = "ToggleKeyAttribute";

    /// <summary>After another system of its stage.</summary>
    public const string After = "AfterAttribute";

    /// <summary>Before another system of its stage.</summary>
    public const string Before = "BeforeAttribute";

    // How a field or a method is shown.

    /// <summary>The name a field is shown under.</summary>
    public const string Label = "LabelAttribute";

    /// <summary>What a field is, said when it is pointed at.</summary>
    public const string Tooltip = "TooltipAttribute";

    /// <summary>A heading above a field.</summary>
    public const string Header = "HeaderAttribute";

    /// <summary>What a number is measured in.</summary>
    public const string Unit = "UnitAttribute";

    /// <summary>The numbers a field may hold.</summary>
    public const string Range = "RangeAttribute";

    /// <summary>How far a number moves at a time.</summary>
    public const string Step = "StepAttribute";

    /// <summary>A field shown and not changed.</summary>
    public const string ReadOnly = "ReadOnlyAttribute";

    /// <summary>A field not shown.</summary>
    public const string Hidden = "HiddenAttribute";

    /// <summary>Room above a field.</summary>
    public const string Space = "SpaceAttribute";

    /// <summary>A line above a field.</summary>
    public const string Separator = "SeparatorAttribute";

    /// <summary>Numbers shown as a color.</summary>
    public const string Color = "ColorAttribute";

    /// <summary>A field across the whole row.</summary>
    public const string Wide = "WideAttribute";

    /// <summary>A vector's numbers on one row.</summary>
    public const string Inline = "InlineAttribute";

    /// <summary>A fold a field is put away in.</summary>
    public const string Foldout = "FoldoutAttribute";

    /// <summary>A note shown with a field.</summary>
    public const string Info = "InfoAttribute";

    /// <summary>Where a field stands among the others.</summary>
    public const string Order = "OrderAttribute";

    /// <summary>A handle chosen from the files of a kind of asset.</summary>
    public const string Asset = "AssetAttribute";

    /// <summary>A field shown only while another holds a value.</summary>
    public const string ShowIf = "ShowIfAttribute";

    /// <summary>A field hidden while another holds a value.</summary>
    public const string HideIf = "HideIfAttribute";

    /// <summary>Methods run when a field is changed in a tool.</summary>
    public const string OnValueChanged = "OnValueChangedAttribute";

    /// <summary>A method a tool offers as a button.</summary>
    public const string Button = "ButtonAttribute";

    /// <summary>An enum whose values combine, which .NET declares.</summary>
    public const string Flags = "FlagsAttribute";

    /// <summary>Every name above, in the order they are declared.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Behavior, DataAsset, DataVersion, FormerName, Persist, InitialState, Command,
        OnStartup, OnFirst, OnPreUpdate, OnFixedUpdate, OnUpdate, OnPostUpdate, OnRender, OnLast, OnCleanup, OnEnter, OnExit, OnTransition,
        With, Without, Changed, InState, RunIf, ToggleKey, After, Before,
        Label, Tooltip, Header, Unit, Range, Step, ReadOnly, Hidden, Space, Separator, Color, Wide, Inline,
        Foldout, Info, Order, Asset, ShowIf, HideIf, OnValueChanged, Button, Flags,
    ];
}
