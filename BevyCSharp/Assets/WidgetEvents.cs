using Bevy.Interop;

namespace Bevy;

/// <summary>The kinds of thing Bevy's widgets report, by the numbers the bridge reports them under.</summary>
internal static class WidgetEvents
{
    internal const int Activate = 0;
    internal const int Number = 1;
    internal const int Flag = 2;
    internal const int Choice = 3;
    internal const int Menu = 4;
    internal const int Tab = 5;
    internal const int TextEdit = 6;
    internal const int Double = 7;
    internal const int Int = 8;
    internal const int Long = 9;
    internal const int VirtualKey = 10;
    internal const int Vector2 = 11;
    internal const int ColorValue = 12;
    internal const int Close = 13;

    /// <summary>The number the bridge reports a <see cref="ValueChange{T}"/> of <typeparamref name="T"/> under.</summary>
    /// <exception cref="NotSupportedException">No widget of Bevy's reports a value of that type.</exception>
    internal static int KindOfValue<T>() =>
        typeof(T) == typeof(float) ? Number
        : typeof(T) == typeof(bool) ? Flag
        : typeof(T) == typeof(Entity) ? Choice
        : typeof(T) == typeof(Entity?) ? Tab
        : typeof(T) == typeof(double) ? Double
        : typeof(T) == typeof(int) ? Int
        : typeof(T) == typeof(long) ? Long
        : typeof(T) == typeof(Vec2) ? Vector2
        : typeof(T) == typeof(Color) ? ColorValue
        : throw new NotSupportedException(
            $"No widget of Bevy's reports a ValueChange of {typeof(T).Name}. A slider reports a float, a "
            + "checkbox or a radio button a bool, a radio group the Entity of the button chosen, a "
            + "tab list the Entity? of the tab chosen, a number input the float, double, int or long it "
            + "holds, a color plane a Vec2 and a color input a Color.");

    /// <summary>Runs the observers of what a widget reported, as the bridge reported it.</summary>
    internal static void Trigger(ObserverRegistry registry, in NativeWidgetEvent reported)
    {
        var entity = new Entity(reported.Entity);
        var final = reported.IsFinal != 0;

        switch (reported.Kind)
        {
            case Activate: registry.Trigger(new Bevy.Activate(entity)); break;
            case Number: registry.Trigger(new ValueChange<float>(entity, reported.Value, final)); break;
            case Flag: registry.Trigger(new ValueChange<bool>(entity, reported.Flag != 0, final)); break;
            case Choice: registry.Trigger(new ValueChange<Entity>(entity, new Entity(reported.Other), final)); break;
            case Menu: registry.Trigger(new MenuEvent(entity, (MenuAction)reported.Action, (NavAction)reported.Navigation)); break;
            case Tab: registry.Trigger(new ValueChange<Entity?>(entity, reported.Flag != 0 ? new Entity(reported.Other) : null, final)); break;
            case TextEdit: registry.Trigger(new TextEditChange(entity)); break;
            case Double: registry.Trigger(new ValueChange<double>(entity, BitConverter.Int64BitsToDouble((long)reported.Other), final)); break;
            case Int: registry.Trigger(new ValueChange<int>(entity, (int)(long)reported.Other, final)); break;
            case Long: registry.Trigger(new ValueChange<long>(entity, (long)reported.Other, final)); break;
            case VirtualKey:
                var (row, column) = ((int)(reported.Other >> 32), (int)(uint)reported.Other);
                registry.Trigger(new VirtualKeyPressed(entity, Feathers.KeyAt(entity, row, column), row, column));
                break;
            case Vector2: registry.Trigger(new ValueChange<Vec2>(entity, new Vec2(reported.VectorX, reported.VectorY), final)); break;
            case ColorValue: registry.Trigger(new ValueChange<Color>(entity, new Color(reported.VectorX, reported.VectorY, reported.VectorZ, reported.VectorW), final)); break;
            case Close: registry.Trigger(new RequestClose(entity)); break;
        }
    }
}
