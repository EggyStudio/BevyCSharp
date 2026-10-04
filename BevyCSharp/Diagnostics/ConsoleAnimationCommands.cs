namespace Bevy;

/// <summary>
/// A model's animation clips, listed and played from the console, the command line and the
/// editor's console alike.
/// </summary>
/// <remarks>
/// The same <see cref="Animation"/> a game calls, on the entity a model's scene was spawned under,
/// which the editor's world panel shows by the model's name. A model that has not arrived yet says
/// so rather than failing, since asking again a moment later is the answer.
/// </remarks>
internal static class ConsoleAnimationCommands
{
    /// <summary>Lists a model's clips.</summary>
    [Command("anim.clips", "Lists a model's animation clips: anim.clips <name|#index>")]
    internal static string Clips(string which)
    {
        var world = ConsoleHost.Ecs;
        if (ConsoleWorldCommands.Find(world, which) is not { } scene) return ConsoleWorldCommands.Missing(which);

        return Asked(() => Animation.TryClips(scene, out var clips)
            ? clips.Count == 0 ? $"{which} has no clips" : string.Join("\n", clips)
            : $"{which} has not arrived yet; ask again in a moment");
    }

    /// <summary>Plays one of a model's clips, once or over and over.</summary>
    [Command("anim.play", "Plays a model's clip: anim.play <name|#index> <clip> [loop]")]
    internal static string Play(string line)
    {
        // One line rather than three arguments, so the last can be left out.
        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2) return Usage();

        var world = ConsoleHost.Ecs;
        if (ConsoleWorldCommands.Find(world, words[0]) is not { } scene) return ConsoleWorldCommands.Missing(words[0]);

        var looping = words.Length > 2 && words[^1] == "loop";
        var clip = string.Join(' ', looping ? words[1..^1] : words[1..]);

        return Asked(() => Animation.Play(scene, clip, new AnimationSettings { Repeat = looping, Blend = 0.2f })
            ? $"playing {clip} on {words[0]}{(looping ? " over and over" : string.Empty)}"
            : $"{words[0]} has not arrived yet; ask again in a moment");
    }

    /// <summary>Stops what a model is playing.</summary>
    [Command("anim.stop", "Stops a model's animation: anim.stop <name|#index>")]
    internal static string Stop(string which)
    {
        var world = ConsoleHost.Ecs;
        if (ConsoleWorldCommands.Find(world, which) is not { } scene) return ConsoleWorldCommands.Missing(which);

        return Asked(() =>
        {
            Animation.Stop(scene);
            return $"stopped {which}";
        });
    }

    /// <summary>Runs a request, turning what refuses it into the console's answer.</summary>
    private static string Asked(Func<string> request)
    {
        try
        {
            return request();
        }
        catch (Exception error) when (error is ArgumentException or Interop.BevyNativeException)
        {
            // Without the parameter's name .NET puts after an argument's message, which says
            // nothing to someone typing at a console.
            var said = error is ArgumentException { ParamName: { } name }
                ? error.Message.Replace($" (Parameter '{name}')", string.Empty, StringComparison.Ordinal)
                : error.Message;

            ConsoleHost.Fail("ANIMATION_REFUSED", said);
            return said;
        }
    }

    private static string Usage()
    {
        ConsoleHost.Fail("BAD_ARGUMENTS", "anim.play takes an entity and a clip, as in anim.play <name|#index> <clip> [loop]");
        return "anim.play <name|#index> <clip> [loop]";
    }
}
