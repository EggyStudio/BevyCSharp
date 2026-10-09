//! App lifecycle: construction, stage wiring, system registration and the run loop.

use core::time::Duration;

use bevy::app::{App, AppExit, First, Last, PostUpdate, ScheduleRunnerPlugin};
use bevy::ecs::schedule::IntoScheduleConfigs;
use bevy::ecs::world::World;
use bevy::prelude::Resource;

use crate::interop::{status, BcsConfig};
use crate::state::{app_mut, loan_world, with_world, BcsApp, CleanupList, SystemReg};

// Where these were before their own modules, which the rest of the bridge names them by.
pub use crate::capabilities::bcs_render_adapter;
pub use crate::component_registry::bcs_component_id_of;
#[cfg(feature = "render")]
pub use crate::offscreen::OffscreenTarget;
pub use crate::stages::Stage;
pub(crate) use crate::stages::BcsSet;

/// Marker inserted once the `Cleanup` callbacks have run, so they run exactly once.
#[derive(Resource)]
struct CleanupRan;

/// Frame budget for headless runs, used to stop after a fixed number of ticks.
#[derive(Resource)]
struct HeadlessFrameLimit {
    remaining: u32,
}

/// Builds the Bevy app for the requested configuration.
///
/// In a `render` build with `headless == 0` this installs `DefaultPlugins` (window,
/// renderer, input backend). Otherwise it installs `MinimalPlugins` plus the input
/// plugin, so the same managed code runs unchanged without a display.
fn build_app(config: &BcsConfig, title: Option<String>, cleanup: CleanupList) -> App {
    let mut app = App::new();

    // Bevy resolves a relative asset directory against the executable, which for a .NET host is
    // whatever launched the process: `dotnet` itself under `dotnet test`, not the directory the
    // assembly and its assets are in. Naming the directory outright is the only way to be sure,
    // so the managed side passes one whenever it knows where its assets are.
    // `None` leaves the decision to Bevy, which watches only when the build carries a watcher
    // and something asked for one. Saying `false` outright would stop a profile that has the
    // watcher from ever using it.
    let watch = if config.watch_assets != 0 { Some(true) } else { None };

    let asset_plugin = move |root: &Option<String>| match root {
        Some(path) if !path.is_empty() => bevy::asset::AssetPlugin {
            file_path: path.clone(),
            watch_for_changes_override: watch,
            ..Default::default()
        },
        _ => bevy::asset::AssetPlugin {
            watch_for_changes_override: watch,
            ..Default::default()
        },
    };
    let asset_root = unsafe { crate::interop::cstr_to_string(config.asset_root) };

    // The player's directory as a second root beside the assets, named `user`, so `user://` reads
    // the same files the managed side writes there. Bevy builds its sources when the asset plugin
    // is added, so it is registered before either branch below adds one.
    let user_root = unsafe { crate::interop::cstr_to_string(config.user_root) };
    if let Some(root) = user_root.as_deref().filter(|root| !root.is_empty()) {
        use bevy::asset::io::AssetSourceBuilder;
        use bevy::asset::AssetApp;

        app.register_asset_source("user", AssetSourceBuilder::platform_default(root, None));
    }

    // The folders the managed side named as sources of their own, such as the editor's own icons
    // beside a project it opens, registered for the same reason and at the same time.
    crate::assets::install_sources(&mut app);

    // The two clock systems frame.profile reads, which do nothing until it turns counting on.
    crate::profile::install(&mut app);

    // The assets the game's own assembly carries, read after the disk through the managed side,
    // where it handed a reader over before this app was built. Registered for the same reason the
    // player's source is, before the asset plugin builds the sources. A bridge carrying a game's
    // assets itself has its own source below, which this would only stand in front of.
    #[cfg(not(feature = "embed"))]
    crate::carried::install(&mut app, asset_root.as_deref().filter(|root| !root.is_empty()).unwrap_or("assets"));

    // A game's own assets compiled in, read in place of the asset root, so every load finds the
    // embedded file under the path it always had. Added here for the same reason the player's
    // source is, before the asset plugin builds the sources, and only by a bridge built for one
    // game (`build-native.sh --embed`).
    #[cfg(feature = "embed")]
    app.add_plugins(bevy_embedded_assets::EmbeddedAssetPlugin {
        mode: bevy_embedded_assets::PluginMode::ReplaceDefault,
    });

    // Whether the renderer is installed at all. A window is one way to draw and an image is the
    // other, and both take Bevy's full plugin set, so everything below asks this rather than
    // asking about the window. Only a run with no renderer takes the minimal set.
    #[cfg(feature = "render")]
    let drawing = config.headless == 0;
    #[cfg(not(feature = "render"))]
    let drawing = false;

    if drawing {
        #[cfg(feature = "render")]
        {
            use bevy::prelude::*;
            use bevy::render::settings::{Backends, RenderCreation, WgpuSettings};
            use bevy::render::RenderPlugin;
            use bevy::window::{ExitCondition, PresentMode, Window, WindowPlugin};
            use bevy::winit::WinitPlugin;

            // Drawing into an image instead of onto a screen. Everything else about the app is
            // unchanged, which is the point, because the same behavior code running against the
            // same renderer makes the picture worth looking at on a machine that has no display to
            // open a window on.
            let offscreen = config.offscreen != 0;

            if config.desktop_title_bar != 0 && !offscreen {
                prefer_desktop_title_bar();
            }

            let present_mode = if config.vsync != 0 {
                PresentMode::AutoVsync
            } else {
                PresentMode::AutoNoVsync
            };

            // `None` leaves wgpu's own preference order alone, which already puts Vulkan first
            // on Linux and Windows. Naming a backend pins the choice instead, and startup then
            // fails loudly if the machine cannot provide it, which is the point of asking.
            let backends = match config.backend {
                1 => Some(Backends::VULKAN),
                2 => Some(Backends::DX12),
                3 => Some(Backends::METAL),
                4 => Some(Backends::GL),
                _ => None,
            };

            let wgpu = match backends {
                Some(backends) => WgpuSettings {
                    backends: Some(backends),
                    ..default()
                },
                None => WgpuSettings::default(),
            };

            let plugins = DefaultPlugins
                .set(WindowPlugin {
                    primary_window: if offscreen {
                        None
                    } else {
                        Some(Window {
                            title: title.clone().unwrap_or_else(|| "BevyCSharp".to_string()),
                            resolution: if config.scale_factor > 0.0 {
                                bevy::window::WindowResolution::new(config.width, config.height)
                                    .with_scale_factor_override(config.scale_factor)
                            } else {
                                (config.width, config.height).into()
                            },
                            // Where it was last closed, when the game asks for that, so it does
                            // not open where the platform chooses and then jump.
                            position: if config.has_position != 0 {
                                bevy::window::WindowPosition::At(bevy::math::IVec2::new(config.x, config.y))
                            } else {
                                bevy::window::WindowPosition::Automatic
                            },
                            present_mode,
                            // Composited with alpha, where asked for, which the platform has to
                            // know before the window exists. Premultiplied, since a camera and the
                            // interface write that. Where the surface cannot, the window
                            // module falls back before the surface is made.
                            transparent: config.transparent != 0,
                            composite_alpha_mode: if config.transparent != 0 {
                                bevy::window::CompositeAlphaMode::PreMultiplied
                            } else {
                                bevy::window::CompositeAlphaMode::Auto
                            },
                            ..default()
                        })
                    },

                    // A windowless run has no window to close, and the default condition ends an
                    // app the moment the last one is gone, which here is immediately.
                    exit_condition: if offscreen {
                        ExitCondition::DontExit
                    } else {
                        ExitCondition::OnPrimaryClosed
                    },
                    ..default()
                })
                .set(RenderPlugin {
                    render_creation: RenderCreation::Automatic(Box::new(wgpu)),
                    ..default()
                })
                .set(asset_plugin(&asset_root));

            // Bevy's logger, set by the first app of the process alone, since a second app's
            // plugin finds it set and says so as an error. See the log module.
            let plugins = if crate::log::first() {
                plugins.set(crate::log::plugin())
            } else {
                plugins.disable::<bevy::log::LogPlugin>()
            };

            if offscreen {
                // Winit owns the loop when there is a window, and panics on a machine with no
                // display server, which is exactly the machine this mode is for. Dropping it
                // leaves the app with no runner, so the schedule runner takes the loop instead,
                // paced like a headless run.
                app.add_plugins(plugins.disable::<WinitPlugin>());

                let runner = if config.headless_fps > 0 {
                    ScheduleRunnerPlugin::run_loop(Duration::from_secs_f64(
                        1.0 / config.headless_fps as f64,
                    ))
                } else {
                    ScheduleRunnerPlugin::run_loop(Duration::ZERO)
                };

                app.add_plugins(runner);
                crate::offscreen::install_offscreen_target(&mut app, config.width.max(1), config.height.max(1));
            } else {
                app.add_plugins(plugins);
            }

            // Meshlets, if the app asked for them and the bridge was built with them. Added beside
            // the default plugins rather than inside them, because the plugin has to be told its
            // cluster budget and has to be kept out entirely on a GPU that cannot run it.
            #[cfg(feature = "meshlet")]
            crate::render::meshlets::forget();

            #[cfg(feature = "meshlet")]
            if config.meshlet_clusters > 0 {
                let backends = backends.map(|backends| wgpu::Backends::from_bits_truncate(backends.bits()));
                crate::render::meshlets::install(&mut app, config.meshlet_clusters, backends);
            }

            // Ray-traced lighting, likewise asked for and likewise kept out where the adapter
            // cannot trace rays, since adding it makes every material deferred.
            #[cfg(feature = "solari")]
            crate::render::solari::forget();

            #[cfg(feature = "solari")]
            if config.ray_traced_lighting != 0 {
                let backends = backends.map(|backends| wgpu::Backends::from_bits_truncate(backends.bits()));
                crate::render::solari::install(&mut app, backends);
            }

            #[cfg(not(feature = "solari"))]
            if config.ray_traced_lighting != 0 {
                bevy::log::warn!(
                    "Ray-traced lighting was asked for, but this bridge was built without it. \
                     Build it with --solari."
                );
            }

            #[cfg(not(feature = "meshlet"))]
            if config.meshlet_clusters > 0 {
                bevy::log::warn!(
                    "Meshlets were asked for, but this bridge was built without them. Build it \
                     with --meshlet."
                );
            }

            // The weather, where the app asked for it, after the meshlets, which keep it out.
            crate::render::weather::forget();

            if config.weather != 0 {
                crate::render::weather::install(&mut app);
            }

            // What the app before this one drew, which would otherwise answer for a camera of
            // this one that happens to reuse its entity number.
            crate::render::watch::forget();

            // Only when asked, since measuring writes timestamps around every pass and reads them
            // back every frame.
            crate::render::timings::forget();

            if config.gpu_timings != 0 {
                crate::render::timings::install(&mut app);
            }

            // Only where a see-through window was asked for, which is the one case where the mode
            // the window asks to be composited in might be one its surface cannot do.
            if config.transparent != 0 && !offscreen {
                crate::window::install(&mut app);
            }

            // Auto exposure is the one post-processing effect `DefaultPlugins` leaves out, since
            // it needs compute shaders and so cannot run everywhere the rest can. Every desktop
            // backend the bridge builds for has them.
            app.add_plugins(bevy::post_process::auto_exposure::AutoExposurePlugin);

            // The render schedule's clock for frame.profile, which does nothing until asked.
            crate::profile::install_render(&mut app);

            // Drawing a mesh as its edges, which an editor offers as a way to outline what is
            // selected, and the same for a 2D mesh, which Bevy draws through its own pipeline and
            // so outlines with its own plugin. Only where the app asked, since each plugin looks at
            // every mesh of its kind in its prepare and queue phases every frame whether or not any
            // is outlined, which at a hundred thousand sprite meshes is 8 ms of the render.
            if config.wireframes != 0 {
                app.add_plugins(bevy::pbr::wireframe::WireframePlugin::default());
                app.add_plugins(bevy::sprite_render::Wireframe2dPlugin::default());
            }

            // Materials drawn by shaders the game wrote, and what compiles and reloads them. The
            // asset root is resolved the way the asset server resolves it, because a Slang file is
            // handed to a compiler by its path on disk rather than read through the server.
            let shader_root = bevy::asset::io::file::FileAssetReader::new(
                asset_root
                    .as_deref()
                    .filter(|path| !path.is_empty())
                    .unwrap_or("assets"),
            )
            .root_path()
            .clone();
            crate::render::material::install(&mut app, shader_root);

            // Which clip each spawned model plays, and the clips that reached their end.
            crate::animation::install(&mut app);

            // HTML and CSS driven UI, when the profile carries it and the app asked for it.
            //
            // Asked for rather than assumed, because the plugin is not free to an app that never
            // opens a document. It spawns a camera of its own, registers its widget systems and
            // watches for documents to build. The editor profile is a superset of the render
            // one, so the sample and a game are built against exactly this library.
            #[cfg(feature = "editor")]
            if config.gui != 0 {
                crate::imgui::install(&mut app);
                crate::capabilities::INTERFACE_INSTALLED.store(true, std::sync::atomic::Ordering::Relaxed);
            }

            // The mesh under a pointer, where an app asks for it, and in the editor, where
            // clicking a mesh to select it is the other half of what a hierarchy list does.
            #[cfg(feature = "editor")]
            let wants_meshes = config.mesh_picking != 0 || config.gui != 0;
            #[cfg(not(feature = "editor"))]
            let wants_meshes = config.mesh_picking != 0;
            if wants_meshes {
                crate::pick::install(&mut app);
            }

            // The scene-wide ambient light and clear color, kept in the picture from the frame
            // they are set, which Bevy's own copying misses for a value set at startup.
            crate::render::post::install(&mut app);

            // An image cannot be told it is a cubemap until it has loaded, so what asks for one
            // leaves the handle here and this picks it up on whichever frame the pixels arrive.
            app.init_resource::<crate::render::post::PendingCubemaps>();
            app.init_resource::<crate::render::post::PendingEnvironments>();
            app.init_resource::<crate::render::post::PendingFaces>();
            app.add_systems(
                bevy::app::PreUpdate,
                (
                    crate::render::post::gather_faces,
                    crate::render::post::reinterpret_cubemaps,
                )
                    .chain(),
            );
            // An irradiance volume's image is only known to be 3D once it has loaded, and once
            // `MakeVolume` has reshaped it, so the check comes after the reshape.
            app.init_resource::<crate::render::images::PendingReshapes>();
            app.add_systems(
                bevy::app::PreUpdate,
                (
                    crate::render::images::reshape_images,
                    crate::render::probes::drop_flat_volumes,
                )
                    .chain(),
            );

            // Debug drawing goes through a queue, because a `Gizmos` parameter cannot be held by an
            // exclusive system. Only registered here, because the plugin that draws them comes with
            // `DefaultPlugins`, so a windowless app has nothing to drain into.
            app.init_resource::<crate::gizmos::GizmoQueue>();

            // Tab and Shift+Tab moving the input focus through an interface's fields and buttons in
            // the order their `TabIndex` gives, within each `TabGroup`. Bevy leaves it out of its
            // defaults, and an interface with text fields is hard to use without it; one that
            // marks nothing with a tab index is unaffected.
            app.add_plugins(bevy::input_focus::tab_navigation::TabNavigationPlugin);
            // And the arrows or a pad moving it by direction, which needs the map of edges and the
            // settings of the nearest-node search this plugin makes, and which nothing does until a
            // game moves the focus through `bcs_nav_move`.
            app.add_plugins(bevy::input_focus::directional_navigation::DirectionalNavigationPlugin);
            // Drained after everything has had its say, and explicitly after the managed `Last`
            // systems, because both live in `Last` and without the ordering the scheduler is free
            // to drain the queue before the frame has filled it, which holds every shape back a
            // frame. A gizmo that arrives a frame late reads as one that lags behind whatever it is
            // drawn on, and worst of all on a shape placed relative to the camera, which then swims
            // about the screen whenever the camera turns.
            app.add_systems(bevy::app::Last, crate::gizmos::drain.after(BcsSet::Last));

            // Drawn in front of the scene rather than inside it. A gizmo is a thing drawn *about*
            // the world (a selection, a handle, an axis) and one that disappears into the object it
            // describes has failed at the only job it has.
            app.init_gizmo_group::<crate::gizmos::FrontGizmos>();
            app.add_systems(bevy::app::Startup, crate::gizmos::draw_in_front);

            // Where the readers of Bevy's window messages keep their place between frames.
            app.init_resource::<crate::events::WindowEventCursors>();
            app.init_resource::<crate::events::FileDrops>();
            app.init_resource::<crate::events::ImeMessages>();
        }
    } else {
        use bevy::MinimalPlugins;
        use bevy::prelude::PluginGroup;

        let runner = if config.headless_fps > 0 {
            ScheduleRunnerPlugin::run_loop(Duration::from_secs_f64(
                1.0 / config.headless_fps as f64,
            ))
        } else {
            ScheduleRunnerPlugin::run_loop(Duration::ZERO)
        };
        app.add_plugins(MinimalPlugins.set(runner));

        // Bevy's logger, which a headless app had none of, so its errors went unsaid. The first
        // app of the process sets it, whichever kind it is, as a windowed app does above.
        if crate::log::first() {
            app.add_plugins(crate::log::plugin());
        }
        app.add_plugins(bevy::input::InputPlugin);
        app.add_plugins(bevy::transform::TransformPlugin);
        app.add_plugins(bevy::state::app::StatesPlugin);
        app.add_plugins(asset_plugin(&asset_root));
    }

    let _ = title;

    // Bevy's frame time diagnostics and its log of every diagnostic once a second, where the app
    // asks, as Bevy's stress tests add them, so a program measured beside Bevy's logs by the same
    // code. A windowless app has no diagnostics plugin among its minimal ones, and is given it.
    if config.log_frame_times != 0 {
        if !app.is_plugin_added::<bevy::diagnostic::DiagnosticsPlugin>() {
            app.add_plugins(bevy::diagnostic::DiagnosticsPlugin);
        }
        app.add_plugins((
            bevy::diagnostic::FrameTimeDiagnosticsPlugin::default(),
            bevy::diagnostic::LogDiagnosticsPlugin::default(),
        ));
    }

    // Bevy refuses to allocate a handle for an asset type it has not been told about, and says
    // so by panicking rather than failing the load. `DefaultPlugins` registers these three, so
    // only the minimal profile has to ask.
    //
    // Asking twice is destructive rather than harmless, which is why every bare registration
    // below goes through `init_asset_once` rather than `init_asset`. What that guards against,
    // and what it cost the one time it was not guarded, is written up on the helper.
    if !drawing {
        use bevy::asset::AssetApp;
        use crate::assets::init_asset_once;

        init_asset_once::<bevy::mesh::Mesh>(&mut app);

        // `ImagePlugin` registers the asset type itself, and adds the default and transparent
        // images the renderer's fallbacks rely on.
        app.add_plugins(bevy::image::ImagePlugin::default());

        // An atlas layout is a list of rectangles, so it is data rather than graphics and a
        // windowless run can build one. `SpritePlugin` adds this on the windowed path.
        app.add_plugins(bevy::image::TextureAtlasPlugin);

        // Both plugins above only *pre-register* their loaders; the real registration happens in
        // `RenderPlugin::finish`, which a windowless app never runs. Without this an image load
        // waits for a loader that is never added, and the material bound to it draws untextured.
        app.register_asset_loader(bevy::image::ImageLoader::new(
            bevy::image::CompressedImageFormats::empty(),
        ));
        app.insert_resource(bevy::image::CompressedImageFormatSupport(
            bevy::image::CompressedImageFormats::empty(),
        ));

        // Materials are data as much as meshes are, so a render build that was asked for a
        // headless app still initializes them. Otherwise building one would fail on a bridge
        // that plainly has the renderer, which reads as a bug rather than a configuration.
        #[cfg(feature = "render")]
        init_asset_once::<bevy::pbr::StandardMaterial>(&mut app);

        // The component a standard material is attached by, registered for reflection, which
        // `PbrPlugin` does on the windowed path. It is generic over the material, and a generic type
        // is not one the automatic registration covers, so without this an entity's material could
        // be set here and not read back through reflection.
        #[cfg(feature = "render")]
        app.register_type::<bevy::pbr::MeshMaterial3d<bevy::pbr::StandardMaterial>>();

        // The same for the air a sky is scattered through. The medium is a description rather
        // than a picture, so it is buildable without a window even though nothing draws it.
        // `LightPlugin` registers it on the windowed path.
        #[cfg(feature = "render")]
        init_asset_once::<bevy::light::atmosphere::ScatteringMedium>(&mut app);

        // The same again for an exposure compensation curve, which is a lookup table built from
        // points rather than anything drawn. `AutoExposurePlugin` registers it on the windowed
        // path.
        #[cfg(feature = "render")]
        {
            use bevy::post_process::auto_exposure::AutoExposureCompensationCurve;
            init_asset_once::<AutoExposureCompensationCurve>(&mut app);
        }

        // Loads `.scn` and `.scn.ron`, and spawns any `WorldAsset` an entity points at, as a glTF
        // scene is too. Unlike the two above, this registers its loader in `build`.
        app.add_plugins(bevy::world_serialization::WorldSerializationPlugin);

        // Registers `AudioSource`, its decoders, and the output device if there is one. Bevy
        // tolerates having none, so it logs and plays nothing, which suits a windowless run anyway.
        // Without this a sound load panics rather than failing, because Bevy refuses to hand out a
        // handle for an asset type it was never told about.
        #[cfg(feature = "render")]
        app.add_plugins(bevy::audio::AudioPlugin {
            // One answer for the app, because how far away a sound is depends on what the world
            // is measured in, and that is a fact about the game rather than about any one sound.
            // A sound may still say otherwise for itself.
            default_spatial_scale: if config.spatial_scale > 0.0 {
                bevy::audio::SpatialScale::new(config.spatial_scale)
            } else {
                bevy::audio::SpatialScale::new(1.0)
            },
            ..Default::default()
        });

        // Registers the glTF loader and the asset types it produces. `DefaultPlugins` carries it
        // on the windowed path, so adding it there as well would hit the double-registration
        // described above.
        #[cfg(feature = "render")]
        app.add_plugins(bevy::gltf::GltfPlugin::default());

        // A font is a file like any other, and loading one without a window has to work or the
        // load would panic rather than fail. The asset and its loader are all that is registered
        // here, because the rest of `TextPlugin` is layout and glyph atlases, which belong to
        // drawing.
        #[cfg(feature = "render")]
        {
            use bevy::asset::AssetApp;
            if init_asset_once::<bevy::text::Font>(&mut app) {
                // Paired with the type deliberately. Loaders are appended to a list that is
                // searched from the back, so a second copy would answer for the first, and Bevy
                // would warn that the extensions are claimed twice.
                app.init_asset_loader::<bevy::text::FontLoader>();
            }
        }
    }

    // A range of numbers read and written as JSON, `{"start":..,"end":..}`, which Bevy's
    // reflection registers without its serde data, so a component holding one, as
    // `VisibilityRange` holds two, could be read from C# and not written. Serde has the impls and
    // only the registration is missing.
    {
        use bevy::reflect::{ReflectDeserialize, ReflectSerialize};
        app.register_type::<core::ops::Range<f32>>();
        app.register_type_data::<core::ops::Range<f32>, ReflectSerialize>();
        app.register_type_data::<core::ops::Range<f32>, ReflectDeserialize>();
    }

    // Where the typed-text reader keeps its place between frames.
    app.init_resource::<crate::sync::TextCursor>();

    // A sound checked before Bevy plays it, once Bevy's audio is added on either path.
    #[cfg(feature = "render")]
    crate::audio::checked::install(&mut app);

    // A rate of zero means "leave Bevy's own", which is 64 Hz. A negative or non-finite one is
    // meaningless rather than merely unusual, so it is ignored the same way.
    if config.fixed_hz.is_finite() && config.fixed_hz > 0.0 {
        app.insert_resource(bevy::time::Time::<bevy::time::Fixed>::from_hz(config.fixed_hz));
    }

    // A clock that advances a set amount a frame, from the first, so what a test or a capture
    // measures in frames is the same on every machine. Zero and anything that is not a duration
    // leave Bevy reading the machine's clock.
    if let Some(strategy) = crate::sync::frame_strategy(config.frame_seconds) {
        app.insert_resource(strategy);
    }

    // Pin the orderings that matter between exclusive C# systems.
    //
    // The frame snapshot runs after Bevy has advanced its clocks, or it would report the previous
    // frame's time, because nothing else in `First` orders the two, so without this the schedule is
    // free to run the snapshot first and `ctx.Time` lags by a frame.
    app.configure_sets(
        First,
        (BcsSet::Sync, BcsSet::First)
            .chain()
            .after(bevy::time::TimeSystems),
    );
    app.configure_sets(PostUpdate, (BcsSet::PostUpdate, BcsSet::Flush).chain());
    // `Cleanup` must come after `ExitCheck`, or on the final frame it would look for a pending
    // `AppExit` that has not been written yet and skip, with no later frame to catch it.
    app.configure_sets(
        Last,
        (
            BcsSet::Render,
            BcsSet::Last,
            BcsSet::ExitCheck,
            BcsSet::Cleanup,
        )
            .chain(),
    );

    // The `Cleanup` stage has to run *inside* the loop, on the final frame, because
    // `App::run` swaps the app out of the handle for an empty one before handing it to the
    // runner, so once `run` returns there is no world left to clean up. This system watches
    // for a pending `AppExit` and drains the callbacks on the frame the exit is decided.
    app.add_systems(
        Last,
        (move |world: &mut World| run_cleanup_on_exit(world, &cleanup)).in_set(BcsSet::Cleanup),
    );

    // An asset that will not load is as wrong in a headless run as in a windowed one, and harder
    // to notice there, so the queue exists in every profile.
    app.init_resource::<crate::events::AssetFailures>();

    // A spawned scene is announced to an observer rather than in a queue, so one is kept here to
    // turn each announcement into an entry the managed side collects once a frame.
    app.init_resource::<crate::events::ReadyInstances>();
    app.add_observer(crate::events::instance_ready);

    if config.headless_frames > 0 {
        app.insert_resource(HeadlessFrameLimit {
            remaining: config.headless_frames,
        });
        app.add_systems(Last, tick_frame_limit.in_set(BcsSet::ExitCheck));
    }

    app
}

