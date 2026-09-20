using BevyCSharp.Cli;

// The one entry point an agent uses on this repository: find a running app, ask it what it can do,
// drive it, capture what it draws, and build and test around that. Everything is one envelope on
// the standard output stream and one exit code, so nothing here has to be read by a person.

var (options, rest) = Options.Parse(args);

if (rest.Length == 0) return Help.Print();

var verb = rest[0];
var remainder = rest[1..];

return verb switch
{
    "status" => Verbs.Status(options),
    "list" => Verbs.List(options),
    "command" or "cmd" => Verbs.Command(options, remainder),
    "shot" or "screenshot" => Verbs.Shot(options, remainder),
    "stop" => Verbs.Stop(options),
    "open" => Launch.Open(options, remainder),
    "logs" => Launch.Logs(options, remainder),
    "build" => Tools.Build(options, remainder),
    "test" => Tools.Test(options, remainder),
    "run" => Tools.Run(options, remainder),
    "doctor" => Tools.Doctor(options),
    "help" or "--help" or "-h" => Help.Print(),
    "version" or "--version" or "-V" => Help.Version(options),
    _ => Help.Unknown(options, verb),
};
