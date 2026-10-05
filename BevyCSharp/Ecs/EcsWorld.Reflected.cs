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
    /// so a system filtering on the component being changed sees it. An immutable component, as
    /// the hierarchy's <c>ChildOf</c> or a slider's value is, cannot be written in place, so it is
    /// copied, the copy written, and the copy inserted over it, which runs its hooks and observers
    /// as Bevy's own insert does: writing a <c>ChildOf</c> moves the child under the new parent.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The entity does not carry the component, or the value does not fit the field.
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
    /// Reads an asset handle inside one of Bevy's components, such as the image a material or a
    /// sprite draws with.
    /// </summary>
    /// <returns>The asset, or <see langword="null"/> when the entity does not carry the
    /// component.</returns>
    /// <remarks>
    /// A handle has no JSON form, because what it holds is a reference to an asset rather than a
    /// value, so it is read here rather than through <see cref="GetReflected"/>. The handle comes
    /// back as the same kind of <see cref="AssetHandle"/> <see cref="AssetServer.Load"/> returns,
    /// and reading one the program already holds returns that one, so reading a field every frame
    /// costs nothing to keep. A handle the program never held is kept from then on, until
    /// <see cref="AssetServer.Release"/> lets it go.
    /// </remarks>
    /// <exception cref="BevyNativeException">The path does not lead to a handle.</exception>
    public AssetHandle? GetReflectedAsset(Entity entity, string typePath, string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(typePath);

        var key = Native.bcs_reflect_get_asset(entity.Bits, typePath, path ?? string.Empty);
        if (key == NativeStatus.NotPresent) return null;

        ReflectedCheck(key, $"Reading the asset at {Described(typePath, path)} on {entity}");
        return new AssetHandle(key);
    }

    /// <summary>
    /// Points an asset handle inside one of Bevy's components at another asset.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The asset has to be of the kind the field holds, so a mesh offered to an image is refused
    /// rather than drawn as garbage.
    /// </para>
    /// <para>
    /// A field holding an optional handle, as a fog volume's density texture does, is set to hold
    /// the asset whether it held one or none.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The path does not lead to a handle, the asset is of another kind, or the handle names no
    /// asset.
    /// </exception>
    public void SetReflectedAsset(Entity entity, string typePath, string path, AssetHandle asset)
    {
        ArgumentException.ThrowIfNullOrEmpty(typePath);

        ReflectedCheck(
            Native.bcs_reflect_set_asset(entity.Bits, typePath, path ?? string.Empty, asset.Key),
            $"Writing the asset at {Described(typePath, path)} on {entity}");
    }

    /// <summary>
    /// Reads a color inside one of Bevy's components as linear RGBA.
    /// </summary>
    /// <returns>The color, or <see langword="null"/> when the entity does not carry the
    /// component.</returns>
    /// <remarks>
    /// Bevy's <c>Color</c> holds a color in any of ten spaces, and its JSON is the space it happens
    /// to be in. This asks Bevy to convert it, so the answer is the same <see cref="Color"/>
    /// whichever space the code that made it chose. A <c>LinearRgba</c> or <c>Srgba</c> field reads
    /// the same way.
    /// </remarks>
    /// <exception cref="BevyNativeException">The path does not lead to a color.</exception>
    public Color? GetReflectedColor(Entity entity, string typePath, string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(typePath);

        var parts = stackalloc float[4];
        var status = Native.bcs_reflect_get_color(entity.Bits, typePath, path ?? string.Empty, parts);
        if (status == NativeStatus.NotPresent) return null;

        ReflectedCheck(status, $"Reading the color at {Described(typePath, path)} on {entity}");
        return new Color(parts[0], parts[1], parts[2], parts[3]);
    }

    /// <summary>Reads a float inside one of Bevy's components as a number, or nothing when it is absent.</summary>
    /// <remarks>What a typed wrapper reads a float through, so the number never passes through text.</remarks>
    internal double? GetReflectedFloat(Entity entity, string typePath, string path)
    {
        double value;
        var status = Native.bcs_reflect_get_float(entity.Bits, typePath, path ?? string.Empty, &value);
        if (status == NativeStatus.NotPresent) return null;

        ReflectedCheck(status, $"Reading the number at {Described(typePath, path)} on {entity}");
        return value;
    }

    /// <summary>Writes a float inside one of Bevy's components at the width it holds.</summary>
    internal void SetReflectedFloat(Entity entity, string typePath, string path, double value) =>
        ReflectedCheck(
            Native.bcs_reflect_set_float(entity.Bits, typePath, path ?? string.Empty, value),
            $"Writing {value} to {Described(typePath, path)} on {entity}");

    /// <summary>
    /// Reads a whole number or a flag inside one of Bevy's components as a number, a flag as one
    /// or zero, or nothing when it is absent.
    /// </summary>
    internal long? GetReflectedInteger(Entity entity, string typePath, string path)
    {
        long value;
        var status = Native.bcs_reflect_get_integer(entity.Bits, typePath, path ?? string.Empty, &value);
        if (status == NativeStatus.NotPresent) return null;

        ReflectedCheck(status, $"Reading the number at {Described(typePath, path)} on {entity}");
        return value;
    }

    /// <summary>
    /// Writes a whole number or a flag inside one of Bevy's components at the type it holds,
    /// refused where the number does not fit it.
    /// </summary>
    internal void SetReflectedInteger(Entity entity, string typePath, string path, long value) =>
        ReflectedCheck(
            Native.bcs_reflect_set_integer(entity.Bits, typePath, path ?? string.Empty, value),
            $"Writing {value} to {Described(typePath, path)} on {entity}");

    /// <summary>
    /// Writes a color inside one of Bevy's components from linear RGBA.
    /// </summary>
    /// <remarks>
    /// A <c>Color</c> keeps the space it was held in, so a color given in sRGB stays sRGB, holding
    /// the color written.
    /// </remarks>
    /// <exception cref="BevyNativeException">The path does not lead to a color.</exception>
    public void SetReflectedColor(Entity entity, string typePath, string path, Color color)
    {
        ArgumentException.ThrowIfNullOrEmpty(typePath);

        ReflectedCheck(
            Native.bcs_reflect_set_color(
                entity.Bits, typePath, path ?? string.Empty, color.R, color.G, color.B, color.A),
            $"Writing the color at {Described(typePath, path)} on {entity}");
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

    /// <summary>
    /// A typed wrapper over one of Bevy's components on an entity, or <see langword="null"/> when
    /// the entity does not carry it.
    /// </summary>
    /// <typeparam name="T">The wrapper, such as <c>Bevy.Reflected.PointLightRef</c>.</typeparam>
    /// <remarks>
    /// The wrapper reads and writes the component in place through Bevy's reflection, with a
    /// property per field, so <c>ctx.Ecs.Get&lt;PointLightRef&gt;(lamp)?.Intensity</c> is the light's
    /// intensity as it is now.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// This build of the bridge has no such component, as a headless build has no light.
    /// </exception>
    public T? Get<T>(Entity entity) where T : struct, IReflectedComponent<T>
    {
        var id = NativeComponents.Resolve(T.TypePath, 0);
        return HasById(entity, id) ? T.Create(this, entity) : null;
    }

    /// <summary>A typed wrapper over one of Bevy's components an entity is known to carry.</summary>
    /// <typeparam name="T">The wrapper, such as <c>Bevy.Reflected.TextColorRef</c>.</typeparam>
    /// <remarks>
    /// <see cref="Get{T}"/> without the question, for a component the program put there itself, so
    /// <c>ctx.Ecs.Wrap&lt;TextColorRef&gt;(label).Value = Color.White</c> is one line. Nothing is
    /// checked until a property is used, and a property used on an entity without the component
    /// throws, as every wrapper's does once its component is gone.
    /// </remarks>
    public T Wrap<T>(Entity entity) where T : struct, IReflectedComponent<T> => T.Create(this, entity);


    /// <summary>
    /// Puts one of Bevy's components on an entity, from JSON or at its default, and returns a typed
    /// wrapper over it.
    /// </summary>
    /// <typeparam name="T">The wrapper, such as <c>Bevy.Reflected.PointLightRef</c>.</typeparam>
    /// <param name="entity">The entity to add it to.</param>
    /// <param name="json">The whole component as JSON, or <see langword="null"/> for its default.</param>
    /// <exception cref="BevyNativeException">As <see cref="InsertReflected"/>.</exception>
    public T Insert<T>(Entity entity, string? json = null) where T : struct, IReflectedComponent<T>
    {
        InsertReflected(entity, T.TypePath, json);
        return T.Create(this, entity);
    }

    /// <summary>
    /// The entity holding one of Bevy's resources, or <see langword="null"/> when the world has none
    /// of it.
    /// </summary>
    /// <param name="typePath">The resource's full type path, such as <c>bevy_ui::UiScale</c>.</param>
    /// <remarks>
    /// <para>
    /// In this Bevy a resource is a component on an entity of its own, so once that entity is
    /// found, every call here that reads or writes a component reads or writes the resource, and
    /// <see cref="Resource{T}"/> gives its wrapper. A resource is found afresh each time rather
    /// than kept, since a plugin may take one away and insert it again on another entity.
    /// </para>
    /// <para>
    /// A type that is a component and no resource is refused rather than answered with
    /// <see langword="null"/>, since asking for it this way is a mistake absence would hide.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">The type is not one of Bevy's reflected resources.</exception>
    public Entity? ResourceEntity(string typePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(typePath);

        ulong bits;
        var status = Native.bcs_reflect_resource_entity(typePath, &bits);
        if (status == NativeStatus.NotPresent) return null;

        ReflectedCheck(status, $"Finding the resource {typePath}");
        return new Entity(bits);
    }

    /// <summary>
    /// A typed wrapper over one of Bevy's resources, or <see langword="null"/> when the world has
    /// none of it.
    /// </summary>
    /// <typeparam name="T">The wrapper, such as <c>Bevy.Reflected.UiScaleRef</c>.</typeparam>
    /// <remarks>
    /// The wrappers Bevy's components have serve its resources too, since a resource is a component
    /// on an entity of its own, so <c>if (ctx.Ecs.Resource&lt;UiScaleRef&gt;() is { } scale)
    /// scale.Value = 2f;</c> doubles the interface's scale. See <see cref="ResourceEntity"/>.
    /// </remarks>
    /// <exception cref="BevyNativeException">The type is not one of Bevy's reflected resources.</exception>
    public T? Resource<T>() where T : struct, IReflectedComponent<T> =>
        ResourceEntity(T.TypePath) is { } entity ? T.Create(this, entity) : null;

    /// <summary>
    /// Puts one of Bevy's resources in the world, from JSON or at its default, replacing the one it
    /// has, and returns a typed wrapper over it.
    /// </summary>
    /// <typeparam name="T">The wrapper, such as <c>Bevy.Reflected.ClearColorRef</c>.</typeparam>
    /// <param name="json">The whole resource as JSON, or <see langword="null"/> for its default.</param>
    /// <remarks>
    /// A resource the world lacks is inserted on a new entity, which Bevy makes the resource's own.
    /// One the world has is replaced where it is, since Bevy refuses a second entity holding the
    /// same resource, and removes the newer with a warning.
    /// </remarks>
    /// <exception cref="BevyNativeException">As <see cref="InsertReflected"/>, or the type is no resource.</exception>
    public T InsertResource<T>(string? json = null) where T : struct, IReflectedComponent<T>
    {
        var entity = ResourceEntity(T.TypePath) ?? Spawn();
        return Insert<T>(entity, json);
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