/// Runs the `Cleanup` callbacks once, on the frame an exit is requested.
fn run_cleanup_on_exit(world: &mut World, cleanup: &CleanupList) {
    if world.contains_resource::<CleanupRan>() {
        return;
    }

    let exiting = world
        .get_resource::<bevy::ecs::message::Messages<AppExit>>()
        .is_some_and(|messages| !messages.is_empty());
    if !exiting {
        return;
    }

    world.insert_resource(CleanupRan);

    // From here a panic on one of Bevy's own threads is the end of a run that was asked to end,
    // as the file watcher's is when the asset server it sends to goes first, and not a crash.
    crate::crash::ending(true);

    // Copy the callbacks out before loaning the world, so the lock is not held across
    // arbitrary managed code that might register more of them.
    let callbacks: Vec<SystemReg> = match cleanup.lock() {
        Ok(guard) => guard.clone(),
        Err(poisoned) => poisoned.into_inner().clone(),
    };

    loan_world(world, || {
        for callback in &callbacks {
            callback.invoke();
        }
    });
}

/// Counts down the headless frame budget and requests exit when it runs out.
fn tick_frame_limit(world: &mut World) {
    let done = match world.get_resource_mut::<HeadlessFrameLimit>() {
        Some(mut limit) => {
            limit.remaining = limit.remaining.saturating_sub(1);
            limit.remaining == 0
        }
        None => false,
    };
    if done {
        world.write_message(AppExit::Success);
    }
}

