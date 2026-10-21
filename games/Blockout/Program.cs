using Bevy;
using Bevy.Physics;

// Blockout, a level blocked out from one recipe of the modeling tools and walked in, three rooms, a
// corridor, stairs up to a second floor, two doors and a window, in the prototype materials. WASD
// walks, the right button held or the arrow keys look about, Space jumps, Tab opens the modeling
// window on the level so it is reshaped while it is walked in, and F5 saves the recipe.
//
// BCS_OFFSCREEN=1 draws into an image rather than a window and BCS_SERVE=1 answers bcs, which is
// how play.sh plays it with no display.
var config = Config.Windowed("Blockout", 1280, 720);
config.GameName = "Blockout";

// The modeling window is drawn with Dear ImGui.
config.Gui = true;

// Ray-traced lighting where the bridge was built with it and the GPU traces rays, which lights the
// rooms through their doors and window and off their walls. Elsewhere, the package's bridge among
// those, the bridge says it was asked for and left out, and the level is lit by the sun, an ambient
// light and ambient occlusion.
config.RayTracedLighting = true;

return BevyApp.Run(app => app.AddPlugin(new PhysicsPlugin()), config);
