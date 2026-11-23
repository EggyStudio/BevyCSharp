using Bevy;
using Diagnostics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers <c>interface.scale</c>, native and so the display's own scale where nothing sets it, a
/// share kept by its text, and one outside its range refused.
/// </summary>
[Collection("engine")]
public sealed class InterfaceSettingsTests : IDisposable
{
    private readonly Setting _scale = InterfaceSettings.All.Single();

    public void Dispose() => _scale.TrySet("native");

    [Fact]
    public void TheInterfaceScaleIsTheDisplaysUnlessAShareIsSet()
    {
        Assert.Same(_scale, Registry.Process.FindSetting("interface.scale"));
        Assert.Equal(("native", "native"), (_scale.DefaultText, _scale.Text));
        Assert.Equal(["native", "75%", "100%", "125%", "150%", "175%", "200%"], _scale.Choices);
        Assert.Equal(2f, InterfaceSettings.Scale(2f));

        Assert.True(_scale.TrySet("150").Done);
        Assert.Equal(("150%", 1.5f), (_scale.Text, InterfaceSettings.Scale(2f)));
        Assert.False(_scale.TrySet("30%").Done);
        Assert.False(_scale.TrySet("big").Done);
        Assert.Equal("150%", _scale.Text);

        Assert.True(_scale.TrySet("Native").Done);
        Assert.Equal(("native", 1.25f), (_scale.Text, InterfaceSettings.Scale(1.25f)));
    }
}
