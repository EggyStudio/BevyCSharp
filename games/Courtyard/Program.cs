using Bevy;
using Bevy.Physics;

// Courtyard, in which the runner is walked around a courtyard with WASD to pick up every coin
// and bring them to the green goal. Enter starts, Escape pauses, F5 saves and F9 loads.
//
// BCS_OFFSCREEN=1 draws into an image rather than a window and BCS_SERVE=1 answers bcs, which is
// how play.sh plays it with no display.
var config = Config.Windowed("Courtyard", 1280, 720);
config.GameName = "Courtyard";

// The behaviors are the scripts in assets, which the package compiles into this program, and the
// states are declared on their enums, so what is left here is physics and the level.
return BevyApp.Run(app => app.AddPlugin(new PhysicsPlugin()), config);

namespace Courtyard
{
    /// <summary>Loads the level the project starts in.</summary>
    /// <remarks>
    /// Here rather than with the scripts, since the editor loads those while the level is edited
    /// and a level loading itself into the editor would be a second one on top of it.
    /// </remarks>
    [Behavior]
    public partial struct Boot
    {
        /// <summary>Spawns the startup scene and starts the save from it.</summary>
        [OnStartup]
        public static void Start(BehaviorContext ctx)
        {
            if (ctx.Res<ProjectSettings>().StartupScene is not { Length: > 0 } level)
            {
                Console.Error.WriteLine($"[courtyard] {ProjectSettings.FileName} names no startup scene");
                return;
            }

            Console.WriteLine($"[courtyard] {level}, {SaveGame.Start(ctx.Ecs, level).Count} entities");
        }
    }
}
