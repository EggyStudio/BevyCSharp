using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The modeling window as a tab of the editor, on the edit mesh the selected entity's mesh was built
/// from, the one a program draws with <see cref="Modeling.DrawWindow"/> drawn in the editor's own
/// strip.
/// </summary>
/// <remarks>
/// An entity is chosen by the editor's picking as any other, and once one drawn with a model's mesh
/// is selected and this tab is open, a click on the scene picks the model's faces and a drag extrudes
/// them, with the editor's own picking off so the selection stays on the entity. A model the
/// console made and showed (<c>model.show</c>) is one, its parts each drawn with a mesh of its.
/// </remarks>
public static class ModelTab
{
    private static ulong _heldOn;

    /// <summary>Whether the tab held the scene's pointer this frame or the last, so a click picks a face rather than an entity.</summary>
    public static bool HoldsPointer => _heldOn != 0 && EditorShell.Frame - _heldOn <= 1;

    /// <summary>Draws the tab.</summary>
    public static void Draw()
    {
        if (EditorShell.Context is not { } ctx) return;

        var entity = EditorSelection.Current;
        var mesh = entity.IsNone ? null : Modeling.Source(Render.MeshOf(ctx.Ecs, entity));
        if (mesh is null)
        {
            ImGui.TextWrapped("Select an entity drawn with a model's mesh, such as one the console's model.box and model.show made, and it is shaped here.");
            return;
        }

        _heldOn = EditorShell.Frame;
        var placed = ctx.Ecs.TryGet<GlobalTransform>(entity, out var world) ? world.ToTransform() : Transform.Identity;
        Modeling.DrawPanel(ctx, mesh, EditorSelection.Camera, placed);
    }
}
