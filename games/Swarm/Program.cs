using Bevy;
using Bevy.Physics;

// Swarm, an arena held against waves of creatures that come at the player in their hundreds, the
// player walked with WASD and firing at the nearest creature by itself. Enter starts, and the
// game ends when the player falls or the fifth wave is cleared.
//
// BCS_OFFSCREEN=1 draws into an image rather than a window and BCS_SERVE=1 answers bcs, which is
// how play.sh plays it with no display.
var config = Config.Windowed("Swarm", 1280, 720);
config.GameName = "Swarm";

// Every behavior is a script in assets, which the package compiles into this program, and the
// arena is built by one of them, so what is left here is the window and physics.
return BevyApp.Run(app => app.AddPlugin(new PhysicsPlugin()), config);