/// Creates the engine. Returns null on failure; the caller owns the handle.
///
/// # Safety
/// `config` must point to a valid [`BcsConfig`] for the duration of the call.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_app_create(config: *const BcsConfig) -> *mut BcsApp {
    crate::interop::guard_with(core::ptr::null_mut(), || {
        if config.is_null() {
            return core::ptr::null_mut();
        }
        let config = unsafe { *config };
        let title = unsafe { crate::interop::cstr_to_string(config.title) };
        let cleanup: CleanupList = Default::default();

        // The last app's component callbacks, by ids this app gives to components of its own.
        crate::lifecycle::forget();

        // A new app, whose panics are crashes again until it, too, begins ending.
        crate::crash::ending(false);

        let app = build_app(&config, title, cleanup.clone());
        Box::into_raw(Box::new(BcsApp::new(app, cleanup)))
    })
}

/// Releases the engine and everything it owns.
///
/// # Safety
/// `handle` must come from [`bcs_app_create`] and must not be used afterwards.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_app_destroy(handle: *mut BcsApp) {
    crate::interop::guard_with((), || {
        if !handle.is_null() {
            drop(unsafe { Box::from_raw(handle) });
        }

        // Gone with its threads, so a panic after this is no longer the end of its run.
        crate::crash::ending(false);
    });
}

