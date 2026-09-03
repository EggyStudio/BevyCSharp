//! `bevy_csharp` a C ABI over the [Bevy] engine, consumed by the BevyCSharp NuGet package.
//!
//! # What this is
//!
//! Bevy owns the engine: the ECS world, the scheduler, timing, input, windowing and the
//! renderer. This crate adds no engine of its own. It exposes just enough of Bevy through
//! a stable, flat C interface for .NET to drive it:
//!
//! **Components** are registered at runtime from C# struct layouts, using Bevy's
//!   dynamic [`ComponentDescriptor`] support. A `[Behavior]` struct becomes a real Bevy
//!   component with a real `ComponentId`.
//! **Systems** are C# function pointers added to Bevy's `Startup`/`First`/`PreUpdate`/
//!   `Update`/`PostUpdate`/`Last` schedules as exclusive systems.
//! **Iteration** hands C# raw pointers into Bevy's table storage, so a behavior's
//!   per-entity method writes straight into the component column with no marshalling.
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
pub mod audio;
pub mod assets;
pub mod ecs;
pub mod events;
pub mod gizmos;
pub mod input;
pub mod interop;
pub mod render;
pub mod state;
pub mod states;
pub mod sync;
pub mod ui;
pub mod window;

/// Version of the C ABI. C# checks this at load time and refuses a mismatch, so a stale
/// native library next to a newer managed assembly fails loudly instead of corrupting memory.
pub const ABI_VERSION: i32 = 40;
