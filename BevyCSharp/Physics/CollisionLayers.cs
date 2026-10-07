namespace Bevy.Physics;

/// <summary>
/// Each collidable's layer, 0 unless set, and which of the 32 layers collide with which, every one
/// with every other to begin with, which the narrow phase asks of a pair before it makes their
/// contacts.
/// </summary>
/// <remarks>
/// Written only between steps and read from Bepu's worker threads during one, so it takes no lock.
/// A collidable is named by its packed reference, which Bepu gives out again once a body is gone,
/// so a body's layer is forgotten as it is removed.
/// </remarks>
internal sealed class CollisionLayers
{
    /// <summary>How many layers there are.</summary>
    public const int Count = 32;

    private readonly Dictionary<uint, byte> _layers = [];

    // Bit b of entry a, set where layers a and b collide.
    private readonly uint[] _collides = [.. Enumerable.Repeat(uint.MaxValue, Count)];

    /// <summary>Puts a collidable on a layer, or back on layer 0, which it is not kept for.</summary>
    public void Set(uint packed, int layer)
    {
        if (layer == 0) _layers.Remove(packed);
        else _layers[packed] = (byte)layer;
    }

    /// <summary>The layer a collidable is on.</summary>
    public int Of(uint packed) => _layers.GetValueOrDefault(packed);

    /// <summary>Whether bodies on layer <paramref name="a"/> collide with bodies on <paramref name="b"/>, both ways.</summary>
    public void SetCollide(int a, int b, bool collide)
    {
        if (collide)
        {
            _collides[a] |= 1u << b;
            _collides[b] |= 1u << a;
        }
        else
        {
            _collides[a] &= ~(1u << b);
            _collides[b] &= ~(1u << a);
        }
    }

    /// <summary>Whether two layers collide.</summary>
    public bool Collide(int a, int b) => (_collides[a] & (1u << b)) != 0;

    /// <summary>Whether two collidables' layers collide, quick where no body has a layer of its own.</summary>
    public bool Collide(uint a, uint b) => _layers.Count == 0 || Collide(Of(a), Of(b));
}
