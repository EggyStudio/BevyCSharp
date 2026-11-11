namespace Bevy;

/// <summary>
/// What a Feathers number input holds, Bevy's <c>NumberInputValue</c>, as
/// <see cref="Feathers.NumberOf"/> reads it.
/// </summary>
/// <param name="Kind">The type it holds, which is the type its changes are reported in.</param>
/// <param name="Value">The value, exact for a float or a double and converted for an integer.</param>
/// <param name="Whole">The value of an integer, exact, and zero for a float or a double.</param>
public readonly record struct NumberInputValue(NumberKind Kind, double Value, long Whole);
