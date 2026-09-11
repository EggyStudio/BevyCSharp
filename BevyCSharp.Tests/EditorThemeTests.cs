using System.Numerics;
using BevyCSharp.Editor.Framework;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// A theme is a file. What it says has to come back as what it was, or a look dialled in by hand is
/// a look that changes every time the editor is opened.
/// </summary>
public sealed class EditorThemeTests
{
    [Fact]
    public void AThemeSurvivesBeingWrittenDownAndReadBack()
    {
        var dialled = EditorTheme.Modern with
        {
            Name = "Mine",
            Accent = new Vector4(0.2f, 0.7f, 0.4f, 1f),
            PanelAlpha = 0.73f,
            WindowRounding = 14f,
            FramePadding = new Vector2(9f, 6f),
            Borders = 1f,
        };

        var read = EditorTheme.Restore(dialled.Describe());

        Assert.Equal("Mine", read.Name);
        Assert.Equal(dialled.PanelAlpha, read.PanelAlpha, 3);
        Assert.Equal(dialled.WindowRounding, read.WindowRounding, 3);
        Assert.Equal(dialled.FramePadding, read.FramePadding);
        Assert.Equal(dialled.Borders, read.Borders, 3);

        // Colours go through hex, so they come back to the nearest byte rather than exactly.
        Assert.Equal(dialled.Accent.X, read.Accent.X, 2);
        Assert.Equal(dialled.Accent.Y, read.Accent.Y, 2);
        Assert.Equal(dialled.Accent.Z, read.Accent.Z, 2);
    }

    [Fact]
    public void WhatAFileDoesNotSayKeepsTheValueItHad()
    {
        // A file outlives the version that wrote it. A line naming nothing this build knows is
        // skipped, and everything it does not mention is left as it was.
        var read = EditorTheme.Restore("accent\t#FF0000\nsomething-else\t12\n", EditorTheme.Native);

        Assert.Equal(1f, read.Accent.X, 2);
        Assert.Equal(0f, read.Accent.Y, 2);
        Assert.Equal(EditorTheme.Native.WindowRounding, read.WindowRounding, 3);
        Assert.True(read.Stock);
    }

    [Fact]
    public void TheTwoThemesAreDifferentLooks()
    {
        Assert.Contains(EditorTheme.Modern, EditorTheme.All);
        Assert.Contains(EditorTheme.Native, EditorTheme.All);

        // One is the editor's own, the other is what ImGui says. The difference that matters is
        // that the stock one asks ImGui for its colours rather than being written over it.
        Assert.False(EditorTheme.Modern.Stock);
        Assert.True(EditorTheme.Native.Stock);
        Assert.Equal(0f, EditorTheme.Modern.Borders);
    }
}
