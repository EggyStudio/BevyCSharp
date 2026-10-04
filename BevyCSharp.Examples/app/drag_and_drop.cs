using Bevy;

namespace BevyCSharp.Examples.Application;

// Shows how to handle files dragged and dropped on the window, each hover, drop and cancellation
// printed as it arrives, in a window that draws nothing.
internal static class DragAndDrop
{
    public static void Build(App app) => app.Update(ctx =>
    {
        foreach (var hovered in ctx.Read<FileHovered>()) Console.WriteLine(hovered);
        foreach (var dropped in ctx.Read<FileDropped>()) Console.WriteLine(dropped);
        foreach (var canceled in ctx.Read<FileHoverCanceled>()) Console.WriteLine(canceled);
    }, "drag_and_drop.FileDragAndDrop");
}
