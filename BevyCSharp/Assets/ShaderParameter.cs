namespace Bevy;

/// <summary>One name a shader declares, as <see cref="ShaderValues"/> reports it.</summary>
/// <param name="Kind">What it is.</param>
/// <param name="Name">The name its value is set by.</param>
/// <param name="Scalar">For numbers, which kind.</param>
/// <param name="Components">
/// For numbers, how many make one element: one for a scalar, four for a <c>float4</c>, sixteen for
/// a <c>float4x4</c>.
/// </param>
/// <param name="Count">How many elements, which is one unless it is an array.</param>
public readonly record struct ShaderParameter(
    ShaderParameterKind Kind,
    string Name,
    ShaderScalar Scalar,
    int Components,
    int Count)
{
    /// <summary>Reads the bridge's listing, one tab-separated line per name.</summary>
    internal static IReadOnlyList<ShaderParameter> Parse(string text)
    {
        var parameters = new List<ShaderParameter>();

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t');

            static int Number(string text) =>
                int.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : 1;

            switch (parts)
            {
                case ["number", var name, var scalar, var components, var count]:
                    parameters.Add(new ShaderParameter(
                        ShaderParameterKind.Number,
                        name,
                        scalar switch
                        {
                            "int" => ShaderScalar.Int,
                            "uint" => ShaderScalar.UInt,
                            "bool" => ShaderScalar.Bool,
                            _ => ShaderScalar.Float,
                        },
                        Number(components),
                        Number(count)));
                    break;

                case [var kind, var name, var count]:
                    parameters.Add(new ShaderParameter(
                        kind switch
                        {
                            "texture" => ShaderParameterKind.Texture,
                            "image" => ShaderParameterKind.Image,
                            "buffer" => ShaderParameterKind.Buffer,
                            "sampler" => ShaderParameterKind.Sampler,
                            "scene" => ShaderParameterKind.RayScene,
                            _ => ShaderParameterKind.Struct,
                        },
                        name,
                        ShaderScalar.Float,
                        0,
                        Number(count)));
                    break;
            }
        }

        return parameters;
    }
}