/// Runs the app. Blocks until the window closes or an exit is requested, then runs the
/// `Cleanup` stage. Returns [`status::OK`] on a clean exit.
///
/// # Safety
/// `handle` must be a live app that has not been run before.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn bcs_app_run(handle: *mut BcsApp) -> i32 {
    crate::interop::guard(|| {
        let Some(app) = (unsafe { app_mut(handle) }) else {
            return status::NULL_ARG;
        };
        if app.running {
            return status::ALREADY_RUNNING;
        }
        app.running = true;

        // `App::run` moves the app out of `app.app`, leaving an empty one behind. Everything
        // that needs the real world (the `Cleanup` stage included) has to happen inside the
        // loop, which is why cleanup is a system rather than something done here.
        let exit = app.app.run();

        match exit {
            AppExit::Success => status::OK,
            AppExit::Error(code) => code.get() as i32,
        }
    })
}

/// Requests a graceful shutdown from inside a system callback.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_app_request_exit() -> i32 {
    crate::interop::guard(|| {
        with_world(|world| {
            world.write_message(AppExit::Success);
            status::OK
        })
    })
}

/// Requests a shutdown that ends the run with `code`, Bevy's `AppExit::Error`, or a clean one for
/// zero, from inside a system callback.
///
/// For a run that has to end and say it failed, as a Rust system's panic ends Bevy's app, where a
/// C# system that throws is logged and the app runs on. The run answers the code, a value from one
/// to 255 as a process's exit code is, and anything past 255 is refused.
#[unsafe(no_mangle)]
pub extern "C" fn bcs_app_request_exit_code(code: i32) -> i32 {
    crate::interop::guard(|| {
        let Ok(code) = u8::try_from(code) else {
            return status::INVALID_STATE;
        };

        with_world(|world| {
            world.write_message(match core::num::NonZeroU8::new(code) {
                Some(code) => AppExit::Error(code),
                None => AppExit::Success,
            });
            status::OK
        })
    })
}

