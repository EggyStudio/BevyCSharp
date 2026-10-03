using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers reading the running game's answers into what the Play tab draws of its world.
/// </summary>
/// <remarks>
/// The answers are the console's text, which a person reads too, so their shape is pinned here
/// where a change to it would otherwise show only as an empty pane.
/// </remarks>
public sealed class EditorRemoteTests
{
    [Fact]
    public void AListOfEntitiesKeepsTheNamedOnesWithWhatTheyCarry()
    {
        var entities = EditorRemote.ReadEntities("#4 Cube [Transform, Mesh3d]\n#9 Sun [Transform, DirectionalLight]\nand 17 unnamed");

        Assert.Equal(
            [new RemoteEntity("#4", "Cube", "Transform, Mesh3d"), new RemoteEntity("#9", "Sun", "Transform, DirectionalLight")],
            entities);

        // A name with a space and brackets of its own keeps them.
        Assert.Equal("Left [door]", Assert.Single(EditorRemote.ReadEntities("#12 Left [door] [Transform]")).Name);
    }

    [Fact]
    public void AnEntitysFieldsAreReadByComponentAndField()
    {
        var fields = EditorRemote.ReadFields("#4 Cube\nTransform.translation = 0, 1, 0\nVisibility\nSpin.Speed = 2.5");

        Assert.Equal(
            [new RemoteField("Transform.translation", "0, 1, 0"), new RemoteField("Spin.Speed", "2.5")],
            fields);
    }
}
