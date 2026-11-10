using System.Text;
using System.Text.Json;

namespace Bevy;

/// <summary>
/// What went wrong, as something a script can branch on.
/// </summary>
/// <param name="Code">
/// A stable token, in capitals. The sentence is for a person and may be reworded at any time; the
/// code is the part a script is allowed to depend on.
/// </param>
/// <param name="Message">One sentence about what happened, and where possible what to do about it.</param>
public readonly record struct CliError(string Code, string Message);
