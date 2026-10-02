using System.Text;
using Bevy.Interop;

namespace Bevy;

public sealed unsafe partial class EcsWorld
{
    /// <summary>
    /// Reads one of Bevy's components, or one field of it, as JSON.
    /// </summary>
    /// <param name="entity">The entity to read.</param>
    /// <param name="typePath">The component's full Rust type path, such as
    /// <c>bevy_light::point_light::PointLight</c>.</param>
    /// <param name="path">Bevy's reflect path from the component's root, such as
    /// <c>intensity</c> or <c>color.0.red</c>, or empty for the whole component.</param>
    /// <returns>The value as JSON, or <see langword="null"/> when the entity does not carry the
    /// component.</returns>
    /// <remarks>
    /// <para>
    /// This reaches every component Bevy reflects, which is nearly all of them, with no mirror
    /// written on this side. Bevy describes its types at runtime, and the bridge reads and writes
    /// through that description with Bevy's own serializer, so a component added by a later Bevy or
    /// by a plugin is reachable the day it exists.
    /// </para>
    /// <para>
    /// It costs a serialization per call, which suits an inspector, a save, the CLI or a script
    /// setting a light once. A system reading many entities a frame uses a mirror such as
    /// <see cref="Transform"/> through <see cref="GetRef{T}"/>, which reads the bytes in place.
    /// </para>
    /// <para>
    /// The type path is the full one because short names collide across Bevy's crates. A path that
    /// does not resolve, or a field that does not exist, throws with Bevy's own account of where it
    /// stopped, because a string path fails at runtime where a renamed field in C# would fail to
    /// compile, and the reason is the only help there is.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The type is not a reflected component, the path leads nowhere, the entity does not exist, or
    /// the value cannot be written as JSON.
    /// </exception>
    public string? GetReflected(Entity entity, string typePath, string path = "")
    {
        ArgumentException.ThrowIfNullOrEmpty(typePath);
        path ??= string.Empty;

        return ReflectedText(
            (buffer, capacity) =>
                Native.bcs_reflect_get(entity.Bits, typePath, path, buffer, capacity),
            $"Reading {Described(typePath, path)} on {entity}");
    }

    /// <summary>
    /// Writes a value given as JSON over one of Bevy's components, or one field of it.
    /// </summary>
    /// <param name="entity">The entity to write.</param>
    /// <param name="typePath">The component's full Rust type path.</param>
    /// <param name="path">Bevy's reflect path to the field, or empty for the whole component.</param>
    /// <param name="json">The value, in the form <see cref="GetReflected"/> reads it.</param>
    /// <remarks>
    /// The value is read against the field's own type before anything is written, so a malformed one
    /// leaves the component untouched and unmarked. The write goes through Bevy's change detection,
    /// so a system filtering on the component being changed sees it. An immutable component cannot
    /// be written in place and is replaced with <see cref="InsertReflected"/> instead.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The entity does not carry the component, the value does not fit the field, or the component
    /// is immutable.
    /// </exception>
    public void SetReflected(Entity entity, string typePath, string path, string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(typePath);
        ArgumentNullException.ThrowIfNull(json);

        var bytes = Encoding.UTF8.GetBytes(json);
        fixed (byte* text = bytes)
            ReflectedCheck(
                Native.bcs_reflect_set(entity.Bits, typePath, path ?? string.Empty, text, bytes.Length),
                $"Writing {Described(typePath, path)} on {entity}");
    }

    /// <summary>
    /// Which variant an enum inside one of Bevy's components holds.
    /// </summary>
    /// <returns>The variant's name, or <see langword="null"/> when the entity does not carry the
    /// component.</returns>
    /// <remarks>
    /// Asked of the bridge rather than read out of <see cref="GetReflected"/>, because the JSON is
    /// not a reliable record of it. Bevy writes an <c>Option</c> as <c>null</c> or the bare value,
    /// and an enum serialized through serde may rename its variants.
    /// </remarks>
    /// <exception cref="BevyNativeException">The path does not lead to an enum.</exception>
    public string? GetVariant(Entity entity, string typePath, string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(typePath);

        return ReflectedText(
            (buffer, capacity) => Native.bcs_reflect_variant(
                entity.Bits, typePath, path ?? string.Empty, buffer, capacity),
            $"Reading the variant of {Described(typePath, path)} on {entity}");
    }

