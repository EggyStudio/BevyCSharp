namespace Bevy;

/// <summary>The mesh primitives the engine can build without an asset file.</summary>
public static class MeshShape
{
    /// <summary>A box, sized by width, height and depth.</summary>
    public const string Cuboid = "Cuboid";

    /// <summary>A sphere, sized by radius.</summary>
    public const string Sphere = "Sphere";

    /// <summary>
    /// A sphere of slices and rings, sized by radius, then the number of slices around it and of
    /// rings from pole to pole.
    /// </summary>
    /// <remarks>
    /// <see cref="Sphere"/> is Bevy's default, a solid of subdivided triangles, which is even all
    /// over. This one is made as a globe is drawn, so an image wraps around it with its edges at
    /// one seam and its top and bottom rows at the poles.
    /// </remarks>
    public const string UvSphere = "UvSphere";

    /// <summary>A flat plane on the XZ axes, sized by width and depth.</summary>
    public const string Plane = "Plane";

    /// <summary>A capsule, sized by radius and length.</summary>
    public const string Capsule = "Capsule";

    /// <summary>A cylinder, sized by radius and height.</summary>
    public const string Cylinder = "Cylinder";

    /// <summary>A cone, sized by the radius of its base and its height.</summary>
    public const string Cone = "Cone";

    /// <summary>A cone with its top cut off, sized by the top radius, the bottom radius and the height.</summary>
    public const string ConicalFrustum = "ConicalFrustum";

    /// <summary>A ring with a round cross-section, sized by its inner and outer radius.</summary>
    public const string Torus = "Torus";

    /// <summary>A flat disc facing the viewer, sized by radius.</summary>
    public const string Circle = "Circle";

    /// <summary>A flat ring facing the viewer, sized by its inner and outer radius.</summary>
    public const string Annulus = "Annulus";

    /// <summary>A flat rectangle facing the viewer, sized by width and height.</summary>
    public const string Rectangle = "Rectangle";

    /// <summary>A flat slice of a disc, sized by its radius and the half angle it spans, in radians.</summary>
    public const string CircularSector = "CircularSector";

    /// <summary>A flat disc cut by a chord, sized by its radius and the half angle the chord spans.</summary>
    public const string CircularSegment = "CircularSegment";

    /// <summary>A flat oval, sized by its half width and half height.</summary>
    public const string Ellipse = "Ellipse";

    /// <summary>A flat capsule, sized by its radius and the length between its two round ends.</summary>
    public const string Capsule2d = "Capsule2d";

    /// <summary>A flat rhombus, sized by its horizontal and vertical diagonals.</summary>
    public const string Rhombus = "Rhombus";

    /// <summary>A flat regular polygon, sized by its circumradius and given its number of sides.</summary>
    public const string RegularPolygon = "RegularPolygon";

    /// <summary>A flat triangle, Bevy's default one scaled by the first number.</summary>
    public const string Triangle = "Triangle";

    /// <summary>A four-sided solid, Bevy's default one scaled by the first number.</summary>
    public const string Tetrahedron = "Tetrahedron";

    /// <summary>
    /// A flat band along the inside of another flat shape's outline, sized by that shape's
    /// measures and, third, how wide the band is.
    /// </summary>
    /// <param name="outline">
    /// <see cref="Circle"/>, <see cref="CircularSector"/>, <see cref="CircularSegment"/>,
    /// <see cref="Ellipse"/>, <see cref="Capsule2d"/>, <see cref="Rhombus"/>,
    /// <see cref="Rectangle"/>, <see cref="RegularPolygon"/> or <see cref="Triangle"/>.
    /// </param>
    /// <remarks>
    /// Bevy's <c>to_ring</c>. Most shapes are inset by the width evenly on every side. A sector
    /// and an ellipse cannot be, since the curve inside an ellipse at an even distance is no
    /// ellipse, so the inside of a sector is the same sector of a smaller radius and the inside of
    /// an ellipse is an ellipse smaller on each axis, as Bevy's examples draw them. A ring of
    /// a circle is the same mesh an <see cref="Annulus"/> makes.
    /// </remarks>
    public static string Ring(string outline) => $"Ring({outline})";

    /// <summary>
    /// A flat shape pushed out into a solid along Z, sized by that shape's measures and, third,
    /// how deep it is, centered on its middle.
    /// </summary>
    /// <param name="outline">
    /// <see cref="Circle"/>, <see cref="Annulus"/>, <see cref="CircularSector"/>,
    /// <see cref="CircularSegment"/>, <see cref="Ellipse"/>, <see cref="Capsule2d"/>,
    /// <see cref="Rhombus"/>, <see cref="Rectangle"/>, <see cref="RegularPolygon"/> or
    /// <see cref="Triangle"/>.
    /// </param>
    /// <remarks>
    /// Bevy's <c>Extrusion</c>, a coin from a circle, a pipe from an annulus or a prism from a
    /// polygon, with the front and back faces the flat shape is and sides that run straight
    /// between their edges.
    /// </remarks>
    public static string Extrusion(string outline) => $"Extrusion({outline})";

    /// <summary>
    /// A sector or a segment whose image is mapped at an angle, sized by its radius, the half angle
    /// it spans and, third, the angle in radians.
    /// </summary>
    /// <param name="arc"><see cref="CircularSector"/> or <see cref="CircularSegment"/>.</param>
    /// <remarks>
    /// Bevy's <c>CircularMeshUvMode::Mask</c>. A sector or a segment is mapped onto its image as a
    /// mask over the circle it is cut from, the circle's center at the image's center, and the angle
    /// turns the vertices as they are mapped rather than the image. A shape turned by its transform
    /// shows its image upright when the angle is that turn the other way, which is how Bevy's
    /// <c>mesh2d_arcs</c> keeps its logo upright in every slice.
    /// </remarks>
    public static string UvAngle(string arc) => $"UvAngle({arc})";
}
