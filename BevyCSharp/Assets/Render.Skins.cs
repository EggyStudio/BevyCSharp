using Bevy.Interop;

namespace Bevy;

public static unsafe partial class Render
{
    /// <summary>
    /// Gives a mesh made in code the joints each vertex follows and how much each of them moves it,
    /// which a skin then bends it by.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Four joints a vertex, each an index into the joints <see cref="SetSkin"/> gives the entity
    /// drawing the mesh, and four weights in the same order, which add up to one, since a vertex is
    /// placed where its joints would carry it, each counted by its weight. A vertex fewer than four
    /// joints move names any joint for the rest with a weight of zero, as Bevy's own example does.
    /// </para>
    /// <para>
    /// Apart from <see cref="MeshData"/>, so a mesh with no skin is described as it always was. The
    /// bounds Bevy culls a skinned mesh by, a box around each joint, are worked out from the mesh's
    /// positions here, which is why the mesh is given its vertices first.
    /// </para>
    /// </remarks>
    /// <param name="mesh">A mesh made with <see cref="CreateMesh(MeshData)"/>, which holds its vertices on this side.</param>
    /// <param name="joints">Four joint indices a vertex, in the order of the mesh's vertices.</param>
    /// <param name="weights">Four weights a vertex, in the order of its joints.</param>
    /// <exception cref="ArgumentException">
    /// The joints and the weights differ in number, are not four a vertex, or are for another
    /// number of vertices than the mesh holds.
    /// </exception>
    /// <exception cref="BevyNativeException">The mesh is gone or kept for the GPU alone.</exception>
    public static void SetMeshJoints(AssetHandle mesh, ReadOnlySpan<ushort> joints, ReadOnlySpan<float> weights)
    {
        if (joints.Length != weights.Length || joints.Length == 0 || joints.Length % 4 != 0)
            throw new ArgumentException($"{joints.Length} joints and {weights.Length} weights, where a vertex has four of each.", nameof(joints));

        int status;
        fixed (ushort* jointsAt = joints)
        fixed (float* weightsAt = weights)
        {
            status = Native.bcs_mesh_set_joints(mesh.Key, jointsAt, weightsAt, joints.Length / 4);
        }

        if (status == NativeStatus.InvalidState)
            throw new ArgumentException($"Joints for {joints.Length / 4} vertices, which {mesh} does not hold.", nameof(joints));
        Native.Check(status, $"giving {mesh} its joints");
    }

    /// <summary>
    /// Makes a skin, Bevy's inverse bindposes, one for each joint, which <see cref="SetSkin"/> binds
    /// an entity's mesh to its joints with.
    /// </summary>
    /// <remarks>
    /// Each is the inverse of where its joint stood, in the mesh's own space, when the mesh was
    /// bound to it, so a joint that has not moved since leaves its vertices where the mesh put
    /// them. Given as transforms, which hold any rig a model brings that has no shear, and turned
    /// into Bevy's matrices on the far side. A skin is an asset and is shared by every entity
    /// skinned to joints that stood alike, as Bevy's example shares one between ten meshes.
    /// </remarks>
    /// <param name="inverseBindposes">One transform a joint, in the order the joints are given in.</param>
    /// <exception cref="ArgumentException">There are none.</exception>
    /// <exception cref="BevyNativeException">This build keeps no skins.</exception>
    public static AssetHandle CreateSkin(ReadOnlySpan<Transform> inverseBindposes)
    {
        if (inverseBindposes.IsEmpty) throw new ArgumentException("A skin needs a bindpose for at least one joint.", nameof(inverseBindposes));

        var floats = new float[inverseBindposes.Length * 10];
        for (var i = 0; i < inverseBindposes.Length; i++)
        {
            var (t, r, s) = (inverseBindposes[i].Translation, inverseBindposes[i].Rotation, inverseBindposes[i].Scale);
            ReadOnlySpan<float> pose = [t.X, t.Y, t.Z, r.X, r.Y, r.Z, r.W, s.X, s.Y, s.Z];
            pose.CopyTo(floats.AsSpan(i * 10));
        }

        int key;
        fixed (float* at = floats)
        {
            Native.Check(Native.bcs_skin_create(at, inverseBindposes.Length, &key), "making a skin");
        }

        return new AssetHandle(key);
    }

    /// <summary>
    /// Skins the mesh an entity draws with a skin and the joints that move it, Bevy's
    /// <c>SkinnedMesh</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The joints are entities, in the order of the skin's bindposes and of the indices the mesh's
    /// vertices name, so the second joint moves every vertex whose joints name index one. Moving a
    /// joint's <see cref="Transform"/> bends the mesh, and the entity's own transform moves nothing,
    /// since every vertex goes where its joints carry it.
    /// </para>
    /// <para>
    /// The mesh is culled by bounds that follow its joints as they move, Bevy's
    /// <c>DynamicSkinnedMeshBounds</c>, from the boxes <see cref="SetMeshJoints"/> worked out, so
    /// a joint carrying part of it out of the box it was made in does not have it disappear.
    /// </para>
    /// </remarks>
    /// <param name="entity">The entity drawing the mesh.</param>
    /// <param name="skin">The skin, from <see cref="CreateSkin"/>.</param>
    /// <param name="joints">The joint entities, one for each of the skin's bindposes.</param>
    /// <exception cref="ArgumentException">No joints are given.</exception>
    /// <exception cref="BevyNativeException">The entity, a joint or the skin is gone.</exception>
    public static void SetSkin(Entity entity, AssetHandle skin, ReadOnlySpan<Entity> joints)
    {
        if (joints.IsEmpty) throw new ArgumentException("A skin moves a mesh by at least one joint.", nameof(joints));

        var bits = new ulong[joints.Length];
        for (var i = 0; i < joints.Length; i++) bits[i] = joints[i].Bits;

        fixed (ulong* at = bits)
        {
            Native.Check(Native.bcs_skin_set(entity.Bits, skin.Key, at, bits.Length), $"skinning {entity}");
        }
    }
}