    /// <summary>
    /// Switches an enum inside one of Bevy's components to another variant.
    /// </summary>
    /// <remarks>
    /// The variant's fields start at their defaults, since a caller choosing a variant by name has
    /// no values for them yet. A variant holding a type Bevy has no default for cannot be made this
    /// way, and is written whole through <see cref="SetReflected"/> instead.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The path does not lead to an enum, the enum has no such variant, or one of its fields has no
    /// default.
    /// </exception>
    public void SetVariant(Entity entity, string typePath, string path, string variant)
    {
        ArgumentException.ThrowIfNullOrEmpty(typePath);
        ArgumentException.ThrowIfNullOrEmpty(variant);

        ReflectedCheck(
            Native.bcs_reflect_set_variant(entity.Bits, typePath, path ?? string.Empty, variant),
            $"Choosing '{variant}' for {Described(typePath, path)} on {entity}");
    }

    /// <summary>
    /// Puts one of Bevy's components on an entity, from JSON or at its default.
    /// </summary>
    /// <param name="entity">The entity to add it to.</param>
    /// <param name="typePath">The component's full Rust type path.</param>
    /// <param name="json">The whole component as JSON, or <see langword="null"/> for its default.</param>
    /// <remarks>
    /// The default is Bevy's own, which for a component that needs a resource to make is built from
    /// the world. Bevy adds the components a component requires as it would for any insert, so a
    /// light arrives with the transform and visibility it needs. Inserting over one the entity
    /// already carries replaces it.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The type is not a reflected component, the JSON does not describe one, or no JSON was given
    /// for a type with no default.
    /// </exception>
    public void InsertReflected(Entity entity, string typePath, string? json = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(typePath);

        var bytes = json is null ? [] : Encoding.UTF8.GetBytes(json);
        fixed (byte* text = bytes)
            ReflectedCheck(
                Native.bcs_reflect_insert(entity.Bits, typePath, text, bytes.Length),
                $"Inserting {typePath} on {entity}");
    }

    /// <summary>
    /// Takes one of Bevy's components off an entity.
    /// </summary>
    /// <returns>Whether the entity carried it.</returns>
    /// <exception cref="BevyNativeException">The type is not a reflected component.</exception>
    public bool RemoveReflected(Entity entity, string typePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(typePath);

        var status = Native.bcs_reflect_remove(entity.Bits, typePath);
        if (status == NativeStatus.NotPresent) return false;

        ReflectedCheck(status, $"Removing {typePath} from {entity}");
        return true;
    }

    /// <summary>Describes every reflected component and the types its fields reach, as JSON.</summary>
    /// <remarks>
    /// <see langword="null"/> when no world is on loan, because the description is the running
    /// app's, taken once its plugins have registered what they register.
    /// </remarks>
    internal static string? DescribeReflected()
    {
        if (Native.bcs_reflect_types(null, 0) == NativeStatus.NoWorld) return null;

        return ReflectedText(
            (buffer, capacity) => Native.bcs_reflect_types(buffer, capacity),
            "Describing Bevy's reflected components");
    }

    /// <summary>Names a component, or one field of it, for a message.</summary>
    private static string Described(string typePath, string? path) =>
        path is { Length: > 0 } ? $"'{path}' of {typePath}" : typePath;

    /// <summary>
    /// Reads text from a reflected entry point, or <see langword="null"/> when the entity does not
    /// carry the component.
    /// </summary>
    /// <remarks>
    /// <see cref="Native.ReadText"/> with two differences. An absent component is the ordinary
    /// answer for an inspector asking about a selection, so it is a null rather than a throw, and a
    /// failure carries the bridge's reason, since a reflect path is checked only when it is used.
    /// </remarks>
    private static string? ReflectedText(Native.TextWriter write, string operation)
    {
        const int Probe = 256;

        byte* probe = stackalloc byte[Probe];
        var length = write(probe, Probe);
        if (length == NativeStatus.NotPresent) return null;
        ReflectedCheck(length, operation);

        if (length == 0) return string.Empty;
        if (length <= Probe) return Encoding.UTF8.GetString(probe, length);

        var buffer = new byte[length];
        fixed (byte* target = buffer)
        {
            var written = write(target, length);
            if (written == NativeStatus.NotPresent) return null;
            ReflectedCheck(written, operation);
            return Encoding.UTF8.GetString(target, Math.Min(written, length));
        }
    }

    /// <summary>Throws with the bridge's reason if <paramref name="status"/> is a failure.</summary>
    private static void ReflectedCheck(int status, string operation)
    {
        if (status >= 0) return;

        // No world means no reason was recorded, and the general description says it better.
        if (status == NativeStatus.NoWorld) NativeStatus.Throw(status, operation);

        var reason = Native.ReadText(Native.bcs_reflect_error, "reading the reflection error");
        throw new BevyNativeException(status, $"{operation} failed. {reason}");
    }
}
