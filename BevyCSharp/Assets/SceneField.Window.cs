using System.Numerics;
using ImGuiNET;

namespace Bevy;

// The field's window, what gi.state says, its settings, and a slice across a cascade painted by a
// compute shader into an image ImGui shows, as 3DEngine's global illumination window shows its
// parts.
internal static partial class SceneField
{
    /// <summary>The pixels a cell takes along each side of the slice's image.</summary>
    private const int SlicePixels = 4;

    private static ShaderInstance _slice;
    private static AssetHandle _sliceImage = AssetHandle.None;
    // The cascade, the axis and the cell the window slices across, the cell the eye's while
    // `_sliceThroughEye` holds.
    private static int _sliceCascade, _sliceAxis = 1, _sliceCell = SceneFieldPlan.Resolution / 2;
    private static bool _sliceThroughEye = true;

    /// <summary>
    /// Paints a slice across a cascade into the window's image, a cell four pixels on a side, and
    /// answers the image, or none where no field is built. Only valid inside a system.
    /// </summary>
    /// <param name="cascade">The cascade.</param>
    /// <param name="axis">The axis the slice is across, 0 for X, 1 for Y and 2 for Z.</param>
    /// <param name="cell">The slice's cell along that axis.</param>
    internal static AssetHandle Slice(int cascade, int axis, int cell)
    {
        if (Plan is null || _built is not { } built || !_slice.IsValid || _slice.Program.State != ShaderProgramState.Ready)
            return AssetHandle.None;

        const uint side = SceneFieldPlan.Resolution * SlicePixels;
        if (_sliceImage == AssetHandle.None) _sliceImage = Shaders.CreateImage(side, side);

        ReadOnlySpan<uint> choice = [(uint)cascade, (uint)axis, (uint)Math.Clamp(cell, 0, SceneFieldPlan.Resolution - 1), SlicePixels];
        _slice.SetTexture("field", built.Field).SetSampler("field_sampler", SamplerSettings.Clamped).SetBuffer("info", built.Info)
            .SetTexture("slice", _sliceImage).SetNumbers("choice", 4, choice);
        Shaders.Dispatch(_slice, side / 8, side / 8);
        return _sliceImage;
    }

    /// <summary>Draws the field's window, which <see cref="Render.DrawSceneFieldWindow"/> asks for.</summary>
    internal static void DrawWindow()
    {
        if (!ImGuiRuntime.IsRunning) return;

        // Not focused as it appears, so a key that opened it stays the program's, and at the top left
        // at a height a window of 720 pixels holds with the slice in it.
        ImGui.SetNextWindowPos(new Vector2(12f, 12f), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(300f, 690f), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Scene field##bcs-field", ImGuiWindowFlags.NoFocusOnAppearing)) DrawPanel();
        ImGui.End();
    }

    private static void DrawPanel()
    {
        // The settings, which take effect on the next frame, as the calls they stand for do.
        var (cascades, cell, budget) = Asked;
        var changed = ImGui.SliderInt("Cascades", ref cascades, 0, 8);
        changed |= ImGui.DragFloat("Finest cell", ref cell, 0.01f, 0.05f, 4f, "%.2f m");
        changed |= ImGui.SliderInt("Built a frame", ref budget, 1, 8);
        if (changed) Render.SetSceneField(cascades, cell, budget);

        string[] places = ["Past the shadow map", "Everywhere", "Off"];
        var shadows = (int)Shadows;
        if (ImGui.Combo("Contact shadows", ref shadows, places, places.Length)) Render.SetSceneFieldShadows((FieldShadows)shadows);

        if (Plan is { } plan) DrawSlice(plan);

        // What gi.state says, under the slice, which a window of its height keeps in view.
        ImGui.Separator();
        ImGui.PushTextWrapPos(0f);
        ImGui.TextUnformatted(ConsoleFieldCommands.State());
        ImGui.PopTextWrapPos();
    }

    private static void DrawSlice(SceneFieldPlan plan)
    {
        ImGui.Separator();

        _sliceCascade = Math.Min(_sliceCascade, plan.Cascades - 1);
        ImGui.SliderInt("Cascade", ref _sliceCascade, 0, plan.Cascades - 1);
        ImGui.RadioButton("Across X", ref _sliceAxis, 0);
        ImGui.SameLine();
        ImGui.RadioButton("Across Y", ref _sliceAxis, 1);
        ImGui.SameLine();
        ImGui.RadioButton("Across Z", ref _sliceAxis, 2);
        ImGui.Checkbox("Through the eye", ref _sliceThroughEye);

        // The eye's cell along the axis, which the slice follows while it is asked to.
        if (_sliceThroughEye && plan.BuiltOrigin(_sliceCascade) is { } origin)
        {
            var at = (plan.Eye - origin) / plan.CellOf(_sliceCascade);
            _sliceCell = Math.Clamp((int)MathF.Floor(_sliceAxis switch { 0 => at.X, 1 => at.Y, _ => at.Z }), 0, SceneFieldPlan.Resolution - 1);
        }

        if (ImGui.SliderInt("Cell", ref _sliceCell, 0, SceneFieldPlan.Resolution - 1)) _sliceThroughEye = false;

        var image = Slice(_sliceCascade, _sliceAxis, _sliceCell);
        if (image == AssetHandle.None) return;

        var side = MathF.Min(ImGui.GetContentRegionAvail().X, SceneFieldPlan.Resolution * SlicePixels);
        ImGui.Image((IntPtr)ImGuiTextures.Of(image), new Vector2(side, side));
        ImGui.PushTextWrapPos(0f);
        ImGui.TextUnformatted(_sliceAxis == 1
            ? "Seen from above, X to the right and Z down. Red is inside a mesh, black its surface, gray outside in bands a cell apart."
            : "Seen from the side, up being up. Red is inside a mesh, black its surface, gray outside in bands a cell apart.");
        ImGui.PopTextWrapPos();
    }
}
