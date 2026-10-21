namespace Bevy;

// The material slots, each a name with a look, added, renamed and taken away, and given a color, a
// texture and the maps of the engine's material, every change a line of the recipe as a change of
// the shape is, so a saved model is drawn as it was left.
public sealed partial class EditMesh
{
    /// <summary>What a slot is drawn with, nothing given where it is the prototype material of its name.</summary>
    /// <param name="slot">The slot's name, one of <see cref="Slots"/>.</param>
    /// <exception cref="ArgumentException">The mesh has no slot of that name.</exception>
    public SlotLook LookOf(string slot) => _looks[Existing(slot)];

    /// <summary>The look of a slot by its index in <see cref="Slots"/>.</summary>
    internal SlotLook LookAt(int slot) => _looks[slot];

    /// <summary>Adds a slot no face is in yet, drawn with the prototype material of its name.</summary>
    /// <param name="slot">The slot's name, one word.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <exception cref="ArgumentException">The mesh has a slot of that name.</exception>
    public EditMesh SlotAdd(string slot)
    {
        OneWord(slot, nameof(slot));
        if (_slots.Contains(slot)) throw new ArgumentException($"There is a slot called {slot}.", nameof(slot));

        SlotIndex(slot);
        Changed();
        return Did($"slot add {slot}");
    }

    /// <summary>Gives a slot another name, its faces and its look kept.</summary>
    /// <param name="slot">The slot's name.</param>
    /// <param name="name">Its new name, one word no other slot has.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>
    /// A slot whose look gives nothing is drawn with the prototype material of its name, so a slot
    /// renamed from <c>default</c> to <c>wall</c> is drawn as a wall's prototype after.
    /// </remarks>
    public EditMesh SlotRename(string slot, string name)
    {
        var index = Existing(slot);
        OneWord(name, nameof(name));
        if (_slots.Contains(name)) throw new ArgumentException($"There is a slot called {name}.", nameof(name));

        _slots[index] = name;
        Changed();
        return Did($"slot rename {slot} {name}");
    }

    /// <summary>Takes a slot away, its faces given the first slot.</summary>
    /// <param name="slot">The slot's name, any but the first.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <remarks>The slots after it move down by one, so <see cref="Modeling.Meshes"/> and <see cref="Modeling.Materials"/> answer one fewer.</remarks>
    /// <exception cref="InvalidOperationException">The slot is the first, which every new face takes.</exception>
    public EditMesh SlotRemove(string slot)
    {
        var index = Existing(slot);
        if (index == 0) throw new InvalidOperationException($"{slot} is the first slot, which every new face takes, and stays.");

        foreach (var face in _faces)
        {
            if (face.Slot == index) face.Slot = 0;
            else if (face.Slot > index) face.Slot--;
        }

        _slots.RemoveAt(index);
        _looks.RemoveAt(index);
        Changed();
        return Did($"slot remove {slot}");
    }

    /// <summary>Gives a slot a color, which tints its texture, or its grid where it has no texture.</summary>
    /// <param name="slot">The slot's name, made where the mesh has none of it.</param>
    /// <param name="red">Red, in sRGB from zero to one, as a color picker shows it.</param>
    /// <param name="green">Green, in sRGB from zero to one.</param>
    /// <param name="blue">Blue, in sRGB from zero to one.</param>
    /// <param name="alpha">How opaque, from zero to one, below one drawing the faces blended.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh SlotColor(string slot, float red, float green, float blue, float alpha = 1f)
    {
        foreach (var part in (ReadOnlySpan<float>)[red, green, blue, alpha]) Unit(part, "A color's parts run from zero to one.");
        var line = $"slot color {slot} {F(red)} {F(green)} {F(blue)}{(alpha == 1f ? "" : " " + F(alpha))}";
        return Look(slot, look => look with { Color = new Vec4(red, green, blue, alpha) }, line);
    }

    /// <summary>Gives a slot a picture its faces show, in the grid's place, or takes it away.</summary>
    /// <param name="slot">The slot's name, made where the mesh has none of it.</param>
    /// <param name="path">The picture, relative to the asset root, or nothing for the grid again.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh SlotTexture(string slot, string? path) =>
        Look(slot, look => look with { Texture = MapPath(path) }, $"slot texture {slot} {path ?? "none"}");

    /// <summary>Gives a slot a normal map, or takes it away.</summary>
    /// <param name="slot">The slot's name, made where the mesh has none of it.</param>
    /// <param name="path">The map, relative to the asset root, or nothing.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh SlotNormalMap(string slot, string? path) =>
        Look(slot, look => look with { NormalMap = MapPath(path) }, $"slot normal-map {slot} {path ?? "none"}");

