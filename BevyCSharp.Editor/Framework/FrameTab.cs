using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The images the scene camera's shaders keep, as a tab along the bottom, where one can be picked
/// and watched. Beside them, how long each render pass takes.
/// </summary>
/// <remarks>
/// <para>
/// A screen-space technique is a chain of images, and when its result is wrong the question is
/// which link broke. This lists every name a shader on the camera can read an image by (the images
/// the camera owns, their history and mip levels, and the prepass's depth, normals and motion) and
/// shows the chosen one as it is this frame, through <see cref="Shaders.Watch"/>.
/// </para>
/// <para>
/// The names the camera does not draw are dimmed, from <see cref="Shaders.DrawnViewImageNames"/>,
/// which asks the renderer rather than guessing from the settings. Hovering one says what turns it
/// on.
/// </para>
/// <para>
/// Values are shown times a scale plus an offset, because the numbers in these images are rarely
/// between zero and one. A distance runs to hundreds, and a motion vector is a hundredth. The scale
/// and offset are applied when an edit is finished rather than on every drag, since each change
/// makes a new watch.
/// </para>
/// <para>
/// The timings are the other half of tuning a chain. The picture shows which link is wrong, and the
/// list shows which link is slow, slowest first, with GPU time where the adapter measures it.
/// </para>
/// </remarks>
public static class FrameTab
{
    /// <summary>How large the watched picture is drawn at most, in pixels.</summary>
    private const uint Width = 480;

    private const uint Height = 270;

    private static Entity _camera;
    private static string? _watching;
    private static AssetHandle _picture;
    private static float _scale = 1f;
    private static float _offset;