/// Opens the window through XWayland on GNOME under Wayland, so the desktop draws its title bar in
/// its own style rather than the window drawing an imitation of an older one.
///
/// winit picks Wayland when `WAYLAND_DISPLAY` is set and X11 otherwise, so taking it out of this
/// process's environment before the event loop is made is the whole of choosing X11. Only when
/// there is an X server to go to (`DISPLAY`), and only on GNOME, the one desktop that leaves a
/// Wayland window to draw its own frame. Said on standard error, so a window that looks different
/// says why.
#[cfg(feature = "render")]
fn prefer_desktop_title_bar() {
    #[cfg(all(unix, not(any(target_os = "macos", target_os = "ios", target_os = "android"))))]
    {
        let desktop = std::env::var("XDG_CURRENT_DESKTOP").unwrap_or_default();
        let gnome = desktop.split(':').any(|part| part.eq_ignore_ascii_case("GNOME"));
        let wayland = std::env::var_os("WAYLAND_DISPLAY").is_some();
        let x11 = std::env::var_os("DISPLAY").is_some();

        if gnome && wayland && x11 {
            // SAFETY: called before the event loop and the renderer start any thread that could
            // read the environment at the same time.
            unsafe { std::env::remove_var("WAYLAND_DISPLAY") };

            // Written straight out, since this runs before Bevy's own logging is set up.
            eprintln!("[bcs] the window opens through XWayland, so GNOME draws its title bar");
        }
    }
}

