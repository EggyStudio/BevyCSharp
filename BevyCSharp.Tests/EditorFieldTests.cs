using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// How a number is written in a field somebody can edit.
/// </summary>
/// <remarks>
/// The rule is the fewest places that say what the value is, up to the three a box this narrow
/// shows. A whole number written as 0.000 is three characters of nothing in a column that is
/// already tight, and a number written to more places than fit is one whose last digits are under
/// the edge of the box. Neither the drag nor the text box rounds the value itself, so what is
/// tested here is only what is read.
/// </remarks>
public sealed class EditorFieldTests
{
    [Theory]
    [InlineData(0f, 0)]
    [InlineData(1f, 0)]
    [InlineData(-2f, 0)]
    [InlineData(62f, 0)]
    [InlineData(1.2f, 1)]
    [InlineData(0.5f, 1)]
    [InlineData(1.25f, 2)]
    [InlineData(0.85f, 2)]
    [InlineData(3.001f, 3)]
    public void ANumberIsWrittenToAsFewPlacesAsSayIt(float number, int places)
    {
        Assert.Equal(places, ComponentFields.Needed(number));
    }

    [Theory]
    [InlineData(1.2345f)]
    [InlineData(0.123456f)]
    [InlineData(-7.98765f)]
    public void ANumberWithMorePlacesThanFitIsWrittenToTheMostThereIs(float number)
    {
        Assert.Equal(ComponentFields.Places, ComponentFields.Needed(number));
    }
}