    /// <summary>Gives a slot an ambient occlusion map, or takes it away.</summary>
    /// <param name="slot">The slot's name, made where the mesh has none of it.</param>
    /// <param name="path">The map, relative to the asset root, or nothing.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh SlotOcclusionMap(string slot, string? path) =>
        Look(slot, look => look with { OcclusionMap = MapPath(path) }, $"slot occlusion-map {slot} {path ?? "none"}");

    /// <summary>Gives a slot a map of metalness and roughness, or takes it away.</summary>
    /// <param name="slot">The slot's name, made where the mesh has none of it.</param>
    /// <param name="path">The map, metalness in its blue channel and roughness in its green, or nothing.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh SlotMetallicRoughnessMap(string slot, string? path) =>
        Look(slot, look => look with { MetallicRoughnessMap = MapPath(path) }, $"slot metallic-roughness-map {slot} {path ?? "none"}");

    /// <summary>Gives a slot an emissive color.</summary>
    /// <param name="slot">The slot's name, made where the mesh has none of it.</param>
    /// <param name="red">Red, in linear numbers that may pass one for a bright light.</param>
    /// <param name="green">Green, in linear numbers.</param>
    /// <param name="blue">Blue, in linear numbers.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh SlotEmissive(string slot, float red, float green, float blue)
    {
        foreach (var part in (ReadOnlySpan<float>)[red, green, blue])
        {
            if (!(part >= 0f) || !float.IsFinite(part)) throw new ArgumentOutOfRangeException(nameof(red), part, "A light's parts are finite numbers, zero or more.");
        }

        return Look(slot, look => look with { Emissive = new Vec3(red, green, blue) }, $"slot emissive {slot} {F(red)} {F(green)} {F(blue)}");
    }

    /// <summary>Gives a slot an emissive map, or takes it away.</summary>
    /// <param name="slot">The slot's name, made where the mesh has none of it.</param>
    /// <param name="path">The map, relative to the asset root, or nothing.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh SlotEmissiveMap(string slot, string? path) =>
        Look(slot, look => look with { EmissiveMap = MapPath(path) }, $"slot emissive-map {slot} {path ?? "none"}");

    /// <summary>Sets how metal a slot's faces are.</summary>
    /// <param name="slot">The slot's name, made where the mesh has none of it.</param>
    /// <param name="metallic">From zero, not metal, to one.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh SlotMetallic(string slot, float metallic)
    {
        Unit(metallic, "Metalness runs from zero to one.");
        return Look(slot, look => look with { Metallic = metallic }, $"slot metallic {slot} {F(metallic)}");
    }

    /// <summary>Sets how rough a slot's faces are.</summary>
    /// <param name="slot">The slot's name, made where the mesh has none of it.</param>
    /// <param name="roughness">From zero, a mirror, to one.</param>
    /// <returns>This mesh, for the next operation.</returns>
    public EditMesh SlotRoughness(string slot, float roughness)
    {
        Unit(roughness, "Roughness runs from zero to one.");
        return Look(slot, look => look with { Roughness = roughness }, $"slot roughness {slot} {F(roughness)}");
    }

    /// <summary>Changes a slot's look, the slot made where the mesh has none of its name, and writes the line.</summary>
    private EditMesh Look(string slot, Func<SlotLook, SlotLook> change, string line)
    {
        OneWord(slot, nameof(slot));
        var index = SlotIndex(slot);
        _looks[index] = change(_looks[index]);
        Changed();
        return Did(line);
    }

    /// <summary>The index of a slot, made with a look, the prototype's where none is given, where the mesh has none of its name.</summary>
    /// <remarks>
    /// A slot found keeps its own look, so a model joined or cut with another is drawn in this
    /// mesh's look for a slot both have, and in the other's for a slot this one lacks.
    /// </remarks>
    private int SlotIndex(string slot, SlotLook? look = null)
    {
        var index = _slots.IndexOf(slot);
        if (index >= 0) return index;

        _slots.Add(slot);
        _looks.Add(look ?? new SlotLook());
        return _slots.Count - 1;
    }

    private int Existing(string slot)
    {
        var index = _slots.IndexOf(slot);
        return index >= 0 ? index : throw new ArgumentException($"There is no slot called {slot}.", nameof(slot));
    }

    private static void OneWord(string name, string parameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, parameter);
        if (name.Any(char.IsWhiteSpace)) throw new ArgumentException("A slot's name is one word.", parameter);
    }

    private static void Unit(float value, string message)
    {
        if (!(value >= 0f && value <= 1f)) throw new ArgumentOutOfRangeException(nameof(value), value, message);
    }

    /// <summary>A map's path as the look keeps it, nothing taking the map away.</summary>
    private static string? MapPath(string? path)
    {
        if (path is null) return null;
        if (path.Length == 0 || path.Any(char.IsWhiteSpace) || path == "none")
            throw new ArgumentException("A texture's path is one word with no space in it, and none takes the texture away.", nameof(path));
        return path;
    }
}
