namespace Bevy;

/// <summary>A buffer on its way back from the GPU, from <see cref="Shaders.BeginBufferRead"/>.</summary>
public readonly record struct BufferRead(int Ticket);