    /// <summary>Draws it.</summary>
    public static void Draw()
    {
        if (!App.HasRenderer)
        {
            ImGui.TextDisabled("This bridge draws nothing, so there is nothing to watch");
            return;
        }

        var camera = EditorSelection.Camera;

        if (camera != _camera)
        {
            Stop();
            _camera = camera;
        }

        if (camera.IsNone)
        {
            ImGui.TextDisabled("No camera");
            return;
        }

        var names = Shaders.ViewImageNames(camera).Concat(Shaders.EngineViewImageNames).Distinct().ToList();
        var drawn = Shaders.DrawnViewImageNames(camera).ToHashSet();
        var room = ImGui.GetContentRegionAvail();

        if (EditorSurface.Region("##frameNames", new Vector2(MathF.Min(220f, room.X * 0.3f), 0f)))
        {
            // Every name is listed, so what a camera could have is in front of whoever is looking,
            // and the ones it does not draw are dimmed with what turns them on. Picking one of those
            // would only show an empty picture and leave a warning in the log.
            foreach (var name in names)
            {
                var has = drawn.Contains(name);

                // In the faint color the editor's hints use, at full strength, since ImGui's own
                // disabled look only takes a little off the text and reads as a list of choices.
                if (!has)
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.DisabledAlpha, 1f);
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
                }

                if (ImGui.Selectable(name, _watching == name, has ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled))
                {
                    Watch(camera, name == _watching ? null : name);
                }

                if (!has)
                {
                    ImGui.PopStyleColor();
                    ImGui.PopStyleVar();
                }

                if (!has && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    ImGui.SetTooltip(Missing(name));
                }
            }
        }

        EditorSurface.EndRegion();

        ImGui.SameLine();

        if (EditorSurface.Region("##frameTimings", new Vector2(MathF.Min(300f, room.X * 0.3f), 0f)))
        {
            Timings();
        }

        EditorSurface.EndRegion();

        ImGui.SameLine();

        if (EditorSurface.Region("##framePicture", new Vector2(0f, 0f)))
        {
            if (_watching is null)
            {
                ImGui.TextDisabled("Pick an image to watch it");
            }
            else
            {
                Settings(camera);
                Picture();
            }
        }

        EditorSurface.EndRegion();
    }

    /// <summary>How long each render pass took, slowest first.</summary>
    private static void Timings()
    {
        var timings = Render.Timings();

        if (timings.Count == 0)
        {
            ImGui.TextDisabled("No timings, because the app was made without Config.GpuTimings");
            return;
        }

        if (!ImGui.BeginTable("##timings", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            return;
        }

        ImGui.TableSetupColumn("pass", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("ms", ImGuiTableColumnFlags.WidthFixed, 60f);

        foreach (var timing in timings.OrderByDescending(timing => timing.GpuMilliseconds ?? timing.CpuMilliseconds ?? 0))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(timing.Name);
            ImGui.TableNextColumn();

            // GPU time where there is one, since a pass costs the frame that, and the CPU's dimmed
            // where the adapter could not say.
            if (timing.GpuMilliseconds is { } gpu) ImGui.Text($"{gpu:0.000}");
            else if (timing.CpuMilliseconds is { } cpu) ImGui.TextDisabled($"{cpu:0.000}");
        }

        ImGui.EndTable();
    }

    /// <summary>The scale and the offset, made a new watch once an edit is finished.</summary>
    private static void Settings(Entity camera)
    {
        ImGui.SetNextItemWidth(140f);
        ImGui.DragFloat("scale", ref _scale, 0.01f);
        var changed = ImGui.IsItemDeactivatedAfterEdit();

        ImGui.SameLine();
        ImGui.SetNextItemWidth(140f);
        ImGui.DragFloat("offset", ref _offset, 0.01f);
        changed |= ImGui.IsItemDeactivatedAfterEdit();

        if (changed) Watch(camera, _watching);
    }

    /// <summary>The picture, as large as the region allows at its own shape.</summary>
    private static void Picture()
    {
        var texture = ImGuiTextures.Of(_picture);

        if (texture == 0)
        {
            ImGui.TextDisabled("Waiting for the first frame");
            return;
        }

        var room = ImGui.GetContentRegionAvail();
        var fit = MathF.Min(room.X / Width, room.Y / Height);
        var size = new Vector2(Width, Height) * MathF.Max(0.1f, MathF.Min(1f, fit));

        ImGui.Image((IntPtr)texture, size);

        if (_watching is { } name && !Shaders.DrawnViewImageNames(_camera).Contains(name))
        {
            ImGui.TextDisabled($"The camera stopped drawing {name}, so this is the last of it. {Missing(name)}");
        }
    }

    /// <summary>Why a camera has no image by this name, and what gives it one.</summary>
    /// <remarks>
    /// The engine's names each come from one setting, so the sentence can name it. A camera image
    /// of the game's own is missing because nothing has asked for it yet or its first frame has not
    /// been drawn.
    /// </remarks>
    private static string Missing(string name) => name switch
    {
        "depth" or "normals" or "motion" =>
            $"The camera draws no {name} of its own. Shaders.SetPrepass(camera, ...) with {name} on draws it, "
            + "once a pixel (PostSettings.Msaa = 1), since a multisampled one cannot be shown.",
        "ambient_occlusion" => "Bevy's ambient occlusion is off. Render.SetAmbientOcclusion turns it on.",
        "gbuffer" => "The camera draws forward. Shaders.SetPrepass(camera, ..., deferred: true) draws the G-buffer.",
        "depth_previous" or "gbuffer_previous" =>
            "The camera keeps no last frame. Shaders.SetPrepass(camera, ..., previous: true) keeps it, "
            + "with depth or the G-buffer asked for as well.",
        "depth_pyramid" => "The camera builds no pyramid. Shaders.SetPrepass(camera, ..., pyramid: true) builds it.",
        _ => "The camera has not drawn this image yet.",
    };

    /// <summary>Watches a name on the camera, or with null stops watching.</summary>
    private static void Watch(Entity camera, string? name)
    {
        Stop();

        if (name is null) return;

        _watching = name;
        _picture = Shaders.Watch(camera, name, Width, Height, _scale, _offset);
    }

    private static void Stop()
    {
        if (_watching is not null && !_camera.IsNone)
        {
            Shaders.Unwatch(_camera, _watching);
        }

        // The image goes with the watch, since a new watch makes a new one.
        if (_picture.IsValid) AssetServer.Release(_picture);

        _watching = null;
        _picture = AssetHandle.None;
    }
}
