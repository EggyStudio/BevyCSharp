//! `bevy_csharp` a C ABI over the [Bevy] engine, consumed by the BevyCSharp NuGet package.
//!
//! # What this is
//!
//! Bevy owns the engine: the ECS world, the scheduler, timing, input, windowing and the renderer.
//! This crate adds no engine of its own. It exposes enough of Bevy through a stable, flat C
//! interface for .NET to drive it:
//!
//! **Components** are registered at runtime from C# struct layouts, using Bevy's
//!   dynamic [`ComponentDescriptor`] support. A `[Behavior]` struct becomes a real Bevy
//!   component with a real `ComponentId`.
//! **Systems** are C# function pointers added to Bevy's `Startup`/`First`/`PreUpdate`/
//!   `Update`/`PostUpdate`/`Last` schedules as exclusive systems.
//! **Iteration** hands C# raw pointers into Bevy's table storage, so a behavior's
//!   per-entity method writes straight into the component column with no marshaling.
//!
//! # Threading
//!
//! A system callback runs on Bevy's main thread with the world loaned to it (see
//! [`state`]). Managed code may fan the per-entity loop out across worker threads, those
//! threads can safely write through the chunk pointers they were handed, but any call
//! that needs the world itself returns [`interop::status::NO_WORLD`], steering structural
//! changes onto the managed command buffer that is applied on the main thread.
//!
//! [Bevy]: https://bevyengine.org
//! [`ComponentDescriptor`]: bevy::ecs::component::ComponentDescriptor

pub mod app;
pub mod capabilities;
pub mod component_registry;
pub mod crash;
pub mod offscreen;
pub mod systems;
pub mod animation;
pub mod audio;
pub mod carried;
pub mod clips;
pub mod assets;
pub mod ecs;
pub mod events;
pub mod focus;
pub mod gamepad;
pub mod gizmos;
pub mod gizmo_settings;
pub mod graphs;
pub mod input;
pub mod input_messages;
pub mod interop;
pub mod interop_render;
pub mod interop_window;
pub mod lifecycle;
pub mod log;
pub mod memory;
pub mod navigation;
pub mod observe;
pub mod pick;
pub mod pointer;
pub mod profile;
pub mod reflected;
pub mod render;
pub mod state;
pub mod stages;
pub mod states;
pub mod skins;
pub mod spawned_windows;
pub mod sync;
pub mod title_bar;
pub mod ui;
pub mod widget;
pub mod window;
pub mod imgui;

/// Version of the C ABI. C# checks this at load time and refuses a mismatch, so a stale
/// native library next to a newer managed assembly fails loudly instead of corrupting memory.
pub const ABI_VERSION: i32 = 231;
