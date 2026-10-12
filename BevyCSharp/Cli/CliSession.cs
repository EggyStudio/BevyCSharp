using System.Diagnostics;
using System.Text.Json;

namespace Bevy;

/// <summary>
/// What one running app tells the command line about itself.
/// </summary>
/// <remarks>
/// <para>
/// A serving app writes one of these beside the others and keeps its heartbeat fresh; the command
/// line reads the directory to find what is running. Discovery by file rather than by a fixed port
/// lets several apps serve at once, and lets a tool tell a session that is up from one that was
/// killed without getting the chance to tidy up.
/// </para>
/// <para>
/// The file carries the port's token, so it is written readable by its owner and nobody else. The
/// permissions are the boundary that matters; the token stops something that already has a port
/// number from guessing its way in.
/// </para>
/// </remarks>
/// <param name="Pid">The process, which is also the file's name.</param>
/// <param name="Port">Where it is listening, on the loopback interface only.</param>
/// <param name="Token">What a request has to carry to be answered.</param>
/// <param name="Project">The directory it was started from, for telling projects apart.</param>
/// <param name="Name">The entry assembly, for telling two apps in one project apart.</param>
/// <param name="Title">The window title, as a person would recognize it.</param>
/// <param name="Started">When it came up.</param>
/// <param name="Abi">The native bridge version it loaded.</param>
/// <param name="Renderer">Whether that bridge can draw, which decides whether it can be captured.</param>
/// <param name="Editor">Whether that bridge carries the interface surface.</param>
/// <param name="State">Where it is in its life: <c>starting</c>, <c>ready</c> or <c>closing</c>.</param>
/// <param name="Frame">What the frame counter said at the last heartbeat.</param>
/// <param name="Heartbeat">When that was.</param>
public sealed record CliSession(
    int Pid,
    int Port,
    string Token,
    string Project,
    string Name,
    string Title,
    DateTimeOffset Started,
    int Abi,
    bool Renderer,
    bool Editor,
    string State,
    ulong Frame,
    DateTimeOffset Heartbeat)
{
    /// <summary>
    /// How long a session may go unheard from before it is assumed gone.
    /// </summary>
    /// <remarks>
    /// Generous next to the half-second the heartbeat is written at, because a frame that stalls on
    /// a shader compile or a slow asset load is still a session, and reporting it dead would send
    /// whoever asked off to start a second one.
    /// </remarks>
    public static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

    /// <summary>True when nothing has been heard from it for longer than <see cref="Patience"/>.</summary>
    public bool Stale => DateTimeOffset.UtcNow - Heartbeat > Patience;

    /// <summary>True when a process with this id is still there.</summary>
    /// <remarks>
    /// Asked separately from <see cref="Stale"/>, because the two say different things. A process
    /// that is gone leaves a file that will never be touched again, while one that is merely busy
    /// leaves a file that will be.
    /// </remarks>
    public bool Running
    {
        get
        {
            try
            {
                return !Process.GetProcessById(Pid).HasExited;
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException)
            {
                return false;
            }
        }
    }

    /// <summary>What <c>bcs status</c> calls it: ready, starting, unreachable or gone.</summary>
    public string Report => !Running ? "gone" : Stale ? "unreachable" : State;
}
