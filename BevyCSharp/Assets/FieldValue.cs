namespace Bevy;

/// <summary>What a field an animation curve moves holds, numbered as the bridge's curve takes it.</summary>
internal enum FieldValue
{
    Number = 1,
    Vec2 = 2,
    Vec3 = 3,
    Vec4 = 4,
    Rotation = 5,
}
