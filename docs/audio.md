# Audio

Sounds and music played, placed in the world and mixed.

<!-- compiled with:
AssetHandle theme = default;
-->
```csharp
var clip = AssetServer.Load(AssetKind.Audio, "sounds/hit.ogg");

Audio.Play(clip, AudioSettings.Effect);          // plays once, then despawns itself
var music = Audio.Play(theme, AudioSettings.Music);
Audio.SetVolume(music, 0.2f);
Audio.Stop(music);
```

Ogg Vorbis, WAV, FLAC and MP3. A file that is none of them, empty or holding something else, is
refused once it has loaded, before anything plays it, so its handle reports `Failed` and an
`AssetLoadFailed` message names the file, where Bevy alone would panic as it played. A sound that is
playing is an entity, so it can be despawned,
parented, tagged with your own components and found by a query, and `Play` hands that entity back.
`PlaybackMode.Despawn` suits a one-shot effect, because nothing has to remember to clean it up.
Played to a device, such a sound is played once and despawned at its end by the bridge rather than
by Bevy, whose despawn of it never gives the entity's index back to the world, so its
`PlaybackSettings` read through the reflected wrappers say `Once`.

A tone needs no file. `Audio.CreatePitch` makes one of a frequency and a length, as Bevy's `Pitch`
is, worked out as it plays, and `Play` plays it as it plays a clip, a semitone up being the
frequency times the twelfth root of two:

```csharp
var a = Audio.CreatePitch(440f, TimeSpan.FromSeconds(0.5));
Audio.Play(a, AudioSettings.Effect);
```

A sound the game makes as it plays, as a decoder of Bevy's makes one, is a stream of samples the game
writes ahead, since C# does not run on the audio thread that asks for them. It plays what it holds in
order and silence once it runs out, going on until it is stopped, so a game keeps it ahead of the
device, here by a tenth of a second written each frame:

<!-- compiled with:
float[] next = new float[4_410];
-->
```csharp
var stream = Audio.CreateSampleStream(sampleRate: 44_100, channels: 1);
Audio.Play(stream);

var wanted = 4_410 - Audio.QueuedSamples(stream);    // a tenth of a second ahead
if (wanted > 0) Audio.QueueSamples(stream, next.AsSpan(0, wanted));
```

A run with no window, headless or offscreen, as a test or a soak is, plays to no device unless
`Config.AudioWithoutWindow` asks for one. Its sounds are given a sink of the bridge's own, which
draws from each on the game's clock as a device would, so everything below answers as it would in a
window, a sound ends when it would have been heard to end, and `Audio.IsSilent` says which a run
does.

`SetVolume`, `Pause` and `Resume` reach the sink Bevy attaches once playback has started, so they
report `NotPresent` if called in the same frame the sound was started in, and in a window on a
machine with no device to play on, which never attaches one. `Audio.HasStarted` asks first. So do
`PositionOf` and `Seek`, which read and move the point a clip has reached, `SetSpeed`, which plays
it faster or slower with its pitch, and `SetMuted`, which silences it and keeps its volume for when
it is heard again:

<!-- compiled with:
Entity music = default;
-->
```csharp
if (!Audio.HasStarted(music)) return;
var at = Audio.PositionOf(music);       // seconds into the clip
Audio.Seek(music, at - 5f);             // back five seconds
Audio.SetSpeed(music, 1.5f);            // half again as fast, and higher
Audio.SetMuted(music, true);
Audio.SetGlobalVolume(0.4f);            // the master slider, over everything at once
```

A looping sound refuses to be sought, because looping keeps the decoded samples so the clip can
start again and what holds them has no way to move within them. Music that has to resume where it left
off is played once and restarted rather than looped.

A sound can play on a bus, which is a name and a volume, so a settings screen's music and effects
sliders are one call each rather than a walk over every sound playing:

<!-- compiled with:
AssetHandle theme = default, hit = default;
-->
```csharp
Audio.Play(theme, new AudioSettings { Mode = PlaybackMode.Loop, Bus = "music" });
Audio.Play(hit, new AudioSettings { Mode = PlaybackMode.Despawn, Bus = "effects" });

Audio.SetBusVolume("music", 0.3f);      // every music track, now and later
```

A sound is heard at its own volume times its bus's times the global one, and each is kept apart,
so a fade on one track and the music slider multiply rather than overwrite each other. Bevy has no
mixer, so the buses are kept on the managed side over each sound's volume. `Audio.VolumeOf` and
`Audio.IsPaused` read a sound back.

`Start` and `Play` cut a window out of a clip, which is how one file holds several effects:

<!-- compiled with:
AssetHandle footsteps = default;
-->
```csharp
Audio.Play(footsteps, new AudioSettings { Start = 1.2f, Play = 0.35f, Mode = PlaybackMode.Despawn });
```

One decode covers the sheet, rather than one file and one decode per effect. `Play` left at zero
runs to the end of the clip.

A sound can be placed in the world instead of played into both ears equally. That takes two
things: the sound saying so, and an entity to hear from.

<!-- compiled with:
AssetHandle hum = default;
-->
```csharp
Audio.SetListener(Render.SpawnCamera3d());      // usually the camera

var engine = Audio.Play(hum, new AudioSettings
{
    Mode = PlaybackMode.Loop,
    Spatial = true,
    SpatialScale = 0.01f,               // a world measured in pixels rather than meters
});

ctx.Ecs.Add(engine, Transform.At(4f, 0f, -2f));
```

A spatial sound is given a `Transform` to be moved by, and is heard quieter with distance and
further to one side as it crosses the listener. `Config.SpatialScale` makes that work in a world
whose units are not meters, set once for the app because what the world is measured in is a fact
about the game rather than about any one sound; a sound may still say otherwise for itself.

`Audio.SetListener` takes an ear gap, and an overload takes the two ear positions instead. Placing
them says which way a head is facing as well as how wide it is, for a listener carried by a
character rather than by a camera.

Sound is in the render profile rather than the minimal one, and not because it draws. It is the
one part of the engine that needs a system library at build time. See
[.github/BUILDING.md](../.github/BUILDING.md).

---

Before this, [The interface](ui.md).
Next, [Physics](physics.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#audio). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#audio). The [guide's contents](../README.md#guide) list every page.
