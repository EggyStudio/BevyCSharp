namespace Bevy;

/// <summary>The sky's part in the app's frame, which <see cref="EnginePlugin"/> adds to every app.</summary>
internal sealed class SkyWork : IPlugin
{
    public void Build(App app)
    {
        // In a run that draws and has a window to show it, after the scene's field and its probes,
        // the weather, which gives the clouds, the fog, the rain and the storm their numbers, the
        // storm, whose flash the clouds glow with, the clouds, carried on by the wind, the fog's
        // froxels, whose images trade places every frame and which read the clouds' shadow this
        // frame, the rain, which reads it too, and the night sky, which reads its clock.
        void Shown(int place, Action<World> step) => app.AreasWork.Add(place, world =>
        {
            if (App.HasRenderer && !world.Resource<Config>().Headless) step(world);
        });
        Shown(2200, SkyWeather.Tick);
        Shown(2300, SkyThunder.Tick);
        Shown(2400, Clouds.Tick);
        Shown(2500, SkyFog.Tick);
        Shown(2600, Precipitation.Tick);
        Shown(2700, NightSky.Tick);
    }
}
