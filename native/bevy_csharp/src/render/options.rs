//! The parts of drawing an app asks for, each added only where it asked, and kept out where the
//! bridge was built without it or the adapter cannot run it.

/// Adds what `config` asks for of meshlets, ray-traced lighting, the weather, GPU timings, a
/// see-through window and wireframes, with auto exposure, the render schedule's clock and the count
/// of the lights drawn, which every app that draws has.
#[cfg(feature = "render")]
pub(crate) fn install(
    app: &mut bevy::app::App,
    config: &crate::interop::BcsConfig,
    offscreen: bool,
    backends: Option<bevy::render::settings::Backends>,
) {
    // Read by meshlets and ray-traced lighting alone, which a build may leave out.
    #[cfg(not(any(feature = "meshlet", feature = "solari")))]
    let _ = backends;

    // Meshlets, if the app asked for them and the bridge was built with them. Added beside
    // the default plugins rather than inside them, because the plugin has to be told its
    // cluster budget and has to be kept out entirely on a GPU that cannot run it.
    #[cfg(feature = "meshlet")]
    crate::render::meshlets::forget();

    #[cfg(feature = "meshlet")]
    if config.meshlet_clusters > 0 {
        let backends = backends.map(|backends| wgpu::Backends::from_bits_truncate(backends.bits()));
        crate::render::meshlets::install(app, config.meshlet_clusters, backends);
    }

    // Ray-traced lighting, likewise asked for and likewise kept out where the adapter
    // cannot trace rays, since adding it makes every material deferred.
    #[cfg(feature = "solari")]
    crate::render::solari::forget();

    #[cfg(feature = "solari")]
    if config.ray_traced_lighting != 0 {
        let backends = backends.map(|backends| wgpu::Backends::from_bits_truncate(backends.bits()));
        crate::render::solari::install(app, backends, config.ray_traced_lighting & 2 != 0);
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
        crate::render::weather::install(app);
    }

    // What the app before this one drew, which would otherwise answer for a camera of
    // this one that happens to reuse its entity number.
    crate::render::watch::forget();

    // Only when asked, since measuring writes timestamps around every pass and reads them
    // back every frame.
    crate::render::timings::forget();

    if config.gpu_timings != 0 {
        crate::render::timings::install(app);
    }

    // Only where a see-through window was asked for, which is the one case where the mode
    // the window asks to be composited in might be one its surface cannot do.
    if config.transparent != 0 && !offscreen {
        crate::window::install(app);
    }

    // Auto exposure is the one post-processing effect `DefaultPlugins` leaves out, since
    // it needs compute shaders and so cannot run everywhere the rest can. Every desktop
    // backend the bridge builds for has them.
    app.add_plugins(bevy::post_process::auto_exposure::AutoExposurePlugin);

    // The render schedule's clock for frame.profile, which does nothing until asked.
    crate::profile::install_render(app);

    // The point lights the render world drew, two numbers a frame, which a stress test reads.
    crate::render::light_counts::install(app);

    // Drawing a mesh as its edges, which an editor offers as a way to outline what is
    // selected, and the same for a 2D mesh, which Bevy draws through its own pipeline and
    // so outlines with its own plugin. Only where the app asked, since each plugin looks at
    // every mesh of its kind in its prepare and queue phases every frame whether or not any
    // is outlined, which at a hundred thousand sprite meshes is 8 ms of the render.
    if config.wireframes != 0 {
        app.add_plugins(bevy::pbr::wireframe::WireframePlugin::default());
        app.add_plugins(bevy::sprite_render::Wireframe2dPlugin::default());
    }
}
