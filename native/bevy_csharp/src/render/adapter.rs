//! Asking the GPU what it can do before the renderer exists.
//!
//! Some of Bevy's plugins need features only some adapters have, and have to be added while the
//! app is built, before Bevy has chosen an adapter, so what a plugin needs is asked of the adapter
//! Bevy is about to choose, on an instance of its own. Meshlets end the process on an adapter
//! without what they need, and ray-traced lighting switches every material to deferred however
//! the adapter answers, so both are added only where the answer is yes.

#![cfg(any(feature = "meshlet", feature = "solari"))]

/// Answers what `needed` has that the adapter Bevy would choose lacks, or `Ok` where it lacks
/// nothing.
///
/// The same choice Bevy makes, by power preference and the environment variables it honors, on
/// `backends` where the app pinned them.
pub fn lacking(backends: Option<wgpu::Backends>, needed: wgpu::Features) -> Result<(), String> {
    let mut descriptor = wgpu::InstanceDescriptor::new_without_display_handle_from_env();

    if let Some(backends) = backends {
        descriptor.backends = backends;
    }

    let instance = wgpu::Instance::new(descriptor);
    let options = wgpu::RequestAdapterOptions {
        power_preference: wgpu::PowerPreference::from_env().unwrap_or(wgpu::PowerPreference::HighPerformance),
        compatible_surface: None,
        force_fallback_adapter: false,
    };

    let adapter = bevy::tasks::block_on(instance.request_adapter(&options)).map_err(|error| error.to_string())?;
    let lacking = needed - adapter.features();

    if lacking.is_empty() {
        Ok(())
    } else {
        Err(format!("{} lacks {lacking:?}", adapter.get_info().name))
    }
}
