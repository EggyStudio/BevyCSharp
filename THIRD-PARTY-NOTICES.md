# Third-party notices

BevyCSharp is under the Mozilla Public License 2.0 (`LICENSE`). Its package carries the library,
its generator and the bridge, a native library built from Bevy and the Rust crates Bevy stands on,
so a game shipped on the package carries them too. Each is named here with its license and the
notices its own files give, so the game can say what it carries.

`build/third-party-notices.py` writes this file from `native/Cargo.lock` through `cargo metadata`,
and NormTests holds it to the lock, so it is written again rather than edited.

## The library's packages

| Package | Used for | License | Source |
|---|---|---|---|
| Twizzle.ImGui-Bundle.NET | Dear ImGui's bindings and its native build, cimgui, for the editor and a game's tools | MIT | https://github.com/JoeTwizzle/ImGui.NET |
| Dear ImGui, inside it | The interface those tools are drawn with | MIT | https://github.com/ocornut/imgui |
| BepuPhysics and BepuUtilities | Rigid bodies, simulated on the managed side | Apache-2.0 | https://github.com/bepu/bepuphysics2 |

A restore fetches them with their own license files. A shader written in Slang is compiled by
`slangc`, which the package does not carry.

## The bridge's crates

Every crate `native/Cargo.lock` names, which is every crate a profile of the bridge compiles for any
system the package is built for, and some that no shipped profile compiles. A crate offered under a
choice of licenses is offered so here as its authors offer it. The copyright column gives the
holders its files name, or else the authors its manifest names, or else the contributors to the
repository or the home its manifest names, and the last column the texts below that its files
hold, or for a crate whose package holds none, the standard text of each license it names.

| Crate | Version | License | Copyright | Texts |
|---|---|---|---|---|
| ab_glyph | 0.2.32 | Apache-2.0 | by Alex Butler &lt;alexheretic@gmail.com&gt; | [1](#text-1) |
| ab_glyph_rasterizer | 0.1.10 | Apache-2.0 | by Alex Butler &lt;alexheretic@gmail.com&gt; | [1](#text-1) |
| accesskit | 0.24.1 | MIT OR Apache-2.0 | by The AccessKit contributors | [4](#text-4), [5](#text-5) |
| accesskit_consumer | 0.35.0 | MIT OR Apache-2.0 | by The AccessKit contributors | [4](#text-4), [5](#text-5) |
| accesskit_consumer | 0.38.0 | MIT OR Apache-2.0 | by The AccessKit contributors | [4](#text-4), [5](#text-5) |
| accesskit_macos | 0.26.3 | MIT OR Apache-2.0 | by The AccessKit contributors | [4](#text-4), [5](#text-5) |
| accesskit_windows | 0.32.1 | MIT OR Apache-2.0 | by The AccessKit contributors | [4](#text-4), [5](#text-5) |
| accesskit_winit | 0.32.2 | Apache-2.0 | by The AccessKit contributors | [5](#text-5) |
| adler2 | 2.0.1 | 0BSD OR MIT OR Apache-2.0 | Copyright (C) Jonas Schievink &lt;jonasschievink@gmail.com&gt; | [2](#text-2), [3](#text-3), [4](#text-4) |
| ahash | 0.8.12 | MIT OR Apache-2.0 | Copyright (c) 2018 Tom Kaitchuck | [5](#text-5), [4](#text-4) |
| aho-corasick | 1.1.5 | Unlicense OR MIT | Copyright (c) 2015 Andrew Gallant | [6](#text-6), [7](#text-7), [8](#text-8) |
| allocator-api2 | 0.2.21 | MIT OR Apache-2.0 | by Zakarum &lt;zaq.dev@icloud.com&gt; | [1](#text-1), [4](#text-4) |
| alsa | 0.11.0 | Apache-2.0/MIT | Copyright (c) 2015-2021 David Henningsson, and other contributors. | [1](#text-1), [9](#text-9) |
| alsa-sys | 0.4.0 | MIT | Copyright (c) 2018 diwic | [9](#text-9) |
| android-activity | 0.6.1 | MIT OR Apache-2.0 | the contributors to https://github.com/rust-mobile/android-activity | [10](#text-10), [1](#text-1), [9](#text-9) |
| android-properties | 0.2.2 | MIT | Copyright (c) 2020 Mikhail Lappo | [9](#text-9) |
| android_log-sys | 0.3.2 | MIT OR Apache-2.0 | Copyright 2016 The android_log_sys Developers<br>Copyright (c) 2016 The android_log_sys Developers | [11](#text-11), [4](#text-4) |
| android_system_properties | 0.1.6 | MIT OR Apache-2.0 | Copyright 2016 Nicolas Silva<br>Copyright (c) 2013 Nicolas Silva<br>COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER | [12](#text-12), [13](#text-13) |
| approx | 0.5.1 | Apache-2.0 | by Brendan Zabarauskas &lt;bjzaba@yahoo.com.au&gt; | [5](#text-5) |
| arboard | 3.6.1 | MIT OR Apache-2.0 | Copyright (c) 2022 The Arboard contributors | [1](#text-1), [9](#text-9) |
| arrayref | 0.3.9 | BSD-2-Clause | Copyright (c) 2015 David Roundy &lt;roundyd@physics.oregonstate.edu&gt; | [14](#text-14) |
| arrayvec | 0.7.8 | MIT OR Apache-2.0 | Copyright (c) Ulrik Sverdrup "bluss" 2015-2023 | [5](#text-5), [4](#text-4) |
| as-raw-xcb-connection | 1.0.1 | MIT OR Apache-2.0 | Copyright 2019 as-raw-xcb-connection Contributers | [5](#text-5), [4](#text-4) |
| ash | 0.38.0+1.3.281 | MIT OR Apache-2.0 | Copyright 2016 Maik Klein<br>Copyright (c) 2016 ASH | [15](#text-15), [4](#text-4) |
| assert_type_match | 0.1.1 | MIT OR Apache-2.0 | Copyright (c) 2024 Gino Valente | [5](#text-5), [9](#text-9) |
| async-broadcast | 0.7.2 | MIT OR Apache-2.0 | Copyright 2020 Yoshua Wuyts<br>Copyright (c) 2020 Yoshua Wuyts | [16](#text-16), [7](#text-7) |
| async-channel | 2.5.0 | Apache-2.0 OR MIT | by Stjepan Glavina &lt;stjepang@gmail.com&gt; | [5](#text-5), [4](#text-4) |
| async-executor | 1.14.0 | Apache-2.0 OR MIT | by Stjepan Glavina &lt;stjepang@gmail.com&gt;, John Nunley &lt;dev@notgull.net&gt; | [5](#text-5), [4](#text-4) |
| async-fs | 2.2.0 | Apache-2.0 OR MIT | by Stjepan Glavina &lt;stjepang@gmail.com&gt; | [5](#text-5), [4](#text-4) |
| async-io | 2.6.0 | Apache-2.0 OR MIT | by Stjepan Glavina &lt;stjepang@gmail.com&gt; | [5](#text-5), [4](#text-4) |
| async-lock | 3.4.2 | Apache-2.0 OR MIT | by Stjepan Glavina &lt;stjepang@gmail.com&gt; | [5](#text-5), [4](#text-4) |
| async-task | 4.7.1 | Apache-2.0 OR MIT | by Stjepan Glavina &lt;stjepang@gmail.com&gt; | [5](#text-5), [4](#text-4) |
| atomic-waker | 1.1.2 | Apache-2.0 OR MIT | Copyright (c) 2016 Alex Crichton<br>Copyright (c) 2017 The Tokio Authors<br>Copyright (c) 2016 Alex Crichton<br>Copyright (c) 2017 The Tokio Authors | [5](#text-5), [4](#text-4), [17](#text-17) |
| atomicow | 1.2.0 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/atomicow | [1](#text-1), [9](#text-9) |
| autocfg | 1.5.1 | Apache-2.0 OR MIT | Copyright (c) 2018 Josh Stone | [5](#text-5), [4](#text-4) |
| base64 | 0.22.1 | MIT OR Apache-2.0 | Copyright (c) 2015 Alice Maz | [5](#text-5), [7](#text-7) |
| bevy | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_a11y | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_android | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_animation | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_animation_macros | 0.19.1 | MIT OR Apache-2.0 | none given | [1](#text-1), [9](#text-9) |
| bevy_anti_alias | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_app | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_asset | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_asset_macros | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_audio | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_camera | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_clipboard | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_color | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_core_pipeline | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_derive | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_diagnostic | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_ecs | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_ecs_macro_logic | 0.19.1 | MIT OR Apache-2.0 | none given | [1](#text-1), [9](#text-9) |
| bevy_ecs_macros | 0.19.1 | MIT OR Apache-2.0 | none given | [1](#text-1), [9](#text-9) |
| bevy_embedded_assets | 0.16.0 | MIT OR Apache-2.0 | by François Mockers &lt;mockersf@gmail.com&gt; | [4](#text-4), [5](#text-5) |
| bevy_encase_derive | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_gilrs | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_gizmos | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_gizmos_macros | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_gizmos_render | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_gltf | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_image | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_input | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_input_focus | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_internal | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_light | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_log | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_macro_utils | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_material | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_material_macros | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_math | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_mesh | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_mikktspace | 1.0.0 | Zlib AND (MIT OR Apache-2.0) | Copyright (c) 2017 The mikktspace Library Developers | [1](#text-1), [4](#text-4) |
| bevy_pbr | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_picking | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_platform | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_post_process | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_ptr | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_reflect | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_reflect_derive | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_render | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_render_macros | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_scene | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_scene_macros | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [4](#text-4), [5](#text-5) |
| bevy_shader | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_solari | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_sprite | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_sprite_render | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_state | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_state_macros | 0.19.1 | MIT OR Apache-2.0 | none given | [1](#text-1), [9](#text-9) |
| bevy_tasks | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_text | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_time | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_transform | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_ui | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_ui_render | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_ui_widgets | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_utils | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_weather | 0.2.0 | MIT OR Apache-2.0 | Copyright (c) 2026 the bevy_weather authors | [9](#text-9) |
| bevy_window | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_winit | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bevy_world_serialization | 0.19.1 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/bevy | [1](#text-1), [9](#text-9) |
| bit-set | 0.9.1 | Apache-2.0 OR MIT | Copyright (c) 2026 The Rust Project Developers | [5](#text-5), [4](#text-4) |
| bit-vec | 0.9.1 | Apache-2.0 OR MIT | Copyright (c) 2023 The Rust Project Developers | [5](#text-5), [4](#text-4) |
| bitflags | 1.3.2 | MIT/Apache-2.0 | Copyright (c) 2014 The Rust Project Developers | [5](#text-5), [4](#text-4) |
| bitflags | 2.13.1 | MIT OR Apache-2.0 | Copyright (c) 2014 The Rust Project Developers | [5](#text-5), [4](#text-4) |
| bitvec | 1.1.1 | MIT | Copyright (c) 2018 myrrlyn (Alexander Payne) | [9](#text-9) |
| blake3 | 1.8.7 | CC0-1.0 OR Apache-2.0 OR Apache-2.0 WITH LLVM-exception | Copyright 2019 Jack O'Connor and Samuel Neves | [18](#text-18), [19](#text-19), [20](#text-20) |
| block2 | 0.5.1 | MIT | by Steven Sheldon, Mads Marquart &lt;mads@marquart.dk&gt; | [4](#text-4) |
| block2 | 0.6.2 | MIT | by Mads Marquart &lt;mads@marquart.dk&gt; | [4](#text-4) |
| blocking | 1.7.0 | Apache-2.0 OR MIT | the contributors to https://github.com/smol-rs/blocking | [5](#text-5), [4](#text-4) |
| bumpalo | 3.20.3 | MIT OR Apache-2.0 | Copyright (c) 2019 Nick Fitzgerald | [5](#text-5), [4](#text-4) |
| bytemuck | 1.25.2 | Zlib OR Apache-2.0 OR MIT | Copyright (c) 2019 Daniel "Lokathor" Gee. | [5](#text-5), [21](#text-21), [22](#text-22) |
| bytemuck_derive | 1.12.0 | Zlib OR Apache-2.0 OR MIT | Copyright (c) 2019 Daniel "Lokathor" Gee. | [5](#text-5), [21](#text-21), [22](#text-22) |
| byteorder | 1.5.0 | Unlicense OR MIT | Copyright (c) 2015 Andrew Gallant | [6](#text-6), [7](#text-7), [8](#text-8) |
| byteorder-lite | 0.1.0 | Unlicense OR MIT | Copyright (c) 2015 Andrew Gallant | [7](#text-7), [8](#text-8) |
| bytes | 1.12.1 | MIT | Copyright (c) 2018 Carl Lerche | [4](#text-4) |
| calloop | 0.13.0 | MIT | Copyright (c) 2018 Victor Berger | [4](#text-4) |
| calloop-wayland-source | 0.3.0 | MIT | Copyright (c) 2023 Kirill Chibisov | [4](#text-4) |
| cargo-emit | 0.2.1 | MIT OR Apache-2.0 | Copyright (c) 2019 Nikolai Vazquez | [5](#text-5), [9](#text-9) |
| cc | 1.4.4 | MIT OR Apache-2.0 | Copyright (c) 2014 Alex Crichton | [5](#text-5), [4](#text-4) |
| cesu8 | 1.1.0 | Apache-2.0/MIT | copyright itself, held by the contributor. | [23](#text-23) |
| cfg-if | 1.0.4 | MIT OR Apache-2.0 | Copyright (c) 2014 Alex Crichton | [5](#text-5), [4](#text-4) |
| cfg_aliases | 0.2.2 | MIT | Copyright (c) 2020 Katharos Technology | [9](#text-9), [24](#text-24) |
| claxon | 0.4.3 | Apache-2.0 | by Ruud van Asseldonk &lt;dev@veniogames.com&gt; | [5](#text-5) |
| clipboard-win | 5.4.1 | BSL-1.0 | by Douman &lt;douman@gmx.se&gt; | [33](#text-33) |
| codespan-reporting | 0.12.0 | Apache-2.0 | by Brendan Zabarauskas &lt;bjzaba@yahoo.com.au&gt; | [5](#text-5) |
| codespan-reporting | 0.13.1 | Apache-2.0 | by Brendan Zabarauskas &lt;bjzaba@yahoo.com.au&gt; | [5](#text-5) |
| combine | 4.6.8 | MIT | Copyright (c) 2015 Markus Westerlind | [7](#text-7) |
| concurrent-queue | 2.5.0 | Apache-2.0 OR MIT | by Stjepan Glavina &lt;stjepang@gmail.com&gt;, Taiki Endo &lt;te316e89@gmail.com&gt;, John Nunley &lt;dev@notgull.net&gt; | [5](#text-5), [4](#text-4) |
| console_error_panic_hook | 0.1.7 | Apache-2.0/MIT | Copyright (c) 2018 Nick Fitzgerald | [5](#text-5), [4](#text-4) |
| const-fnv1a-hash | 1.1.0 | MIT | Copyright (c) 2021 Hindrik Stegenga | [9](#text-9) |
| const_panic | 0.2.17 | Zlib | Copyright (c) 2021 Matias Rodriguez. | [22](#text-22) |
| const_soft_float | 0.1.4 | MIT OR Apache-2.0 | Copyright 2023 Kirk Nickish and https://github.com/823984418<br>Copyright (c) 2023 Kirk Nickish and https://github.com/823984418 | [18](#text-18), [4](#text-4) |
| constant_time_eq | 0.4.2 | CC0-1.0 OR MIT-0 OR Apache-2.0 | by Cesar Eduardo Barros &lt;cesarb@cesarb.eti.br&gt; | [1](#text-1), [20](#text-20), [25](#text-25) |
| constgebra | 0.1.4 | MIT OR Apache-2.0 | the contributors to https://github.com/knickish/constgebra | [4](#text-4), [5](#text-5) |
| convert_case | 0.10.0 | MIT | Copyright (c) 2025 rutrum | [9](#text-9) |
| core-foundation | 0.9.4 | MIT OR Apache-2.0 | Copyright (c) 2012-2013 Mozilla Foundation | [5](#text-5), [4](#text-4) |
| core-foundation-sys | 0.8.7 | MIT OR Apache-2.0 | Copyright (c) 2012-2013 Mozilla Foundation | [5](#text-5), [4](#text-4) |
| core-graphics | 0.23.2 | MIT OR Apache-2.0 | Copyright (c) 2012-2013 Mozilla Foundation | [26](#text-26), [5](#text-5), [4](#text-4) |
| core-graphics-types | 0.1.3 | MIT OR Apache-2.0 | Copyright (c) 2012-2013 Mozilla Foundation | [5](#text-5), [4](#text-4) |
| core_maths | 0.1.1 | MIT | Copyright (c) 2024 Robert Bastian | [9](#text-9) |
| coreaudio-rs | 0.14.2 | MIT/Apache-2.0 | Copyright (c) 2015 | [5](#text-5), [4](#text-4) |
| cpal | 0.17.3 | Apache-2.0 | the contributors to https://github.com/RustAudio/cpal | [5](#text-5) |
| cpufeatures | 0.3.1 | MIT OR Apache-2.0 | Copyright (c) 2020-2026 The RustCrypto Project Developers | [5](#text-5), [4](#text-4) |
| crc32fast | 1.5.1 | MIT OR Apache-2.0 | Copyright (c) 2018 Sam Rijs, Alex Crichton and contributors | [27](#text-27), [9](#text-9) |
| critical-section | 1.2.0 | MIT OR Apache-2.0 | Copyright (c) 2022 The critical-section authors | [5](#text-5), [4](#text-4) |
| crossbeam-channel | 0.5.16 | MIT OR Apache-2.0 | Copyright (c) 2019 The Crossbeam Project Developers<br>COPYRIGHT AND/OR OTHER APPLICABLE LAW. ANY USE OF THE WORK OTHER THAN AS<br>copyright protection under copyright law or other applicable laws.<br>Copyright (c) 2009 The Go Authors. All rights reserved. | [5](#text-5), [7](#text-7), [28](#text-28) |
| crossbeam-queue | 0.3.13 | MIT OR Apache-2.0 | Copyright (c) 2019 The Crossbeam Project Developers | [5](#text-5), [7](#text-7) |
| crossbeam-utils | 0.8.22 | MIT OR Apache-2.0 | Copyright (c) 2019 The Crossbeam Project Developers | [5](#text-5), [7](#text-7) |
| crunchy | 0.2.4 | MIT | Copyright 2017-2023 Eira Fransham. | [7](#text-7) |
| ctrlc | 3.5.2 | MIT/Apache-2.0 | Copyright 2017 CtrlC developers | [18](#text-18), [4](#text-4) |
| cursor-icon | 1.2.0 | MIT OR Apache-2.0 OR Zlib | Copyright 2023 Kirill Chibisov<br>Copyright (c) 2023 Kirill Chibisov | [18](#text-18), [4](#text-4), [22](#text-22) |
| dasp_sample | 0.11.0 | MIT OR Apache-2.0 | by mitchmindtree &lt;mitchell.nordine@gmail.com&gt; | [4](#text-4), [5](#text-5) |
| data-encoding | 2.11.1 | MIT | Copyright (c) 2015-2020 Julien Cretin<br>Copyright (c) 2017-2020 Google Inc. | [7](#text-7) |
| derive_more | 2.1.1 | MIT | Copyright (c) 2016 Jelte Fennema | [7](#text-7) |
| derive_more-impl | 2.1.1 | MIT | Copyright (c) 2016 Jelte Fennema | [7](#text-7) |
| dispatch | 0.2.0 | MIT | by Steven Sheldon | [4](#text-4) |
| dispatch2 | 0.3.1 | Zlib OR Apache-2.0 OR MIT | by Mads Marquart &lt;mads@marquart.dk&gt;, Mary &lt;mary@mary.zone&gt; | [22](#text-22), [5](#text-5), [4](#text-4) |
| displaydoc | 0.2.7 | MIT OR Apache-2.0 | by Jane Lusby &lt;jlusby@yaah.dev&gt; | [5](#text-5), [4](#text-4) |
| disqualified | 1.0.0 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/disqualified | [1](#text-1), [9](#text-9) |
| dlib | 0.5.3 | MIT | Copyright (c) 2015 Victor Berger | [4](#text-4) |
| document-features | 0.2.12 | MIT OR Apache-2.0 | Copyright (c) 2020 Olivier Goffart &lt;ogoffart@sixtyfps.io&gt; | [5](#text-5), [4](#text-4) |
| downcast-rs | 1.2.1 | MIT/Apache-2.0 | Copyright (c) 2020 Ashish Myles and contributors | [5](#text-5), [4](#text-4) |
| downcast-rs | 2.0.2 | MIT OR Apache-2.0 | Copyright (c) 2020 Ashish Myles and contributors | [5](#text-5), [4](#text-4) |
| dpi | 0.1.2 | Apache-2.0 AND MIT | copyright:<br>Copyright © 1993,2004 Sun Microsystems or<br>Copyright © 2003-2011 David Schultz or<br>Copyright © 2003-2009 Steven G. Kargl or<br>Copyright © 2003-2009 Bruce D. Evans or<br>Copyright © 2008 Stephen L. Moshier or<br>Copyright © 2017-2018 Arm Limited | [27](#text-27), [29](#text-29) |
| either | 1.18.0 | MIT OR Apache-2.0 | Copyright (c) 2015 | [5](#text-5), [4](#text-4) |
| encase | 0.12.1 | MIT-0 | the contributors to https://github.com/teoxoy/encase | [30](#text-30) |
| encase_derive | 0.12.1 | MIT-0 | the contributors to https://github.com/teoxoy/encase | [25](#text-25) |
| encase_derive_impl | 0.12.1 | MIT-0 | the contributors to https://github.com/teoxoy/encase | [25](#text-25) |
| encoding_rs | 0.8.35 | (Apache-2.0 OR MIT) AND BSD-3-Clause | Copyright Mozilla Foundation<br>Copyright © WHATWG (Apple, Google, Mozilla, Microsoft). | [31](#text-31), [5](#text-5), [4](#text-4), [32](#text-32) |
| equivalent | 1.0.2 | Apache-2.0 OR MIT | Copyright (c) 2016--2023 | [5](#text-5), [4](#text-4) |
| erased-serde | 0.4.10 | MIT OR Apache-2.0 | by David Tolnay &lt;dtolnay@gmail.com&gt; | [1](#text-1), [4](#text-4) |
| errno | 0.3.14 | MIT OR Apache-2.0 | Copyright (c) 2014 Chris Wong | [5](#text-5), [4](#text-4) |
| error-code | 3.4.0 | BSL-1.0 | by Douman &lt;douman@gmx.se&gt; | [33](#text-33) |
| euclid | 0.22.14 | MIT OR Apache-2.0 | Copyright (c) 2012-2013 Mozilla Foundation | [26](#text-26), [5](#text-5), [4](#text-4) |
| event-listener | 5.4.2 | Apache-2.0 OR MIT | by Stjepan Glavina &lt;stjepang@gmail.com&gt;, John Nunley &lt;dev@notgull.net&gt; | [5](#text-5), [4](#text-4) |
| event-listener-strategy | 0.5.4 | Apache-2.0 OR MIT | by John Nunley &lt;dev@notgull.net&gt; | [5](#text-5), [4](#text-4) |
| fastrand | 2.5.0 | Apache-2.0 OR MIT | by Stjepan Glavina &lt;stjepang@gmail.com&gt; | [5](#text-5), [4](#text-4) |
| fdeflate | 0.3.7 | MIT OR Apache-2.0 | by The image-rs Developers | [1](#text-1), [9](#text-9) |
| file-id | 0.2.3 | MIT OR Apache-2.0 | Copyright 2023 Notify Contributors<br>Copyright (c) 2023 Notify Contributors | [18](#text-18), [4](#text-4) |
| find-msvc-tools | 0.1.11 | MIT OR Apache-2.0 | Copyright (c) 2014 Alex Crichton | [5](#text-5), [4](#text-4) |
| fixedbitset | 0.5.7 | MIT OR Apache-2.0 | Copyright (c) 2015-2017 | [5](#text-5), [4](#text-4) |
| flate2 | 1.1.10 | MIT OR Apache-2.0 | Copyright (c) 2014-2026 Alex Crichton | [5](#text-5), [4](#text-4) |
| float-cmp | 0.10.0 | MIT | Copyright (c) 2014-2020 Optimal Computing (NZ) Ltd | [4](#text-4) |
| fnv | 1.0.7 | Apache-2.0 / MIT | Copyright (c) 2017 Contributors | [5](#text-5), [4](#text-4) |
| foldhash | 0.1.5 | Zlib | Copyright (c) 2024 Orson Peters | [22](#text-22) |
| foldhash | 0.2.0 | Zlib | Copyright (c) 2024 Orson Peters | [22](#text-22) |
| font-types | 0.11.3 | MIT OR Apache-2.0 | Copyright 2019 Colin Rothfels<br>Copyright (c) 2019 Colin Rothfels | [15](#text-15), [4](#text-4) |
| font-types | 0.12.4 | MIT OR Apache-2.0 | Copyright 2019 Fontations Developers<br>Copyright (c) 2019 Fontations Developers | [15](#text-15), [4](#text-4) |
| fontique | 0.9.0 | Apache-2.0 OR MIT | Copyright 2024 the Parley Authors | [5](#text-5), [4](#text-4) |
| foreign-types | 0.5.0 | MIT/Apache-2.0 | Copyright (c) 2017 The foreign-types Developers | [27](#text-27), [4](#text-4) |
| foreign-types-macros | 0.2.4 | MIT/Apache-2.0 | Copyright (c) 2017 The foreign-types Developers | [27](#text-27), [4](#text-4) |
| foreign-types-shared | 0.3.1 | MIT/Apache-2.0 | Copyright (c) 2017 The foreign-types Developers | [27](#text-27), [4](#text-4) |
| fsevent-sys | 4.1.0 | MIT | Copyright (c) 2015 Pierre Baillet | [7](#text-7) |
| funty | 2.0.0 | MIT | Copyright (c) 2020 myrrlyn (Alexander Payne) | [9](#text-9) |
| futures-channel | 0.3.34 | MIT OR Apache-2.0 | Copyright (c) 2016 Alex Crichton<br>Copyright (c) 2017 The Tokio Authors | [18](#text-18), [4](#text-4) |
| futures-core | 0.3.34 | MIT OR Apache-2.0 | Copyright (c) 2016 Alex Crichton<br>Copyright (c) 2017 The Tokio Authors | [18](#text-18), [4](#text-4) |
| futures-io | 0.3.34 | MIT OR Apache-2.0 | Copyright (c) 2016 Alex Crichton<br>Copyright (c) 2017 The Tokio Authors | [18](#text-18), [4](#text-4) |
| futures-lite | 2.6.1 | Apache-2.0 OR MIT | Copyright (c) 2016 Alex Crichton<br>Copyright (c) 2017 The Tokio Authors<br>Copyright (c) 2016 Alex Crichton<br>Copyright (c) 2017 The Tokio Authors | [5](#text-5), [4](#text-4), [17](#text-17) |
| futures-macro | 0.3.34 | MIT OR Apache-2.0 | Copyright (c) 2016 Alex Crichton<br>Copyright (c) 2017 The Tokio Authors | [18](#text-18), [4](#text-4) |
| futures-task | 0.3.34 | MIT OR Apache-2.0 | Copyright (c) 2016 Alex Crichton<br>Copyright (c) 2017 The Tokio Authors | [18](#text-18), [4](#text-4) |
| futures-util | 0.3.34 | MIT OR Apache-2.0 | Copyright (c) 2016 Alex Crichton<br>Copyright (c) 2017 The Tokio Authors | [18](#text-18), [4](#text-4) |
| gethostname | 1.1.0 | Apache-2.0 | by Sebastian Wiesner &lt;sebastian@swsnr.de&gt; | [5](#text-5) |
| getrandom | 0.3.4 | MIT OR Apache-2.0 | Copyright (c) 2018-2025 The rust-random Project Developers<br>Copyright (c) 2014 The Rust Project Developers | [34](#text-34), [4](#text-4) |
| getrandom | 0.4.3 | MIT OR Apache-2.0 | Copyright (c) 2018-2026 The rust-random Project Developers<br>Copyright (c) 2014 The Rust Project Developers | [34](#text-34), [4](#text-4) |
| gilrs | 0.11.2 | Apache-2.0/MIT | Copyright (C) 1997-2025 Sam Lantinga &lt;slouken@libsdl.org&gt; | [22](#text-22) |
| gilrs-core | 0.6.8 | Apache-2.0/MIT | by Mateusz Sieczko &lt;arvamer@gmail.com&gt; | [5](#text-5), [4](#text-4) |
| glam | 0.32.1 | MIT OR Apache-2.0 | Copyright 2020 Cameron Hart | [18](#text-18), [4](#text-4) |
| gltf | 1.4.1 | MIT OR Apache-2.0 | Copyright (c) 2017 The gltf Library Developers | [5](#text-5), [4](#text-4) |
| gltf-derive | 1.4.1 | MIT OR Apache-2.0 | Copyright (c) 2016 Vincent Prouillet | [5](#text-5), [35](#text-35) |
| gltf-json | 1.4.1 | MIT OR Apache-2.0 | Copyright (c) 2017 The gltf Library Developers | [5](#text-5), [4](#text-4) |
| gpu-allocator | 0.28.0 | MIT OR Apache-2.0 | Copyright 2021 Traverse Research B.V.<br>Copyright (c) 2021 Traverse Research B.V. | [18](#text-18), [4](#text-4) |
| gpu-descriptor | 0.3.2 | MIT OR Apache-2.0 | by Zakarum &lt;zakarumych@ya.ru&gt; | [4](#text-4), [5](#text-5) |
| gpu-descriptor-types | 0.2.0 | MIT OR Apache-2.0 | by Zakarum &lt;zakarumych@ya.ru&gt; | [4](#text-4), [5](#text-5) |
| grid | 1.0.1 | MIT | Copyright (c) 2020 Armin Becher | [9](#text-9) |
| guillotiere | 0.6.2 | MIT/Apache-2.0 | Copyright (c) 2019 Nicolas Silva | [9](#text-9) |
| half | 2.7.1 | MIT OR Apache-2.0 | by Kathryn Long &lt;squeeself@gmail.com&gt; | [1](#text-1), [9](#text-9) |
| harfrust | 0.6.2 | MIT | Copyright (c) HarfBuzz developers<br>Copyright (c) 2020 Yevhenii Reizner | [7](#text-7) |
| hash32 | 0.3.1 | MIT OR Apache-2.0 | Copyright (c) 2018 Jorge Aparicio | [5](#text-5), [4](#text-4) |
| hashbrown | 0.15.5 | MIT OR Apache-2.0 | Copyright (c) 2016 Amanieu d'Antras | [5](#text-5), [4](#text-4) |
| hashbrown | 0.16.1 | MIT OR Apache-2.0 | Copyright (c) 2016 Amanieu d'Antras | [5](#text-5), [4](#text-4) |
| hashbrown | 0.17.1 | MIT OR Apache-2.0 | Copyright (c) 2016 Amanieu d'Antras | [5](#text-5), [4](#text-4) |
| heapless | 0.9.3 | MIT OR Apache-2.0 | Copyright (c) 2017 Jorge Aparicio | [5](#text-5), [4](#text-4) |
| hermit-abi | 0.5.2 | MIT OR Apache-2.0 | by Stefan Lankes | [5](#text-5), [4](#text-4) |
| hexasphere | 18.0.0 | MIT OR Apache-2.0 | by OptimisticPeach &lt;patrikbuhring@gmail.com&gt; | [1](#text-1), [4](#text-4) |
| hexf-parse | 0.2.1 | CC0-1.0 | by Kang Seonghoon &lt;public+rust@mearie.org&gt; | [20](#text-20) |
| hound | 3.5.1 | Apache-2.0 | by Ruud van Asseldonk &lt;dev@veniogames.com&gt; | [5](#text-5) |
| icu_collections | 2.3.0 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| icu_locale_core | 2.3.0 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| icu_locale_fallback | 2.3.0 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| icu_locale_fallback_data | 2.3.0 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| icu_normalizer | 2.3.0 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| icu_normalizer_data | 2.3.0 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| icu_properties | 2.3.0 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| icu_properties_data | 2.3.0 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| icu_provider | 2.3.1 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| icu_segmenter | 2.3.0 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| icu_segmenter_data | 2.3.0 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| image | 0.25.6 | MIT OR Apache-2.0 | by The image-rs Developers | [1](#text-1), [9](#text-9) |
| image-webp | 0.2.4 | MIT OR Apache-2.0 | the contributors to https://github.com/image-rs/image-webp | [1](#text-1), [9](#text-9) |
| indexmap | 2.14.1 | Apache-2.0 OR MIT | Copyright (c) 2016--2017 | [5](#text-5), [4](#text-4) |
| inflections | 1.1.1 | MIT | Copyright (c) 2016 Caleb Meredith | [4](#text-4) |
| inotify | 0.11.5 | ISC | Copyright (c) Hanno Braun and contributors | [37](#text-37) |
| inotify-sys | 0.1.8 | ISC | Copyright (c) Hanno Braun and contributors | [37](#text-37) |
| inventory | 0.3.24 | MIT OR Apache-2.0 | by David Tolnay &lt;dtolnay@gmail.com&gt; | [1](#text-1), [4](#text-4) |
| itertools | 0.14.0 | MIT OR Apache-2.0 | Copyright (c) 2015 | [5](#text-5), [4](#text-4) |
| itoa | 1.0.18 | MIT OR Apache-2.0 | by David Tolnay &lt;dtolnay@gmail.com&gt; | [1](#text-1), [4](#text-4) |
| jni | 0.21.1 | MIT/Apache-2.0 | Copyright (c) 2016 Prevoty, Inc. and jni-rs contributors | [5](#text-5), [7](#text-7) |
| jni | 0.22.4 | MIT OR Apache-2.0 | by jni team | [4](#text-4), [5](#text-5) |
| jni-macros | 0.22.4 | MIT OR Apache-2.0 | the contributors to https://github.com/jni-rs/jni-rs | [4](#text-4), [5](#text-5) |
| jni-sys | 0.3.1 | MIT OR Apache-2.0 | Copyright (c) 2015 The rust-jni-sys Developers | [27](#text-27), [4](#text-4) |
| jni-sys | 0.4.1 | MIT OR Apache-2.0 | Copyright (c) 2015 The rust-jni-sys Developers | [27](#text-27), [4](#text-4) |
| jni-sys-macros | 0.4.1 | MIT OR Apache-2.0 | by Robert Bragg &lt;robert@sixbynine.org&gt; | [4](#text-4), [5](#text-5) |
| jobserver | 0.1.35 | MIT OR Apache-2.0 | Copyright (c) 2014 Alex Crichton | [5](#text-5), [4](#text-4) |
| js-sys | 0.3.104 | MIT OR Apache-2.0 | Copyright (c) 2014 Alex Crichton | [5](#text-5), [4](#text-4) |
| kqueue | 1.2.1 | MIT | Copyright (c) 2016 William Orr &lt;will@worrbase.com&gt; | [4](#text-4) |
| kqueue-sys | 1.1.2 | MIT | Copyright (c) 2016 William Orr &lt;will@worrbase.com&gt; | [4](#text-4) |
| ktx2 | 0.5.0 | Apache-2.0 | Copyright 2021 The BVE-Reborn Developers | [18](#text-18) |
| lazy_static | 1.5.0 | MIT OR Apache-2.0 | Copyright (c) 2010 The Rust Project Developers | [5](#text-5), [4](#text-4) |
| lewton | 0.10.2 | MIT OR Apache-2.0 | Copyright (c) 2016 est31 &lt;MTest31@outlook.com&gt; and contributors<br>Copyright (c) 2016 est31 &lt;MTest31@outlook.com&gt; and contributors | [38](#text-38) |
| libc | 0.2.189 | MIT OR Apache-2.0 | Copyright (c) The Rust Project Developers | [1](#text-1), [4](#text-4) |
| libloading | 0.8.9 | ISC | Copyright © 2015, Simonas Kazlauskas | [37](#text-37) |
| libm | 0.2.16 | MIT | copyright:<br>Copyright © 1993,2004 Sun Microsystems or<br>Copyright © 2003-2011 David Schultz or<br>Copyright © 2003-2009 Steven G. Kargl or<br>Copyright © 2003-2009 Bruce D. Evans or<br>Copyright © 2008 Stephen L. Moshier or<br>Copyright © 2017-2018 Arm Limited | [39](#text-39) |
| libredox | 0.1.21 | MIT | Copyright (c) 2023 4lDO2 | [9](#text-9) |
| libudev-sys | 0.1.4 | MIT | Copyright (c) 2015 David Cuddeback | [4](#text-4) |
| linebender_resource_handle | 0.1.1 | Apache-2.0 OR MIT | the contributors to https://github.com/linebender/raw_resource_handle | [1](#text-1), [9](#text-9) |
| linux-raw-sys | 0.4.15 | Apache-2.0 WITH LLVM-exception OR Apache-2.0 OR MIT | by Dan Gohman &lt;dev@sunfishcode.online&gt; | [40](#text-40), [5](#text-5), [41](#text-41), [4](#text-4) |
| linux-raw-sys | 0.12.1 | Apache-2.0 WITH LLVM-exception OR Apache-2.0 OR MIT | by Dan Gohman &lt;dev@sunfishcode.online&gt; | [40](#text-40), [5](#text-5), [41](#text-41), [4](#text-4) |
| litemap | 0.8.3 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| litrs | 1.0.0 | MIT OR Apache-2.0 | Copyright (c) 2020 Project Developers | [1](#text-1), [4](#text-4) |
| lock_api | 0.4.14 | MIT OR Apache-2.0 | Copyright (c) 2016 The Rust Project Developers | [5](#text-5), [4](#text-4) |
| log | 0.4.34 | MIT OR Apache-2.0 | Copyright (c) 2014 The Rust Project Developers | [5](#text-5), [4](#text-4) |
| lz4_flex | 0.13.1 | MIT | Copyright (c) 2020 Pascal Seitz<br>COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER | [13](#text-13) |
| mach2 | 0.5.0 | BSD-2-Clause OR MIT OR Apache-2.0 | Copyright (c) 2019 Nick Fitzgerald, 2021 Yuki Okushi | [1](#text-1), [14](#text-14), [4](#text-4) |
| matchers | 0.2.0 | MIT | Copyright (c) 2019 Eliza Weisman | [4](#text-4) |
| memchr | 2.8.3 | Unlicense OR MIT | Copyright (c) 2015 Andrew Gallant | [6](#text-6), [7](#text-7), [8](#text-8) |
| memmap2 | 0.9.11 | MIT OR Apache-2.0 | Copyright [2015] [Dan Burkert]<br>Copyright (c) 2020 Yevhenii Reizner<br>Copyright (c) 2015 Dan Burkert | [18](#text-18), [4](#text-4) |
| meshopt | 0.6.2 | MIT OR Apache-2.0 | by Graham Wihlidal &lt;graham@wihlidal.ca&gt; | [4](#text-4), [5](#text-5) |
| metis | 0.2.2 | MIT OR Apache-2.0 | Copyright 2021 CEA and Contributors<br>Copyright (c) 2021 CEA and Contributors | [18](#text-18), [9](#text-9) |
| metis-sys | 0.3.2 | MIT OR Apache-2.0 | Copyright 1995-2018, Regents of the University of Minnesota<br>Copyright 1995-2013, Regents of the University of Minnesota | [42](#text-42) |
| miniz_oxide | 0.8.9 | MIT OR Zlib OR Apache-2.0 | Copyright 2013-2014 RAD Game Tools and Valve Software<br>Copyright 2010-2014 Rich Geldreich and Tenacious Software LLC<br>Copyright (c) 2017 Frommi<br>Copyright (c) 2017-2024 oyvindln<br>Copyright (c) 2020 Frommi | [9](#text-9), [1](#text-1), [22](#text-22) |
| miniz_oxide | 0.9.1 | MIT OR Zlib OR Apache-2.0 | Copyright 2013-2014 RAD Game Tools and Valve Software<br>Copyright 2010-2014 Rich Geldreich and Tenacious Software LLC<br>Copyright (c) 2017 Frommi<br>Copyright (c) 2017-2024 oyvindln<br>Copyright (c) 2020 Frommi | [9](#text-9), [1](#text-1), [22](#text-22) |
| mio | 1.2.3 | MIT | Copyright (c) 2014 Carl Lerche and other MIO contributors | [4](#text-4) |
| naga | 29.0.4 | MIT OR Apache-2.0 | Copyright (c) 2025 The gfx-rs developers | [1](#text-1), [9](#text-9) |
| naga_oil | 0.22.0 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/naga_oil/ | [1](#text-1), [9](#text-9) |
| ndk | 0.9.0 | MIT OR Apache-2.0 | by The Rust Mobile contributors | [4](#text-4), [5](#text-5) |
| ndk-context | 0.1.1 | MIT OR Apache-2.0 | by The Rust Windowing contributors | [4](#text-4), [5](#text-5) |
| ndk-sys | 0.6.0+11769913 | MIT OR Apache-2.0 | by The Rust Windowing contributors | [4](#text-4), [5](#text-5) |
| nix | 0.31.3 | MIT | Copyright (c) 2015 Carl Lerche + nix-rust Authors | [7](#text-7) |
| nom | 8.0.0 | MIT | Copyright (c) 2014-2019 Geoffroy Couprie | [4](#text-4) |
| nonmax | 0.5.5 | MIT OR Apache-2.0 | Copyright (c) 2020 Lucien Greathouse | [43](#text-43), [4](#text-4) |
| notify | 8.2.0 | CC0-1.0 | by Félix Saparelli &lt;me@passcod.name&gt;, Daniel Faust &lt;hessijames@gmail.com&gt;, Aron Heinecke &lt;Ox0p54r36@t-online.de&gt; | [44](#text-44) |
| notify-debouncer-full | 0.7.0 | MIT OR Apache-2.0 | Copyright 2023 Notify Contributors<br>Copyright (c) 2023 Notify Contributors | [18](#text-18), [4](#text-4) |
| notify-types | 2.1.0 | MIT OR Apache-2.0 | Copyright 2023 Notify Contributors<br>Copyright (c) 2023 Notify Contributors | [18](#text-18), [4](#text-4) |
| nu-ansi-term | 0.50.3 | MIT | Copyright (c) 2014 Benjamin Sago<br>Copyright (c) 2021-2022 The Nushell Project Developers | [7](#text-7) |
| num-bigint | 0.4.8 | MIT OR Apache-2.0 | Copyright (c) 2014 The Rust Project Developers | [5](#text-5), [4](#text-4) |
| num-derive | 0.4.2 | MIT OR Apache-2.0 | Copyright (c) 2014 The Rust Project Developers | [5](#text-5), [4](#text-4) |
| num-integer | 0.1.47 | MIT OR Apache-2.0 | Copyright (c) 2014 The Rust Project Developers | [5](#text-5), [4](#text-4) |
| num-rational | 0.4.2 | MIT OR Apache-2.0 | Copyright (c) 2014 The Rust Project Developers | [5](#text-5), [4](#text-4) |
| num-traits | 0.2.19 | MIT OR Apache-2.0 | Copyright (c) 2014 The Rust Project Developers | [5](#text-5), [4](#text-4) |
| num_enum | 0.7.6 | BSD-3-Clause OR MIT OR Apache-2.0 | Copyright (c) 2018, Daniel Wagner-Hall | [1](#text-1), [45](#text-45), [4](#text-4) |
| num_enum_derive | 0.7.6 | BSD-3-Clause OR MIT OR Apache-2.0 | Copyright (c) 2018, Daniel Wagner-Hall | [1](#text-1), [45](#text-45), [4](#text-4) |
| objc-sys | 0.3.5 | MIT | by Mads Marquart &lt;mads@marquart.dk&gt; | [4](#text-4) |
| objc2 | 0.5.2 | MIT | by Steven Sheldon, Mads Marquart &lt;mads@marquart.dk&gt; | [4](#text-4) |
| objc2 | 0.6.4 | MIT | by Mads Marquart &lt;mads@marquart.dk&gt; | [4](#text-4) |
| objc2-app-kit | 0.2.2 | MIT | the contributors to https://github.com/madsmtm/objc2 | [4](#text-4) |
| objc2-app-kit | 0.3.2 | Zlib OR Apache-2.0 OR MIT | the contributors to https://github.com/madsmtm/objc2 | [22](#text-22), [5](#text-5), [4](#text-4) |
| objc2-audio-toolbox | 0.3.2 | Zlib OR Apache-2.0 OR MIT | the contributors to https://github.com/madsmtm/objc2 | [22](#text-22), [5](#text-5), [4](#text-4) |
| objc2-avf-audio | 0.3.2 | Zlib OR Apache-2.0 OR MIT | the contributors to https://github.com/madsmtm/objc2 | [22](#text-22), [5](#text-5), [4](#text-4) |
| objc2-cloud-kit | 0.2.2 | MIT | the contributors to https://github.com/madsmtm/objc2 | [4](#text-4) |
| objc2-contacts | 0.2.2 | MIT | the contributors to https://github.com/madsmtm/objc2 | [4](#text-4) |
| objc2-core-audio | 0.3.2 | Zlib OR Apache-2.0 OR MIT | the contributors to https://github.com/madsmtm/objc2 | [22](#text-22), [5](#text-5), [4](#text-4) |
| objc2-core-audio-types | 0.3.2 | Zlib OR Apache-2.0 OR MIT | the contributors to https://github.com/madsmtm/objc2 | [22](#text-22), [5](#text-5), [4](#text-4) |
| objc2-core-data | 0.2.2 | MIT | the contributors to https://github.com/madsmtm/objc2 | [4](#text-4) |
| objc2-core-foundation | 0.3.2 | Zlib OR Apache-2.0 OR MIT | the contributors to https://github.com/madsmtm/objc2 | [22](#text-22), [5](#text-5), [4](#text-4) |
| objc2-core-graphics | 0.3.2 | Zlib OR Apache-2.0 OR MIT | the contributors to https://github.com/madsmtm/objc2 | [22](#text-22), [5](#text-5), [4](#text-4) |
| objc2-core-image | 0.2.2 | MIT | the contributors to https://github.com/madsmtm/objc2 | [4](#text-4) |
| objc2-core-location | 0.2.2 | MIT | the contributors to https://github.com/madsmtm/objc2 | [4](#text-4) |
| objc2-encode | 4.1.0 | MIT | by Mads Marquart &lt;mads@marquart.dk&gt; | [4](#text-4) |
| objc2-foundation | 0.2.2 | MIT | the contributors to https://github.com/madsmtm/objc2 | [4](#text-4) |
| objc2-foundation | 0.3.2 | MIT | the contributors to https://github.com/madsmtm/objc2 | [4](#text-4) |
| objc2-io-kit | 0.3.2 | Zlib OR Apache-2.0 OR MIT | the contributors to https://github.com/madsmtm/objc2 | [22](#text-22), [5](#text-5), [4](#text-4) |
| objc2-io-surface | 0.3.2 | Zlib OR Apache-2.0 OR MIT | the contributors to https://github.com/madsmtm/objc2 | [22](#text-22), [5](#text-5), [4](#text-4) |
| objc2-link-presentation | 0.2.2 | MIT | the contributors to https://github.com/madsmtm/objc2 | [4](#text-4) |
| objc2-metal | 0.2.2 | MIT | the contributors to https://github.com/madsmtm/objc2 | [4](#text-4) |
| objc2-metal | 0.3.2 | Zlib OR Apache-2.0 OR MIT | the contributors to https://github.com/madsmtm/objc2 | [22](#text-22), [5](#text-5), [4](#text-4) |
| objc2-quartz-core | 0.2.2 | MIT | the contributors to https://github.com/madsmtm/objc2 | [4](#text-4) |
| objc2-quartz-core | 0.3.2 | Zlib OR Apache-2.0 OR MIT | the contributors to https://github.com/madsmtm/objc2 | [22](#text-22), [5](#text-5), [4](#text-4) |
| objc2-symbols | 0.2.2 | MIT | the contributors to https://github.com/madsmtm/objc2 | [4](#text-4) |
| objc2-ui-kit | 0.2.2 | MIT | the contributors to https://github.com/madsmtm/objc2 | [4](#text-4) |
| objc2-uniform-type-identifiers | 0.2.2 | MIT | the contributors to https://github.com/madsmtm/objc2 | [4](#text-4) |
| objc2-user-notifications | 0.2.2 | MIT | the contributors to https://github.com/madsmtm/objc2 | [4](#text-4) |
| offset-allocator | 0.2.0 | MIT | Copyright (c) 2023 Sebastian Aaltonen, Patrick Walton | [9](#text-9) |
| ogg | 0.8.0 | BSD-3-Clause | Copyright (c) 2016-2017 est31 &lt;MTest31@outlook.com&gt; and contributors<br>Copyright (c) 2002-2015 Xiph.org Foundation | [46](#text-46) |
| once_cell | 1.21.4 | MIT OR Apache-2.0 | by Aleksey Kladov &lt;aleksey.kladov@gmail.com&gt; | [5](#text-5), [4](#text-4) |
| orbclient | 0.3.55 | MIT | Copyright (c) 2015-2019 Jeremy Soller<br>Copyright (C) 1989, 1991 Free Software Foundation, Inc., | [7](#text-7), [47](#text-47) |
| ordered-float | 5.5.0 | MIT | Copyright (c) 2015 Jonathan Reem | [4](#text-4) |
| os_pipe | 1.2.3 | MIT | by Jack O'Connor | [7](#text-7) |
| owned_ttf_parser | 0.25.1 | Apache-2.0 | by Alex Butler &lt;alexheretic@gmail.com&gt; | [1](#text-1) |
| parking | 2.2.1 | Apache-2.0 OR MIT | Copyright 2014-2020 The Rust Project Developers | [5](#text-5), [4](#text-4), [48](#text-48) |
| parking_lot | 0.12.5 | MIT OR Apache-2.0 | Copyright (c) 2016 The Rust Project Developers | [5](#text-5), [4](#text-4) |
| parking_lot_core | 0.9.12 | MIT OR Apache-2.0 | Copyright (c) 2016 The Rust Project Developers | [5](#text-5), [4](#text-4) |
| parlance | 0.1.0 | Apache-2.0 OR MIT | Copyright 2020 the Parley Authors | [5](#text-5), [4](#text-4) |
| parley | 0.9.0 | Apache-2.0 OR MIT | Copyright 2020 the Parley Authors | [5](#text-5), [4](#text-4) |
| parley_data | 0.9.0 | Apache-2.0 OR MIT | Copyright 2020 the Parley Authors | [5](#text-5), [4](#text-4) |
| percent-encoding | 2.3.2 | MIT OR Apache-2.0 | Copyright (c) 2013-2025 The rust-url developers | [5](#text-5), [4](#text-4) |
| petgraph | 0.8.3 | MIT OR Apache-2.0 | Copyright (c) 2015 | [5](#text-5), [4](#text-4), [49](#text-49) |
| pin-project | 1.1.13 | Apache-2.0 OR MIT | the contributors to https://github.com/taiki-e/pin-project | [1](#text-1), [4](#text-4) |
| pin-project-internal | 1.1.13 | Apache-2.0 OR MIT | the contributors to https://github.com/taiki-e/pin-project | [1](#text-1), [4](#text-4) |
| pin-project-lite | 0.2.17 | Apache-2.0 OR MIT | the contributors to https://github.com/taiki-e/pin-project-lite | [1](#text-1), [4](#text-4) |
| piper | 0.2.5 | MIT OR Apache-2.0 | by Stjepan Glavina &lt;stjepang@gmail.com&gt;, John Nunley &lt;dev@notgull.net&gt; | [5](#text-5), [4](#text-4) |
| pkg-config | 0.3.34 | MIT OR Apache-2.0 | Copyright (c) 2014 Alex Crichton | [5](#text-5), [4](#text-4) |
| plain | 0.2.3 | MIT/Apache-2.0 | Copyright (c) 2017 Plain contributors | [5](#text-5), [4](#text-4) |
| png | 0.17.16 | MIT OR Apache-2.0 | Copyright (c) 2015 nwin | [5](#text-5), [4](#text-4) |
| polling | 3.11.0 | Apache-2.0 OR MIT | by Stjepan Glavina &lt;stjepang@gmail.com&gt;, John Nunley &lt;dev@notgull.net&gt; | [5](#text-5), [4](#text-4) |
| portable-atomic | 1.15.0 | Apache-2.0 OR MIT | the contributors to https://github.com/taiki-e/portable-atomic | [1](#text-1), [4](#text-4) |
| portable-atomic-util | 0.2.7 | Apache-2.0 OR MIT | the contributors to https://github.com/taiki-e/portable-atomic-util | [1](#text-1), [4](#text-4) |
| potential_utf | 0.1.6 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| pp-rs | 0.2.1 | BSD-3-Clause | Copyright (c) 2020, Corentin Wallez | [50](#text-50) |
| presser | 0.3.1 | MIT OR Apache-2.0 | Copyright (c) 2019 Embark Studios | [5](#text-5), [4](#text-4) |
| proc-macro-crate | 3.5.0 | MIT OR Apache-2.0 | by Bastian Köcher &lt;git@kchr.de&gt; | [3](#text-3), [4](#text-4) |
| proc-macro2 | 1.0.107 | MIT OR Apache-2.0 | by David Tolnay &lt;dtolnay@gmail.com&gt;, Alex Crichton &lt;alex@alexcrichton.com&gt; | [1](#text-1), [4](#text-4) |
| profiling | 1.0.18 | MIT OR Apache-2.0 | by Philip Degarmo &lt;aclysma@gmail.com&gt; | [4](#text-4), [5](#text-5) |
| quick-error | 2.0.1 | MIT/Apache-2.0 | Copyright (c) 2015 The quick-error Developers | [27](#text-27), [4](#text-4) |
| quick-xml | 0.41.0 | MIT | Copyright (c) 2016 Johann Tuffe | [7](#text-7) |
| quote | 1.0.47 | MIT OR Apache-2.0 | by David Tolnay &lt;dtolnay@gmail.com&gt; | [1](#text-1), [4](#text-4) |
| r-efi | 5.3.0 | MIT OR Apache-2.0 OR LGPL-2.1-or-later | the contributors to https://github.com/r-efi/r-efi | [4](#text-4), [5](#text-5) |
| r-efi | 6.0.0 | MIT OR Apache-2.0 OR LGPL-2.1-or-later | the contributors to https://github.com/r-efi/r-efi | [4](#text-4), [5](#text-5) |
| radium | 0.7.0 | MIT | Copyright (c) 2019 kneecaw (Nika Layzell) | [9](#text-9) |
| radsort | 0.1.1 | MIT OR Apache-2.0 | Copyright 2020 Jakub Valtar | [5](#text-5), [4](#text-4) |
| rand | 0.10.2 | MIT OR Apache-2.0 | copyright assignment is required to contribute to the Rand project.<br>Copyright 2018 Developers of the Rand project<br>Copyright (c) 2014 The Rust Project Developers | [51](#text-51), [52](#text-52), [4](#text-4) |
| rand_core | 0.10.1 | MIT OR Apache-2.0 | copyright assignment is required to contribute to the Rand project.<br>Copyright (c) 2018-2026 The Rand Project Developers | [53](#text-53), [54](#text-54), [4](#text-4) |
| rand_distr | 0.6.0 | MIT OR Apache-2.0 | copyright assignment is required to contribute to the Rand project.<br>Copyright 2018 Developers of the Rand project | [51](#text-51), [54](#text-54), [4](#text-4) |
| range-alloc | 0.1.5 | MIT OR Apache-2.0 | Copyright (c) 2023 The gfx-rs developers | [1](#text-1), [9](#text-9) |
| raw-window-handle | 0.6.2 | MIT OR Apache-2.0 OR Zlib | Copyright (c) 2019 Osspial<br>Copyright (c) 2020 Osspial | [1](#text-1), [9](#text-9), [22](#text-22) |
| raw-window-metal | 1.1.0 | MIT OR Apache-2.0 | the contributors to https://github.com/rust-windowing/raw-window-metal | [5](#text-5), [4](#text-4) |
| read-fonts | 0.39.2 | MIT OR Apache-2.0 | Copyright 2019 Colin Rothfels<br>Copyright (c) 2019 Colin Rothfels | [15](#text-15), [4](#text-4) |
| read-fonts | 0.41.0 | MIT OR Apache-2.0 | Copyright 2019 Fontations Developers<br>Copyright (c) 2019 Fontations Developers | [15](#text-15), [4](#text-4) |
| rectangle-pack | 0.4.2 | MIT/Apache-2.0 | Copyright 2021 Chinedu Francis Nwafili<br>Copyright (c) 2021 Chinedu Francis Nwafili | [18](#text-18), [4](#text-4) |
| redox_syscall | 0.4.1 | MIT | Copyright (c) 2017 Redox OS Developers | [9](#text-9) |
| redox_syscall | 0.5.18 | MIT | Copyright (c) 2017 Redox OS Developers | [9](#text-9) |
| redox_syscall | 0.9.3 | MIT | Copyright (c) 2017 Redox OS Developers | [9](#text-9) |
| regex | 1.13.1 | MIT OR Apache-2.0 | Copyright (c) 2014 The Rust Project Developers | [5](#text-5), [4](#text-4) |
| regex-automata | 0.4.18 | MIT OR Apache-2.0 | Copyright (c) 2014 The Rust Project Developers | [5](#text-5), [4](#text-4) |
| regex-syntax | 0.8.11 | MIT OR Apache-2.0 | Copyright (c) 2014 The Rust Project Developers<br>Copyright © 1991-2018 Unicode, Inc. All rights reserved. | [5](#text-5), [4](#text-4), [55](#text-55) |
| renderdoc-sys | 1.1.0 | MIT OR Apache-2.0 | Copyright (c) 2022 Eyal Kalderon | [5](#text-5), [4](#text-4) |
| rodio | 0.22.2 | MIT OR Apache-2.0 | Copyright (c) The Rodio Project Contributors | [1](#text-1), [4](#text-4) |
| ron | 0.12.2 | MIT OR Apache-2.0 | Copyright (c) 2017 RON developers | [5](#text-5), [4](#text-4) |
| rustc-hash | 1.1.0 | Apache-2.0/MIT | by The Rust Project Developers | [5](#text-5), [4](#text-4) |
| rustc_version | 0.4.1 | MIT OR Apache-2.0 | Copyright (c) 2016 The Rust Project Developers | [5](#text-5), [4](#text-4) |
| rustix | 0.38.44 | Apache-2.0 WITH LLVM-exception OR Apache-2.0 OR MIT | by Dan Gohman &lt;dev@sunfishcode.online&gt;, Jakub Konka &lt;kubkon@jakubkonka.com&gt; | [56](#text-56), [5](#text-5), [41](#text-41), [4](#text-4) |
| rustix | 1.1.4 | Apache-2.0 WITH LLVM-exception OR Apache-2.0 OR MIT | by Dan Gohman &lt;dev@sunfishcode.online&gt;, Jakub Konka &lt;kubkon@jakubkonka.com&gt; | [56](#text-56), [5](#text-5), [41](#text-41), [4](#text-4) |
| rustversion | 1.0.23 | MIT OR Apache-2.0 | by David Tolnay &lt;dtolnay@gmail.com&gt; | [1](#text-1), [4](#text-4) |
| ruzstd | 0.8.3 | MIT | Copyright (c) 2019 Moritz Borcherding | [9](#text-9) |
| same-file | 1.0.6 | Unlicense/MIT | Copyright (c) 2017 Andrew Gallant | [6](#text-6), [7](#text-7), [8](#text-8) |
| scoped-tls | 1.0.1 | MIT/Apache-2.0 | Copyright (c) 2014 Alex Crichton | [5](#text-5), [4](#text-4) |
| scopeguard | 1.2.0 | MIT OR Apache-2.0 | Copyright (c) 2016-2019 Ulrik Sverdrup "bluss" and scopeguard developers | [5](#text-5), [4](#text-4) |
| sctk-adwaita | 0.10.1 | MIT | Copyright (c) 2022 Bartłomiej Maryńczak | [9](#text-9) |
| semver | 1.0.28 | MIT OR Apache-2.0 | by David Tolnay &lt;dtolnay@gmail.com&gt; | [1](#text-1), [4](#text-4) |
| send_wrapper | 0.6.0 | MIT/Apache-2.0 | by Thomas Keh | [5](#text-5), [4](#text-4) |
| serde | 1.0.229 | MIT OR Apache-2.0 | by Erick Tryzelaar &lt;erick.tryzelaar@gmail.com&gt;, David Tolnay &lt;dtolnay@gmail.com&gt; | [1](#text-1), [4](#text-4) |
| serde_core | 1.0.229 | MIT OR Apache-2.0 | by Erick Tryzelaar &lt;erick.tryzelaar@gmail.com&gt;, David Tolnay &lt;dtolnay@gmail.com&gt; | [1](#text-1), [4](#text-4) |
| serde_derive | 1.0.229 | MIT OR Apache-2.0 | by Erick Tryzelaar &lt;erick.tryzelaar@gmail.com&gt;, David Tolnay &lt;dtolnay@gmail.com&gt; | [1](#text-1), [4](#text-4) |
| serde_json | 1.0.151 | MIT OR Apache-2.0 | by Erick Tryzelaar &lt;erick.tryzelaar@gmail.com&gt;, David Tolnay &lt;dtolnay@gmail.com&gt; | [1](#text-1), [4](#text-4) |
| sharded-slab | 0.1.7 | MIT | Copyright (c) 2019 Eliza Weisman | [4](#text-4) |
| shlex | 2.0.1 | MIT OR Apache-2.0 | Copyright 2015 Nicholas Allegra (comex).<br>Copyright (c) 2015 Nicholas Allegra (comex). | [12](#text-12), [7](#text-7) |
| simd-adler32 | 0.3.10 | MIT | Copyright (c) [2021] [Marvin Countryman] | [9](#text-9) |
| simd_cesu8 | 1.2.0 | Apache-2.0 OR MIT | by Sean C. Roach &lt;me@seancroach.dev&gt; | [5](#text-5), [4](#text-4) |
| simdutf8 | 0.1.5 | MIT OR Apache-2.0 | by Hans Kratz &lt;hans@appfour.com&gt; | [1](#text-1), [9](#text-9) |
| skrifa | 0.42.1 | MIT OR Apache-2.0 | Copyright 2019 Colin Rothfels<br>Copyright (c) 2019 Colin Rothfels | [15](#text-15), [4](#text-4) |
| skrifa | 0.44.0 | MIT OR Apache-2.0 | Copyright 2019 Fontations Developers<br>Copyright (c) 2019 Fontations Developers | [15](#text-15), [4](#text-4) |
| slab | 0.4.12 | MIT | Copyright (c) 2019 Carl Lerche | [4](#text-4) |
| slotmap | 1.1.1 | Zlib | Copyright (c) 2021 Orson Peters &lt;orsonpeters@gmail.com&gt; | [22](#text-22) |
| smallvec | 1.15.2 | MIT OR Apache-2.0 | Copyright (c) 2018 The Servo Project Developers | [5](#text-5), [4](#text-4) |
| smithay-client-toolkit | 0.19.2 | MIT | Copyright (c) 2018 Victor Berger | [4](#text-4) |
| smol_str | 0.2.2 | MIT OR Apache-2.0 | by Aleksey Kladov &lt;aleksey.kladov@gmail.com&gt; | [5](#text-5), [4](#text-4) |
| spin | 0.10.1 | MIT | Copyright (c) 2014 Mathijs van de Nes | [7](#text-7) |
| spirv | 0.4.0+sdk-1.4.341.0 | Apache-2.0 | by Lei Zhang &lt;antiagainst@gmail.com&gt; | [5](#text-5) |
| stable_deref_trait | 1.2.1 | MIT OR Apache-2.0 | Copyright (c) 2017 Robert Grosse | [5](#text-5), [4](#text-4) |
| stackfuture | 0.3.1 | MIT | Copyright (c) Microsoft Corporation. | [57](#text-57) |
| static_assertions | 1.1.0 | MIT OR Apache-2.0 | Copyright (c) 2017 Nikolai Vazquez | [5](#text-5), [9](#text-9) |
| strict-num | 0.1.1 | MIT | Copyright (c) 2022 Yevhenii Reizner | [4](#text-4) |
| svg_fmt | 0.4.5 | MIT/Apache-2.0 | by Nicolas Silva &lt;nical@fastmail.com&gt; | [4](#text-4), [5](#text-5) |
| swash | 0.2.10 | Apache-2.0 OR MIT | Copyright (c) 2020 Chad Brokaw | [5](#text-5), [4](#text-4) |
| symphonia | 0.5.5 | MPL-2.0 | by Philip Deljanov &lt;philip.deljanov@gmail.com&gt; | [68](#text-68) |
| symphonia-bundle-mp3 | 0.5.5 | MPL-2.0 | by Philip Deljanov &lt;philip.deljanov@gmail.com&gt; | [68](#text-68) |
| symphonia-core | 0.5.5 | MPL-2.0 | by Philip Deljanov &lt;philip.deljanov@gmail.com&gt; | [68](#text-68) |
| symphonia-metadata | 0.5.5 | MPL-2.0 | by Philip Deljanov &lt;philip.deljanov@gmail.com&gt; | [68](#text-68) |
| syn | 2.0.119 | MIT OR Apache-2.0 | by David Tolnay &lt;dtolnay@gmail.com&gt; | [1](#text-1), [4](#text-4) |
| syn | 3.0.4 | MIT OR Apache-2.0 | by David Tolnay &lt;dtolnay@gmail.com&gt; | [1](#text-1), [4](#text-4) |
| synstructure | 0.13.2 | MIT | Copyright 2016 Nika Layzell | [4](#text-4) |
| sys-locale | 0.3.2 | MIT OR Apache-2.0 | Copyright (c) 2021 1Password | [5](#text-5), [9](#text-9) |
| taffy | 0.10.1 | MIT | by Alice Cecile &lt;alice.i.cecile@gmail.com&gt;, Johnathan Kelley &lt;jkelleyrtp@gmail.com&gt;, Nico Burns &lt;nico@nicoburns.com&gt; | [4](#text-4) |
| tap | 1.0.1 | MIT | Copyright (c) 2017 Elliot Linder &lt;darfink@gmail.com&gt; | [9](#text-9) |
| termcolor | 1.4.1 | Unlicense OR MIT | Copyright (c) 2015 Andrew Gallant | [6](#text-6), [7](#text-7), [8](#text-8) |
| thiserror | 1.0.69 | MIT OR Apache-2.0 | by David Tolnay &lt;dtolnay@gmail.com&gt; | [1](#text-1), [4](#text-4) |
| thiserror | 2.0.20 | MIT OR Apache-2.0 | by David Tolnay &lt;dtolnay@gmail.com&gt; | [1](#text-1), [4](#text-4) |
| thiserror-impl | 1.0.69 | MIT OR Apache-2.0 | by David Tolnay &lt;dtolnay@gmail.com&gt; | [1](#text-1), [4](#text-4) |
| thiserror-impl | 2.0.20 | MIT OR Apache-2.0 | by David Tolnay &lt;dtolnay@gmail.com&gt; | [1](#text-1), [4](#text-4) |
| thread_local | 1.1.10 | MIT OR Apache-2.0 | Copyright (c) 2016 The Rust Project Developers | [5](#text-5), [4](#text-4) |
| tiny-skia | 0.11.4 | BSD-3-Clause | Copyright (c) 2011 Google Inc. All rights reserved.<br>Copyright (c) 2020 Yevhenii Reizner All rights reserved. | [58](#text-58) |
| tiny-skia-path | 0.11.4 | BSD-3-Clause | Copyright (c) 2011 Google Inc. All rights reserved.<br>Copyright (c) 2020 Yevhenii Reizner All rights reserved. | [58](#text-58) |
| tinystr | 0.8.4 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| tinyvec | 1.12.0 | Zlib OR Apache-2.0 OR MIT | Copyright (c) 2019 Daniel "Lokathor" Gee. | [5](#text-5), [4](#text-4), [22](#text-22) |
| tinyvec_macros | 0.1.1 | MIT OR Apache-2.0 OR Zlib | Copyright 2020 Tomasz "Soveu" Marx<br>Copyright (c) 2020 Soveu | [18](#text-18), [9](#text-9), [59](#text-59) |
| toml_datetime | 1.1.1+spec-1.1.0 | MIT OR Apache-2.0 | Copyright (c) Individual contributors | [27](#text-27), [4](#text-4) |
| toml_edit | 0.25.13+spec-1.1.0 | MIT OR Apache-2.0 | Copyright (c) Individual contributors | [27](#text-27), [4](#text-4) |
| toml_parser | 1.1.3+spec-1.1.0 | MIT OR Apache-2.0 | Copyright (c) Individual contributors | [27](#text-27), [4](#text-4) |
| tracing | 0.1.44 | MIT | Copyright (c) 2019 Tokio Contributors | [4](#text-4) |
| tracing-attributes | 0.1.31 | MIT | Copyright (c) 2019 Tokio Contributors | [4](#text-4) |
| tracing-core | 0.1.36 | MIT | Copyright (c) 2019 Tokio Contributors<br>Copyright (c) 2014 Mathijs van de Nes | [4](#text-4), [7](#text-7) |
| tracing-log | 0.2.0 | MIT | Copyright (c) 2019 Tokio Contributors | [4](#text-4) |
| tracing-oslog | 0.3.0 | Zlib | Copyright (c) 2021 Lucy &lt;lucy@absolucy.moe&gt; | [60](#text-60) |
| tracing-subscriber | 0.3.23 | MIT | Copyright (c) 2019 Tokio Contributors | [4](#text-4) |
| tracing-wasm | 0.2.1 | MIT OR Apache-2.0 | Copyright (c) 2020 Story.ai | [5](#text-5), [4](#text-4) |
| tree_magic_mini | 3.2.2 | MIT | Copyright (c) 2017 Aaron Hancock | [9](#text-9) |
| ttf-parser | 0.25.1 | MIT OR Apache-2.0 | Copyright (c) 2018 Yevhenii Reizner | [5](#text-5), [4](#text-4) |
| twox-hash | 2.1.4 | MIT | Copyright (c) 2015 Jake Goulding | [7](#text-7) |
| typeid | 1.0.3 | MIT OR Apache-2.0 | by David Tolnay &lt;dtolnay@gmail.com&gt; | [1](#text-1), [4](#text-4) |
| typewit | 1.15.2 | Zlib | Copyright (c) 2023 Matias Rodriguez. | [22](#text-22) |
| unicode-ident | 1.0.24 | (MIT OR Apache-2.0) AND Unicode-3.0 | Copyright © 1991-2023 Unicode, Inc. | [1](#text-1), [4](#text-4), [61](#text-61) |
| unicode-segmentation | 1.13.3 | MIT OR Apache-2.0 | Copyright (c) 2015 The Rust Project Developers | [26](#text-26), [5](#text-5), [4](#text-4) |
| unicode-width | 0.2.2 | MIT OR Apache-2.0 | Copyright (c) 2015 The Rust Project Developers | [26](#text-26), [5](#text-5), [4](#text-4) |
| unicode-xid | 0.2.6 | MIT OR Apache-2.0 | Copyright (c) 2015 The Rust Project Developers | [26](#text-26), [5](#text-5), [4](#text-4) |
| utf8_iter | 1.0.4 | Apache-2.0 OR MIT | Copyright Mozilla Foundation | [62](#text-62), [5](#text-5), [4](#text-4) |
| uuid | 1.26.0 | Apache-2.0 OR MIT | Copyright (c) 2014 The Rust Project Developers<br>Copyright (c) 2018 Ashley Mannix, Christopher Armstrong, Dylan DPC, Hunar Roop Kahlon | [5](#text-5), [4](#text-4) |
| valuable | 0.1.1 | MIT | the contributors to https://github.com/tokio-rs/valuable | [4](#text-4) |
| variadics_please | 1.1.0 | MIT OR Apache-2.0 | the contributors to https://github.com/bevyengine/variadics_please | [1](#text-1), [9](#text-9) |
| vec_map | 0.8.2 | MIT/Apache-2.0 | Copyright (c) 2015 The Rust Project Developers | [5](#text-5), [4](#text-4) |
| version_check | 0.9.5 | MIT/Apache-2.0 | Copyright (c) 2017-2018 Sergio Benitez<br>COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER | [5](#text-5), [13](#text-13) |
| walkdir | 2.5.0 | Unlicense/MIT | Copyright (c) 2015 Andrew Gallant | [6](#text-6), [7](#text-7), [8](#text-8) |
| wasi | 0.11.1+wasi-snapshot-preview1 | Apache-2.0 WITH LLVM-exception OR Apache-2.0 OR MIT | by The Cranelift Project Developers | [5](#text-5), [41](#text-41), [4](#text-4) |
| wasip2 | 1.0.4+wasi-0.2.12 | Apache-2.0 WITH LLVM-exception OR Apache-2.0 OR MIT | the contributors to https://github.com/bytecodealliance/wasi-rs | [5](#text-5), [41](#text-41), [4](#text-4) |
| wasm-bindgen | 0.2.127 | MIT OR Apache-2.0 | Copyright (c) 2014 Alex Crichton | [5](#text-5), [4](#text-4) |
| wasm-bindgen-futures | 0.4.77 | MIT OR Apache-2.0 | Copyright (c) 2014 Alex Crichton | [5](#text-5), [4](#text-4) |
| wasm-bindgen-macro | 0.2.127 | MIT OR Apache-2.0 | Copyright (c) 2014 Alex Crichton | [5](#text-5), [4](#text-4) |
| wasm-bindgen-macro-support | 0.2.127 | MIT OR Apache-2.0 | Copyright (c) 2014 Alex Crichton | [5](#text-5), [4](#text-4) |
| wasm-bindgen-shared | 0.2.127 | MIT OR Apache-2.0 | Copyright (c) 2014 Alex Crichton | [5](#text-5), [4](#text-4) |
| wayland-backend | 0.3.17 | MIT | Copyright (c) 2015 Elinor Berger | [4](#text-4) |
| wayland-client | 0.31.15 | MIT | Copyright (c) 2015 Elinor Berger | [4](#text-4) |
| wayland-csd-frame | 0.3.0 | MIT | Copyright (c) 2023 Kirill Chibisov | [4](#text-4) |
| wayland-cursor | 0.31.14 | MIT | Copyright (c) 2015 Elinor Berger | [4](#text-4) |
| wayland-protocols | 0.32.13 | MIT | Copyright (c) 2015 Elinor Berger<br>Copyright © 2008-2013 Kristian Høgsberg<br>Copyright © 2010-2013 Intel Corporation<br>Copyright © 2013      Rafael Antognolli<br>Copyright © 2013      Jasper St. Pierre<br>Copyright © 2014      Jonas Ådahl<br>Copyright © 2014      Jason Ekstrand<br>Copyright © 2014-2015 Collabora, Ltd.<br>Copyright © 2015      Red Hat Inc. | [4](#text-4), [63](#text-63) |
| wayland-protocols-plasma | 0.3.12 | MIT | Copyright (c) 2015 Elinor Berger<br>Copyright (C) 1991, 1999 Free Software Foundation, Inc.<br>copyright law: that is to say, a work containing the Library or a | [4](#text-4), [64](#text-64) |
| wayland-protocols-wlr | 0.3.12 | MIT | Copyright (c) 2015 Elinor Berger | [4](#text-4) |
| wayland-scanner | 0.31.11 | MIT | Copyright (c) 2015 Elinor Berger | [4](#text-4) |
| wayland-sys | 0.31.11 | MIT | Copyright (c) 2015 Elinor Berger | [4](#text-4) |
| weak-table | 0.3.2 | MIT | Copyright (c) 2018 Jesse A. Tov | [9](#text-9) |
| web-sys | 0.3.104 | MIT OR Apache-2.0 | Copyright (c) 2014 Alex Crichton | [5](#text-5), [4](#text-4) |
| web-task | 1.1.3 | MIT OR Apache-2.0 | Copyright (c) 2014 Alex Crichton<br>Copyright (c) 2025 Miles Silberling-Cook | [5](#text-5), [4](#text-4) |
| web-time | 1.1.0 | MIT OR Apache-2.0 | Copyright 2023 dAxpeDDa<br>Copyright (c) 2023 dAxpeDDa | [18](#text-18), [9](#text-9) |
| wgpu | 29.0.4 | MIT OR Apache-2.0 | Copyright (c) 2025 The gfx-rs developers | [1](#text-1), [9](#text-9) |
| wgpu-core | 29.0.4 | MIT OR Apache-2.0 | Copyright (c) 2025 The gfx-rs developers | [1](#text-1), [9](#text-9) |
| wgpu-core-deps-apple | 29.0.4 | MIT OR Apache-2.0 | Copyright (c) 2025 The gfx-rs developers | [1](#text-1), [9](#text-9) |
| wgpu-core-deps-windows-linux-android | 29.0.4 | MIT OR Apache-2.0 | Copyright (c) 2025 The gfx-rs developers | [1](#text-1), [9](#text-9) |
| wgpu-hal | 29.0.4 | MIT OR Apache-2.0 | Copyright (c) 2025 The gfx-rs developers | [1](#text-1), [9](#text-9) |
| wgpu-naga-bridge | 29.0.4 | MIT OR Apache-2.0 | Copyright (c) 2025 The gfx-rs developers | [1](#text-1), [9](#text-9) |
| wgpu-types | 29.0.4 | MIT OR Apache-2.0 | Copyright (c) 2025 The gfx-rs developers | [1](#text-1), [9](#text-9) |
| winapi-util | 0.1.11 | Unlicense OR MIT | Copyright (c) 2017 Andrew Gallant | [6](#text-6), [7](#text-7), [8](#text-8) |
| windows | 0.62.2 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows-collections | 0.3.2 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows-core | 0.62.2 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows-future | 0.3.2 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows-implement | 0.60.2 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows-interface | 0.59.3 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows-link | 0.2.1 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows-numerics | 0.3.1 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows-result | 0.4.1 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows-strings | 0.5.1 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows-sys | 0.45.0 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows-sys | 0.52.0 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows-sys | 0.59.0 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows-sys | 0.60.2 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows-sys | 0.61.2 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows-targets | 0.42.2 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows-targets | 0.52.6 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows-targets | 0.53.5 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows-threading | 0.2.1 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_aarch64_gnullvm | 0.42.2 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_aarch64_gnullvm | 0.52.6 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_aarch64_gnullvm | 0.53.1 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_aarch64_msvc | 0.42.2 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_aarch64_msvc | 0.52.6 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_aarch64_msvc | 0.53.1 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_i686_gnu | 0.42.2 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_i686_gnu | 0.52.6 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_i686_gnu | 0.53.1 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_i686_gnullvm | 0.52.6 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_i686_gnullvm | 0.53.1 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_i686_msvc | 0.42.2 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_i686_msvc | 0.52.6 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_i686_msvc | 0.53.1 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_x86_64_gnu | 0.42.2 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_x86_64_gnu | 0.52.6 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_x86_64_gnu | 0.53.1 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_x86_64_gnullvm | 0.42.2 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_x86_64_gnullvm | 0.52.6 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_x86_64_gnullvm | 0.53.1 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_x86_64_msvc | 0.42.2 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_x86_64_msvc | 0.52.6 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| windows_x86_64_msvc | 0.53.1 | MIT OR Apache-2.0 | Copyright (c) Microsoft Corporation. | [18](#text-18), [65](#text-65) |
| winit | 0.30.13 | Apache-2.0 | by The winit contributors, Pierre Krieger &lt;pierre.krieger1708@gmail.com&gt; | [27](#text-27) |
| winnow | 1.0.4 | MIT | the contributors to https://github.com/winnow-rs/winnow | [4](#text-4) |
| wit-bindgen | 0.57.1 | Apache-2.0 WITH LLVM-exception OR Apache-2.0 OR MIT | by Alex Crichton &lt;alex@alexcrichton.com&gt; | [5](#text-5), [41](#text-41), [4](#text-4) |
| wl-clipboard-rs | 0.9.3 | MIT/Apache-2.0 | Copyright (c) 2019 Ivan Molodetskikh | [5](#text-5), [4](#text-4) |
| writeable | 0.6.4 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| wyz | 0.5.1 | MIT | Copyright (c) 2018 myrrlyn (Alexander Payne) | [9](#text-9) |
| x11-dl | 2.21.0 | MIT | by daggerbot &lt;daggerbot@gmail.com&gt;, Erle Pereira &lt;erle@erlepereira.com&gt;, AltF02 &lt;contact@altf2.dev&gt; | [4](#text-4) |
| x11rb | 0.13.2 | MIT OR Apache-2.0 | Copyright 2019 x11rb Contributers | [5](#text-5), [4](#text-4) |
| x11rb-protocol | 0.13.2 | MIT OR Apache-2.0 | Copyright 2019 x11rb Contributers | [5](#text-5), [4](#text-4) |
| xcursor | 0.3.11 | MIT | Copyright (c) 2020 Samuele Esposito | [9](#text-9) |
| xkbcommon-dl | 0.4.2 | MIT | Copyright (c) 2023 Kirill Chibisov | [7](#text-7) |
| xkeysym | 0.2.1 | MIT OR Apache-2.0 OR Zlib | Copyright 2022-2023 John Nunley<br>Copyright (c) 2022-2023 John Nunley | [18](#text-18), [4](#text-4), [22](#text-22) |
| yazi | 0.2.1 | Apache-2.0 OR MIT | Copyright (c) 2020 Chad Brokaw | [5](#text-5), [4](#text-4) |
| yoke | 0.8.3 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| yoke-derive | 0.8.2 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| zeno | 0.3.3 | Apache-2.0 OR MIT | Copyright (c) 2020 Chad Brokaw | [5](#text-5), [4](#text-4) |
| zerocopy | 0.8.56 | BSD-2-Clause OR Apache-2.0 OR MIT | Copyright 2023 The Fuchsia Authors<br>Copyright 2019 The Fuchsia Authors. | [18](#text-18), [66](#text-66), [4](#text-4) |
| zerocopy-derive | 0.8.56 | BSD-2-Clause OR Apache-2.0 OR MIT | Copyright 2023 The Fuchsia Authors<br>Copyright 2019 The Fuchsia Authors. | [18](#text-18), [66](#text-66), [4](#text-4) |
| zerofrom | 0.1.8 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| zerofrom-derive | 0.1.7 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| zerotrie | 0.2.5 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| zerovec | 0.11.8 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| zerovec-derive | 0.11.6 | Unicode-3.0 | Copyright © 2020-2024 Unicode, Inc. | [36](#text-36) |
| zlib-rs | 0.6.7 | Zlib | the contributors to https://github.com/trifectatechfoundation/zlib-rs | [67](#text-67) |
| zmij | 1.0.23 | MIT | by David Tolnay &lt;dtolnay@gmail.com&gt; | [4](#text-4) |
| zune-core | 0.4.12 | MIT OR Apache-2.0 OR Zlib | the contributors to https://github.com/etemesi254/zune-image/tree/dev/zune-core | [4](#text-4), [5](#text-5), [22](#text-22) |
| zune-jpeg | 0.4.21 | MIT OR Apache-2.0 OR Zlib | by caleb &lt;etemesicaleb@gmail.com&gt; | [4](#text-4), [5](#text-5), [22](#text-22) |

Bevy's default font, a subset of Fira Mono that bevy_text compiles into the bridge, is under the
SIL Open Font License 1.1, whose text with its holders is [text 69](#text-69).

The crates under the Mozilla Public License 2.0 are compiled in unchanged, and their source is
on crates.io at the versions above: symphonia, symphonia-bundle-mp3, symphonia-core and symphonia-metadata.

## The examples

The programs in `BevyCSharp.Examples` named for Bevy's examples are Bevy's
(https://github.com/bevyengine/bevy/tree/v0.19.1/examples), by Bevy's contributors under
MIT or Apache-2.0, written again in C#, each naming at its head the example of Bevy's it is written
from. They are not in the package. The files of Bevy's they load are fetched from Bevy's repository
by `build/fetch-bevy-assets.sh` and are not kept in this one. Bevy credits them so:

> * Generic RPG Pack (CC0 license) by [Bakudas](https://twitter.com/bakudas) and [Gabe Fern](https://twitter.com/_Gabrielfer)
> * Environment maps (`.hdr` files) from [HDRIHaven](https://hdrihaven.com) (CC0 license)
> * Alien from [Kenney's Space Kit](https://www.kenney.nl/assets/space-kit) (CC0 1.0 Universal)
> * Cake from [Kenney's Food Kit](https://www.kenney.nl/assets/food-kit) (CC0 1.0 Universal)
> * Ground tile from [Kenney's Tower Defense Kit](https://www.kenney.nl/assets/tower-defense-kit) (CC0 1.0 Universal)
> * Game icons from [Kenney's Game Icons](https://www.kenney.nl/assets/game-icons) (CC0 1.0 Universal)
> * Space ships from [Kenney's Simple Space Kit](https://www.kenney.nl/assets/simple-space) (CC0 1.0 Universal)
> * UI borders from [Kenney's Fantasy UI Borders Kit](https://kenney.nl/assets/fantasy-ui-borders) (CC0 1.0 Universal)
> * glTF animated fox from [glTF Sample Models][fox]
>   * Low poly fox [by PixelMannen] (CC0 1.0 Universal)
>   * Rigging and animation [by @tomkranis on Sketchfab] ([CC-BY 4.0])
> * FiraMono by The Mozilla Foundation and Telefonica S.A (SIL Open Font License, Version 1.1: assets/fonts/FiraMono-LICENSE)
> * Barycentric from [mk_bary_gltf](https://github.com/komadori/mk_bary_gltf) (MIT OR Apache-2.0)
> * `MorphStressTest.gltf`, [MorphStressTest] ([CC-BY 4.0] by Analytical Graphics, Inc, Model and textures by Ed Mackey)
> * Mysterious acoustic guitar music sample from [florianreichelt](https://freesound.org/people/florianreichelt/sounds/412429/) (CC0 license)
> * Epic orchestra music sample, modified to loop, from [Migfus20](https://freesound.org/people/Migfus20/sounds/560449/) ([CC BY 4.0 DEED](https://creativecommons.org/licenses/by/4.0/))
>
> [MorphStressTest]: https://github.com/KhronosGroup/glTF-Sample-Models/tree/master/2.0/MorphStressTest
> [fox]: https://github.com/KhronosGroup/glTF-Sample-Assets/tree/main/Models/Fox
> [by PixelMannen]: https://opengameart.org/content/fox-and-shiba
> [by @tomkranis on Sketchfab]: https://sketchfab.com/models/371dea88d7e04a76af5763f2a36866bc
> [CC-BY 4.0]: https://creativecommons.org/licenses/by/4.0/

## The scene packs

The scenes the manifests in `scenes/` name are fetched as packs when they are asked for, and are
kept in neither this repository nor the package. Each pack holds its makers' own license file
beside its model.

- **Intel Sponza** (https://www.intel.com/content/www/us/en/developer/topic-technology/graphics-research/samples.html), under CC BY 4.0. Intel Sponza (2022) by Frank Meinl and Anton Kaplanyan, from Intel's Sample Library, under CC BY 4.0. Its textures are resized to 1K and compressed for the GPU here, and its lights and cameras left out.

## Texts

### Text 1

Apache-2.0, as ab_glyph 0.2.32, ab_glyph_rasterizer 0.1.10, allocator-api2 0.2.21, alsa 0.11.0, android-activity 0.6.1, arboard 3.6.1, atomicow 1.2.0, bevy 0.19.1, bevy_a11y 0.19.1, bevy_android 0.19.1, bevy_animation 0.19.1, bevy_animation_macros 0.19.1, bevy_anti_alias 0.19.1, bevy_app 0.19.1, bevy_asset 0.19.1, bevy_asset_macros 0.19.1, bevy_audio 0.19.1, bevy_camera 0.19.1, bevy_clipboard 0.19.1, bevy_color 0.19.1, bevy_core_pipeline 0.19.1, bevy_derive 0.19.1, bevy_diagnostic 0.19.1, bevy_ecs 0.19.1, bevy_ecs_macro_logic 0.19.1, bevy_ecs_macros 0.19.1, bevy_encase_derive 0.19.1, bevy_gilrs 0.19.1, bevy_gizmos 0.19.1, bevy_gizmos_macros 0.19.1, bevy_gizmos_render 0.19.1, bevy_gltf 0.19.1, bevy_image 0.19.1, bevy_input 0.19.1, bevy_input_focus 0.19.1, bevy_internal 0.19.1, bevy_light 0.19.1, bevy_log 0.19.1, bevy_macro_utils 0.19.1, bevy_material 0.19.1, bevy_material_macros 0.19.1, bevy_math 0.19.1, bevy_mesh 0.19.1, bevy_mikktspace 1.0.0, bevy_pbr 0.19.1, bevy_picking 0.19.1, bevy_platform 0.19.1, bevy_post_process 0.19.1, bevy_ptr 0.19.1, bevy_reflect 0.19.1, bevy_reflect_derive 0.19.1, bevy_render 0.19.1, bevy_render_macros 0.19.1, bevy_scene 0.19.1, bevy_shader 0.19.1, bevy_solari 0.19.1, bevy_sprite 0.19.1, bevy_sprite_render 0.19.1, bevy_state 0.19.1, bevy_state_macros 0.19.1, bevy_tasks 0.19.1, bevy_text 0.19.1, bevy_time 0.19.1, bevy_transform 0.19.1, bevy_ui 0.19.1, bevy_ui_render 0.19.1, bevy_ui_widgets 0.19.1, bevy_utils 0.19.1, bevy_window 0.19.1, bevy_winit 0.19.1, bevy_world_serialization 0.19.1, constant_time_eq 0.4.2, disqualified 1.0.0, erased-serde 0.4.10, fdeflate 0.3.7, half 2.7.1, hexasphere 18.0.0, image 0.25.6, image-webp 0.2.4, inventory 0.3.24, itoa 1.0.18, libc 0.2.189, linebender_resource_handle 0.1.1, litrs 1.0.0, mach2 0.5.0, miniz_oxide 0.8.9, miniz_oxide 0.9.1, naga 29.0.4, naga_oil 0.22.0, num_enum 0.7.6, num_enum_derive 0.7.6, owned_ttf_parser 0.25.1, pin-project 1.1.13, pin-project-internal 1.1.13, pin-project-lite 0.2.17, portable-atomic 1.15.0, portable-atomic-util 0.2.7, proc-macro2 1.0.107, quote 1.0.47, range-alloc 0.1.5, raw-window-handle 0.6.2, rodio 0.22.2, rustversion 1.0.23, semver 1.0.28, serde 1.0.229, serde_core 1.0.229, serde_derive 1.0.229, serde_json 1.0.151, simdutf8 0.1.5, syn 2.0.119, syn 3.0.4, thiserror 1.0.69, thiserror 2.0.20, thiserror-impl 1.0.69, thiserror-impl 2.0.20, typeid 1.0.3, unicode-ident 1.0.24, variadics_please 1.1.0, wgpu 29.0.4, wgpu-core 29.0.4, wgpu-core-deps-apple 29.0.4, wgpu-core-deps-windows-linux-android 29.0.4, wgpu-hal 29.0.4, wgpu-naga-bridge 29.0.4, wgpu-types 29.0.4 carry it.

~~~~
                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS
~~~~

### Text 2

0BSD, as adler2 2.0.1 carries it.

~~~~
Permission to use, copy, modify, and/or distribute this software for
any purpose with or without fee is hereby granted.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN
AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT
OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
~~~~

### Text 3

Apache-2.0, as adler2 2.0.1, proc-macro-crate 3.5.0 carry it.

~~~~
                              Apache License
                        Version 2.0, January 2004
                     https://www.apache.org/licenses/LICENSE-2.0

TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

1. Definitions.

   "License" shall mean the terms and conditions for use, reproduction,
   and distribution as defined by Sections 1 through 9 of this document.

   "Licensor" shall mean the copyright owner or entity authorized by
   the copyright owner that is granting the License.

   "Legal Entity" shall mean the union of the acting entity and all
   other entities that control, are controlled by, or are under common
   control with that entity. For the purposes of this definition,
   "control" means (i) the power, direct or indirect, to cause the
   direction or management of such entity, whether by contract or
   otherwise, or (ii) ownership of fifty percent (50%) or more of the
   outstanding shares, or (iii) beneficial ownership of such entity.

   "You" (or "Your") shall mean an individual or Legal Entity
   exercising permissions granted by this License.

   "Source" form shall mean the preferred form for making modifications,
   including but not limited to software source code, documentation
   source, and configuration files.

   "Object" form shall mean any form resulting from mechanical
   transformation or translation of a Source form, including but
   not limited to compiled object code, generated documentation,
   and conversions to other media types.

   "Work" shall mean the work of authorship, whether in Source or
   Object form, made available under the License, as indicated by a
   copyright notice that is included in or attached to the work
   (an example is provided in the Appendix below).

   "Derivative Works" shall mean any work, whether in Source or Object
   form, that is based on (or derived from) the Work and for which the
   editorial revisions, annotations, elaborations, or other modifications
   represent, as a whole, an original work of authorship. For the purposes
   of this License, Derivative Works shall not include works that remain
   separable from, or merely link (or bind by name) to the interfaces of,
   the Work and Derivative Works thereof.

   "Contribution" shall mean any work of authorship, including
   the original version of the Work and any modifications or additions
   to that Work or Derivative Works thereof, that is intentionally
   submitted to Licensor for inclusion in the Work by the copyright owner
   or by an individual or Legal Entity authorized to submit on behalf of
   the copyright owner. For the purposes of this definition, "submitted"
   means any form of electronic, verbal, or written communication sent
   to the Licensor or its representatives, including but not limited to
   communication on electronic mailing lists, source code control systems,
   and issue tracking systems that are managed by, or on behalf of, the
   Licensor for the purpose of discussing and improving the Work, but
   excluding communication that is conspicuously marked or otherwise
   designated in writing by the copyright owner as "Not a Contribution."

   "Contributor" shall mean Licensor and any individual or Legal Entity
   on behalf of whom a Contribution has been received by Licensor and
   subsequently incorporated within the Work.

2. Grant of Copyright License. Subject to the terms and conditions of
   this License, each Contributor hereby grants to You a perpetual,
   worldwide, non-exclusive, no-charge, royalty-free, irrevocable
   copyright license to reproduce, prepare Derivative Works of,
   publicly display, publicly perform, sublicense, and distribute the
   Work and such Derivative Works in Source or Object form.

3. Grant of Patent License. Subject to the terms and conditions of
   this License, each Contributor hereby grants to You a perpetual,
   worldwide, non-exclusive, no-charge, royalty-free, irrevocable
   (except as stated in this section) patent license to make, have made,
   use, offer to sell, sell, import, and otherwise transfer the Work,
   where such license applies only to those patent claims licensable
   by such Contributor that are necessarily infringed by their
   Contribution(s) alone or by combination of their Contribution(s)
   with the Work to which such Contribution(s) was submitted. If You
   institute patent litigation against any entity (including a
   cross-claim or counterclaim in a lawsuit) alleging that the Work
   or a Contribution incorporated within the Work constitutes direct
   or contributory patent infringement, then any patent licenses
   granted to You under this License for that Work shall terminate
   as of the date such litigation is filed.

4. Redistribution. You may reproduce and distribute copies of the
   Work or Derivative Works thereof in any medium, with or without
   modifications, and in Source or Object form, provided that You
   meet the following conditions:

   (a) You must give any other recipients of the Work or
       Derivative Works a copy of this License; and

   (b) You must cause any modified files to carry prominent notices
       stating that You changed the files; and

   (c) You must retain, in the Source form of any Derivative Works
       that You distribute, all copyright, patent, trademark, and
       attribution notices from the Source form of the Work,
       excluding those notices that do not pertain to any part of
       the Derivative Works; and

   (d) If the Work includes a "NOTICE" text file as part of its
       distribution, then any Derivative Works that You distribute must
       include a readable copy of the attribution notices contained
       within such NOTICE file, excluding those notices that do not
       pertain to any part of the Derivative Works, in at least one
       of the following places: within a NOTICE text file distributed
       as part of the Derivative Works; within the Source form or
       documentation, if provided along with the Derivative Works; or,
       within a display generated by the Derivative Works, if and
       wherever such third-party notices normally appear. The contents
       of the NOTICE file are for informational purposes only and
       do not modify the License. You may add Your own attribution
       notices within Derivative Works that You distribute, alongside
       or as an addendum to the NOTICE text from the Work, provided
       that such additional attribution notices cannot be construed
       as modifying the License.

   You may add Your own copyright statement to Your modifications and
   may provide additional or different license terms and conditions
   for use, reproduction, or distribution of Your modifications, or
   for any such Derivative Works as a whole, provided Your use,
   reproduction, and distribution of the Work otherwise complies with
   the conditions stated in this License.

5. Submission of Contributions. Unless You explicitly state otherwise,
   any Contribution intentionally submitted for inclusion in the Work
   by You to the Licensor shall be under the terms and conditions of
   this License, without any additional terms or conditions.
   Notwithstanding the above, nothing herein shall supersede or modify
   the terms of any separate license agreement you may have executed
   with Licensor regarding such Contributions.

6. Trademarks. This License does not grant permission to use the trade
   names, trademarks, service marks, or product names of the Licensor,
   except as required for reasonable and customary use in describing the
   origin of the Work and reproducing the content of the NOTICE file.

7. Disclaimer of Warranty. Unless required by applicable law or
   agreed to in writing, Licensor provides the Work (and each
   Contributor provides its Contributions) on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
   implied, including, without limitation, any warranties or conditions
   of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
   PARTICULAR PURPOSE. You are solely responsible for determining the
   appropriateness of using or redistributing the Work and assume any
   risks associated with Your exercise of permissions under this License.

8. Limitation of Liability. In no event and under no legal theory,
   whether in tort (including negligence), contract, or otherwise,
   unless required by applicable law (such as deliberate and grossly
   negligent acts) or agreed to in writing, shall any Contributor be
   liable to You for damages, including any direct, indirect, special,
   incidental, or consequential damages of any character arising as a
   result of this License or out of the use or inability to use the
   Work (including but not limited to damages for loss of goodwill,
   work stoppage, computer failure or malfunction, or any and all
   other commercial damages or losses), even if such Contributor
   has been advised of the possibility of such damages.

9. Accepting Warranty or Additional Liability. While redistributing
   the Work or Derivative Works thereof, You may choose to offer,
   and charge a fee for, acceptance of support, warranty, indemnity,
   or other liability obligations and/or rights consistent with this
   License. However, in accepting such obligations, You may act only
   on Your own behalf and on Your sole responsibility, not on behalf
   of any other Contributor, and only if You agree to indemnify,
   defend, and hold each Contributor harmless for any liability
   incurred by, or claims asserted against, such Contributor by reason
   of your accepting any such warranty or additional liability.

END OF TERMS AND CONDITIONS

APPENDIX: How to apply the Apache License to your work.

   To apply the Apache License to your work, attach the following
   boilerplate notice, with the fields enclosed by brackets "[]"
   replaced with your own identifying information. (Don't include
   the brackets!)  The text should be enclosed in the appropriate
   comment syntax for the file format. We also recommend that a
   file or class name and description of purpose be included on the
   same "printed page" as the copyright notice for easier
   identification within third-party archives.

Copyright [yyyy] [name of copyright owner]

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

	https://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
~~~~

### Text 4

MIT, as adler2 2.0.1, ahash 0.8.12, allocator-api2 0.2.21, android_log-sys 0.3.2, arrayvec 0.7.8, as-raw-xcb-connection 1.0.1, ash 0.38.0+1.3.281, async-channel 2.5.0, async-executor 1.14.0, async-fs 2.2.0, async-io 2.6.0, async-lock 3.4.2, async-task 4.7.1, atomic-waker 1.1.2, autocfg 1.5.1, bevy_mikktspace 1.0.0, bit-set 0.9.1, bit-vec 0.9.1, bitflags 1.3.2, bitflags 2.13.1, blocking 1.7.0, bumpalo 3.20.3, bytes 1.12.1, calloop 0.13.0, calloop-wayland-source 0.3.0, cc 1.4.4, cfg-if 1.0.4, concurrent-queue 2.5.0, console_error_panic_hook 0.1.7, const_soft_float 0.1.4, core-foundation 0.9.4, core-foundation-sys 0.8.7, core-graphics 0.23.2, core-graphics-types 0.1.3, coreaudio-rs 0.14.2, cpufeatures 0.3.1, critical-section 1.2.0, ctrlc 3.5.2, cursor-icon 1.2.0, displaydoc 0.2.7, dlib 0.5.3, document-features 0.2.12, downcast-rs 1.2.1, downcast-rs 2.0.2, either 1.18.0, encoding_rs 0.8.35, equivalent 1.0.2, erased-serde 0.4.10, errno 0.3.14, euclid 0.22.14, event-listener 5.4.2, event-listener-strategy 0.5.4, fastrand 2.5.0, file-id 0.2.3, find-msvc-tools 0.1.11, fixedbitset 0.5.7, flate2 1.1.10, float-cmp 0.10.0, fnv 1.0.7, font-types 0.11.3, font-types 0.12.4, fontique 0.9.0, foreign-types 0.5.0, foreign-types-macros 0.2.4, foreign-types-shared 0.3.1, futures-channel 0.3.34, futures-core 0.3.34, futures-io 0.3.34, futures-lite 2.6.1, futures-macro 0.3.34, futures-task 0.3.34, futures-util 0.3.34, getrandom 0.3.4, getrandom 0.4.3, glam 0.32.1, gltf 1.4.1, gltf-json 1.4.1, gpu-allocator 0.28.0, hash32 0.3.1, hashbrown 0.15.5, hashbrown 0.16.1, hashbrown 0.17.1, heapless 0.9.3, hermit-abi 0.5.2, hexasphere 18.0.0, indexmap 2.14.1, inflections 1.1.1, inventory 0.3.24, itertools 0.14.0, itoa 1.0.18, jni-sys 0.3.1, jni-sys 0.4.1, jobserver 0.1.35, js-sys 0.3.104, kqueue 1.2.1, kqueue-sys 1.1.2, lazy_static 1.5.0, libc 0.2.189, libudev-sys 0.1.4, linux-raw-sys 0.4.15, linux-raw-sys 0.12.1, litrs 1.0.0, lock_api 0.4.14, log 0.4.34, mach2 0.5.0, matchers 0.2.0, memmap2 0.9.11, mio 1.2.3, nom 8.0.0, nonmax 0.5.5, notify-debouncer-full 0.7.0, notify-types 2.1.0, num-bigint 0.4.8, num-derive 0.4.2, num-integer 0.1.47, num-rational 0.4.2, num-traits 0.2.19, num_enum 0.7.6, num_enum_derive 0.7.6, once_cell 1.21.4, ordered-float 5.5.0, parking 2.2.1, parking_lot 0.12.5, parking_lot_core 0.9.12, parlance 0.1.0, parley 0.9.0, parley_data 0.9.0, percent-encoding 2.3.2, petgraph 0.8.3, pin-project 1.1.13, pin-project-internal 1.1.13, pin-project-lite 0.2.17, piper 0.2.5, pkg-config 0.3.34, plain 0.2.3, png 0.17.16, polling 3.11.0, portable-atomic 1.15.0, portable-atomic-util 0.2.7, presser 0.3.1, proc-macro-crate 3.5.0, proc-macro2 1.0.107, quick-error 2.0.1, quote 1.0.47, radsort 0.1.1, rand 0.10.2, rand_core 0.10.1, rand_distr 0.6.0, raw-window-metal 1.1.0, read-fonts 0.39.2, read-fonts 0.41.0, rectangle-pack 0.4.2, regex 1.13.1, regex-automata 0.4.18, regex-syntax 0.8.11, renderdoc-sys 1.1.0, rodio 0.22.2, ron 0.12.2, rustc-hash 1.1.0, rustc_version 0.4.1, rustix 0.38.44, rustix 1.1.4, rustversion 1.0.23, scoped-tls 1.0.1, scopeguard 1.2.0, semver 1.0.28, send_wrapper 0.6.0, serde 1.0.229, serde_core 1.0.229, serde_derive 1.0.229, serde_json 1.0.151, sharded-slab 0.1.7, simd_cesu8 1.2.0, skrifa 0.42.1, skrifa 0.44.0, slab 0.4.12, smallvec 1.15.2, smithay-client-toolkit 0.19.2, smol_str 0.2.2, stable_deref_trait 1.2.1, strict-num 0.1.1, swash 0.2.10, syn 2.0.119, syn 3.0.4, synstructure 0.13.2, thiserror 1.0.69, thiserror 2.0.20, thiserror-impl 1.0.69, thiserror-impl 2.0.20, thread_local 1.1.10, tinyvec 1.12.0, toml_datetime 1.1.1+spec-1.1.0, toml_edit 0.25.13+spec-1.1.0, toml_parser 1.1.3+spec-1.1.0, tracing 0.1.44, tracing-attributes 0.1.31, tracing-core 0.1.36, tracing-log 0.2.0, tracing-subscriber 0.3.23, tracing-wasm 0.2.1, ttf-parser 0.25.1, typeid 1.0.3, unicode-ident 1.0.24, unicode-segmentation 1.13.3, unicode-width 0.2.2, unicode-xid 0.2.6, utf8_iter 1.0.4, uuid 1.26.0, vec_map 0.8.2, wasi 0.11.1+wasi-snapshot-preview1, wasip2 1.0.4+wasi-0.2.12, wasm-bindgen 0.2.127, wasm-bindgen-futures 0.4.77, wasm-bindgen-macro 0.2.127, wasm-bindgen-macro-support 0.2.127, wasm-bindgen-shared 0.2.127, wayland-backend 0.3.17, wayland-client 0.31.15, wayland-csd-frame 0.3.0, wayland-cursor 0.31.14, wayland-protocols 0.32.13, wayland-protocols-plasma 0.3.12, wayland-protocols-wlr 0.3.12, wayland-scanner 0.31.11, wayland-sys 0.31.11, web-sys 0.3.104, web-task 1.1.3, winnow 1.0.4, wit-bindgen 0.57.1, wl-clipboard-rs 0.9.3, x11-dl 2.21.0, x11rb 0.13.2, x11rb-protocol 0.13.2, xkeysym 0.2.1, yazi 0.2.1, zeno 0.3.3, zerocopy 0.8.56, zerocopy-derive 0.8.56, zmij 1.0.23 carry it, and the text taken for accesskit 0.24.1, accesskit_consumer 0.35.0, accesskit_consumer 0.38.0, accesskit_macos 0.26.3, accesskit_windows 0.32.1, bevy_embedded_assets 0.16.0, bevy_scene_macros 0.19.1, block2 0.5.1, block2 0.6.2, constgebra 0.1.4, dasp_sample 0.11.0, dispatch 0.2.0, dispatch2 0.3.1, gilrs-core 0.6.8, gpu-descriptor 0.3.2, gpu-descriptor-types 0.2.0, jni 0.22.4, jni-macros 0.22.4, jni-sys-macros 0.4.1, meshopt 0.6.2, ndk 0.9.0, ndk-context 0.1.1, ndk-sys 0.6.0+11769913, objc-sys 0.3.5, objc2 0.5.2, objc2 0.6.4, objc2-app-kit 0.2.2, objc2-app-kit 0.3.2, objc2-audio-toolbox 0.3.2, objc2-avf-audio 0.3.2, objc2-cloud-kit 0.2.2, objc2-contacts 0.2.2, objc2-core-audio 0.3.2, objc2-core-audio-types 0.3.2, objc2-core-data 0.2.2, objc2-core-foundation 0.3.2, objc2-core-graphics 0.3.2, objc2-core-image 0.2.2, objc2-core-location 0.2.2, objc2-encode 4.1.0, objc2-foundation 0.2.2, objc2-foundation 0.3.2, objc2-io-kit 0.3.2, objc2-io-surface 0.3.2, objc2-link-presentation 0.2.2, objc2-metal 0.2.2, objc2-metal 0.3.2, objc2-quartz-core 0.2.2, objc2-quartz-core 0.3.2, objc2-symbols 0.2.2, objc2-ui-kit 0.2.2, objc2-uniform-type-identifiers 0.2.2, objc2-user-notifications 0.2.2, profiling 1.0.18, r-efi 5.3.0, r-efi 6.0.0, svg_fmt 0.4.5, taffy 0.10.1, valuable 0.1.1, zune-core 0.4.12, zune-jpeg 0.4.21, whose packages hold none.

~~~~
Permission is hereby granted, free of charge, to any
person obtaining a copy of this software and associated
documentation files (the "Software"), to deal in the
Software without restriction, including without
limitation the rights to use, copy, modify, merge,
publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software
is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice
shall be included in all copies or substantial portions
of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF
ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT
SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.
~~~~

### Text 5

Apache-2.0, as ahash 0.8.12, approx 0.5.1, arrayvec 0.7.8, as-raw-xcb-connection 1.0.1, assert_type_match 0.1.1, async-channel 2.5.0, async-executor 1.14.0, async-fs 2.2.0, async-io 2.6.0, async-lock 3.4.2, async-task 4.7.1, atomic-waker 1.1.2, autocfg 1.5.1, base64 0.22.1, bit-set 0.9.1, bit-vec 0.9.1, bitflags 1.3.2, bitflags 2.13.1, blocking 1.7.0, bumpalo 3.20.3, bytemuck 1.25.2, bytemuck_derive 1.12.0, cargo-emit 0.2.1, cc 1.4.4, cfg-if 1.0.4, claxon 0.4.3, codespan-reporting 0.12.0, codespan-reporting 0.13.1, concurrent-queue 2.5.0, console_error_panic_hook 0.1.7, core-foundation 0.9.4, core-foundation-sys 0.8.7, core-graphics 0.23.2, core-graphics-types 0.1.3, coreaudio-rs 0.14.2, cpal 0.17.3, cpufeatures 0.3.1, critical-section 1.2.0, crossbeam-channel 0.5.16, crossbeam-queue 0.3.13, crossbeam-utils 0.8.22, displaydoc 0.2.7, document-features 0.2.12, downcast-rs 1.2.1, downcast-rs 2.0.2, either 1.18.0, encoding_rs 0.8.35, equivalent 1.0.2, errno 0.3.14, euclid 0.22.14, event-listener 5.4.2, event-listener-strategy 0.5.4, fastrand 2.5.0, find-msvc-tools 0.1.11, fixedbitset 0.5.7, flate2 1.1.10, fnv 1.0.7, fontique 0.9.0, futures-lite 2.6.1, gethostname 1.1.0, gltf 1.4.1, gltf-derive 1.4.1, gltf-json 1.4.1, hash32 0.3.1, hashbrown 0.15.5, hashbrown 0.16.1, hashbrown 0.17.1, heapless 0.9.3, hermit-abi 0.5.2, hound 3.5.1, indexmap 2.14.1, itertools 0.14.0, jni 0.21.1, jobserver 0.1.35, js-sys 0.3.104, lazy_static 1.5.0, linux-raw-sys 0.4.15, linux-raw-sys 0.12.1, lock_api 0.4.14, log 0.4.34, num-bigint 0.4.8, num-derive 0.4.2, num-integer 0.1.47, num-rational 0.4.2, num-traits 0.2.19, once_cell 1.21.4, parking 2.2.1, parking_lot 0.12.5, parking_lot_core 0.9.12, parlance 0.1.0, parley 0.9.0, parley_data 0.9.0, percent-encoding 2.3.2, petgraph 0.8.3, piper 0.2.5, pkg-config 0.3.34, plain 0.2.3, png 0.17.16, polling 3.11.0, presser 0.3.1, radsort 0.1.1, raw-window-metal 1.1.0, regex 1.13.1, regex-automata 0.4.18, regex-syntax 0.8.11, renderdoc-sys 1.1.0, ron 0.12.2, rustc-hash 1.1.0, rustc_version 0.4.1, rustix 0.38.44, rustix 1.1.4, scoped-tls 1.0.1, scopeguard 1.2.0, send_wrapper 0.6.0, simd_cesu8 1.2.0, smallvec 1.15.2, smol_str 0.2.2, stable_deref_trait 1.2.1, static_assertions 1.1.0, swash 0.2.10, sys-locale 0.3.2, thread_local 1.1.10, tinyvec 1.12.0, tracing-wasm 0.2.1, ttf-parser 0.25.1, unicode-segmentation 1.13.3, unicode-width 0.2.2, unicode-xid 0.2.6, utf8_iter 1.0.4, uuid 1.26.0, vec_map 0.8.2, version_check 0.9.5, wasi 0.11.1+wasi-snapshot-preview1, wasip2 1.0.4+wasi-0.2.12, wasm-bindgen 0.2.127, wasm-bindgen-futures 0.4.77, wasm-bindgen-macro 0.2.127, wasm-bindgen-macro-support 0.2.127, wasm-bindgen-shared 0.2.127, web-sys 0.3.104, web-task 1.1.3, wit-bindgen 0.57.1, wl-clipboard-rs 0.9.3, x11rb 0.13.2, x11rb-protocol 0.13.2, yazi 0.2.1, zeno 0.3.3 carry it, and the text taken for accesskit 0.24.1, accesskit_consumer 0.35.0, accesskit_consumer 0.38.0, accesskit_macos 0.26.3, accesskit_windows 0.32.1, accesskit_winit 0.32.2, bevy_embedded_assets 0.16.0, bevy_scene_macros 0.19.1, constgebra 0.1.4, dasp_sample 0.11.0, dispatch2 0.3.1, gilrs-core 0.6.8, gpu-descriptor 0.3.2, gpu-descriptor-types 0.2.0, jni 0.22.4, jni-macros 0.22.4, jni-sys-macros 0.4.1, meshopt 0.6.2, ndk 0.9.0, ndk-context 0.1.1, ndk-sys 0.6.0+11769913, objc2-app-kit 0.3.2, objc2-audio-toolbox 0.3.2, objc2-avf-audio 0.3.2, objc2-core-audio 0.3.2, objc2-core-audio-types 0.3.2, objc2-core-foundation 0.3.2, objc2-core-graphics 0.3.2, objc2-io-kit 0.3.2, objc2-io-surface 0.3.2, objc2-metal 0.3.2, objc2-quartz-core 0.3.2, profiling 1.0.18, r-efi 5.3.0, r-efi 6.0.0, spirv 0.4.0+sdk-1.4.341.0, svg_fmt 0.4.5, zune-core 0.4.12, zune-jpeg 0.4.21, whose packages hold none.

~~~~
                              Apache License
                        Version 2.0, January 2004
                     http://www.apache.org/licenses/

TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

1. Definitions.

   "License" shall mean the terms and conditions for use, reproduction,
   and distribution as defined by Sections 1 through 9 of this document.

   "Licensor" shall mean the copyright owner or entity authorized by
   the copyright owner that is granting the License.

   "Legal Entity" shall mean the union of the acting entity and all
   other entities that control, are controlled by, or are under common
   control with that entity. For the purposes of this definition,
   "control" means (i) the power, direct or indirect, to cause the
   direction or management of such entity, whether by contract or
   otherwise, or (ii) ownership of fifty percent (50%) or more of the
   outstanding shares, or (iii) beneficial ownership of such entity.

   "You" (or "Your") shall mean an individual or Legal Entity
   exercising permissions granted by this License.

   "Source" form shall mean the preferred form for making modifications,
   including but not limited to software source code, documentation
   source, and configuration files.

   "Object" form shall mean any form resulting from mechanical
   transformation or translation of a Source form, including but
   not limited to compiled object code, generated documentation,
   and conversions to other media types.

   "Work" shall mean the work of authorship, whether in Source or
   Object form, made available under the License, as indicated by a
   copyright notice that is included in or attached to the work
   (an example is provided in the Appendix below).

   "Derivative Works" shall mean any work, whether in Source or Object
   form, that is based on (or derived from) the Work and for which the
   editorial revisions, annotations, elaborations, or other modifications
   represent, as a whole, an original work of authorship. For the purposes
   of this License, Derivative Works shall not include works that remain
   separable from, or merely link (or bind by name) to the interfaces of,
   the Work and Derivative Works thereof.

   "Contribution" shall mean any work of authorship, including
   the original version of the Work and any modifications or additions
   to that Work or Derivative Works thereof, that is intentionally
   submitted to Licensor for inclusion in the Work by the copyright owner
   or by an individual or Legal Entity authorized to submit on behalf of
   the copyright owner. For the purposes of this definition, "submitted"
   means any form of electronic, verbal, or written communication sent
   to the Licensor or its representatives, including but not limited to
   communication on electronic mailing lists, source code control systems,
   and issue tracking systems that are managed by, or on behalf of, the
   Licensor for the purpose of discussing and improving the Work, but
   excluding communication that is conspicuously marked or otherwise
   designated in writing by the copyright owner as "Not a Contribution."

   "Contributor" shall mean Licensor and any individual or Legal Entity
   on behalf of whom a Contribution has been received by Licensor and
   subsequently incorporated within the Work.

2. Grant of Copyright License. Subject to the terms and conditions of
   this License, each Contributor hereby grants to You a perpetual,
   worldwide, non-exclusive, no-charge, royalty-free, irrevocable
   copyright license to reproduce, prepare Derivative Works of,
   publicly display, publicly perform, sublicense, and distribute the
   Work and such Derivative Works in Source or Object form.

3. Grant of Patent License. Subject to the terms and conditions of
   this License, each Contributor hereby grants to You a perpetual,
   worldwide, non-exclusive, no-charge, royalty-free, irrevocable
   (except as stated in this section) patent license to make, have made,
   use, offer to sell, sell, import, and otherwise transfer the Work,
   where such license applies only to those patent claims licensable
   by such Contributor that are necessarily infringed by their
   Contribution(s) alone or by combination of their Contribution(s)
   with the Work to which such Contribution(s) was submitted. If You
   institute patent litigation against any entity (including a
   cross-claim or counterclaim in a lawsuit) alleging that the Work
   or a Contribution incorporated within the Work constitutes direct
   or contributory patent infringement, then any patent licenses
   granted to You under this License for that Work shall terminate
   as of the date such litigation is filed.

4. Redistribution. You may reproduce and distribute copies of the
   Work or Derivative Works thereof in any medium, with or without
   modifications, and in Source or Object form, provided that You
   meet the following conditions:

   (a) You must give any other recipients of the Work or
       Derivative Works a copy of this License; and

   (b) You must cause any modified files to carry prominent notices
       stating that You changed the files; and

   (c) You must retain, in the Source form of any Derivative Works
       that You distribute, all copyright, patent, trademark, and
       attribution notices from the Source form of the Work,
       excluding those notices that do not pertain to any part of
       the Derivative Works; and

   (d) If the Work includes a "NOTICE" text file as part of its
       distribution, then any Derivative Works that You distribute must
       include a readable copy of the attribution notices contained
       within such NOTICE file, excluding those notices that do not
       pertain to any part of the Derivative Works, in at least one
       of the following places: within a NOTICE text file distributed
       as part of the Derivative Works; within the Source form or
       documentation, if provided along with the Derivative Works; or,
       within a display generated by the Derivative Works, if and
       wherever such third-party notices normally appear. The contents
       of the NOTICE file are for informational purposes only and
       do not modify the License. You may add Your own attribution
       notices within Derivative Works that You distribute, alongside
       or as an addendum to the NOTICE text from the Work, provided
       that such additional attribution notices cannot be construed
       as modifying the License.

   You may add Your own copyright statement to Your modifications and
   may provide additional or different license terms and conditions
   for use, reproduction, or distribution of Your modifications, or
   for any such Derivative Works as a whole, provided Your use,
   reproduction, and distribution of the Work otherwise complies with
   the conditions stated in this License.

5. Submission of Contributions. Unless You explicitly state otherwise,
   any Contribution intentionally submitted for inclusion in the Work
   by You to the Licensor shall be under the terms and conditions of
   this License, without any additional terms or conditions.
   Notwithstanding the above, nothing herein shall supersede or modify
   the terms of any separate license agreement you may have executed
   with Licensor regarding such Contributions.

6. Trademarks. This License does not grant permission to use the trade
   names, trademarks, service marks, or product names of the Licensor,
   except as required for reasonable and customary use in describing the
   origin of the Work and reproducing the content of the NOTICE file.

7. Disclaimer of Warranty. Unless required by applicable law or
   agreed to in writing, Licensor provides the Work (and each
   Contributor provides its Contributions) on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
   implied, including, without limitation, any warranties or conditions
   of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
   PARTICULAR PURPOSE. You are solely responsible for determining the
   appropriateness of using or redistributing the Work and assume any
   risks associated with Your exercise of permissions under this License.

8. Limitation of Liability. In no event and under no legal theory,
   whether in tort (including negligence), contract, or otherwise,
   unless required by applicable law (such as deliberate and grossly
   negligent acts) or agreed to in writing, shall any Contributor be
   liable to You for damages, including any direct, indirect, special,
   incidental, or consequential damages of any character arising as a
   result of this License or out of the use or inability to use the
   Work (including but not limited to damages for loss of goodwill,
   work stoppage, computer failure or malfunction, or any and all
   other commercial damages or losses), even if such Contributor
   has been advised of the possibility of such damages.

9. Accepting Warranty or Additional Liability. While redistributing
   the Work or Derivative Works thereof, You may choose to offer,
   and charge a fee for, acceptance of support, warranty, indemnity,
   or other liability obligations and/or rights consistent with this
   License. However, in accepting such obligations, You may act only
   on Your own behalf and on Your sole responsibility, not on behalf
   of any other Contributor, and only if You agree to indemnify,
   defend, and hold each Contributor harmless for any liability
   incurred by, or claims asserted against, such Contributor by reason
   of your accepting any such warranty or additional liability.

END OF TERMS AND CONDITIONS

APPENDIX: How to apply the Apache License to your work.

   To apply the Apache License to your work, attach the following
   boilerplate notice, with the fields enclosed by brackets "[]"
   replaced with your own identifying information. (Don't include
   the brackets!)  The text should be enclosed in the appropriate
   comment syntax for the file format. We also recommend that a
   file or class name and description of purpose be included on the
   same "printed page" as the copyright notice for easier
   identification within third-party archives.

Copyright [yyyy] [name of copyright owner]

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

	http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
~~~~

### Text 6

A notice, as aho-corasick 1.1.5, byteorder 1.5.0, memchr 2.8.3, same-file 1.0.6, termcolor 1.4.1, walkdir 2.5.0, winapi-util 0.1.11 carry it.

~~~~
This project is dual-licensed under the Unlicense and MIT licenses.

You may use this code under the terms of either license.
~~~~

### Text 7

MIT, as aho-corasick 1.1.5, async-broadcast 0.7.2, base64 0.22.1, byteorder 1.5.0, byteorder-lite 0.1.0, combine 4.6.8, crossbeam-channel 0.5.16, crossbeam-queue 0.3.13, crossbeam-utils 0.8.22, crunchy 0.2.4, data-encoding 2.11.1, derive_more 2.1.1, derive_more-impl 2.1.1, fsevent-sys 4.1.0, harfrust 0.6.2, jni 0.21.1, memchr 2.8.3, nix 0.31.3, nu-ansi-term 0.50.3, orbclient 0.3.55, os_pipe 1.2.3, quick-xml 0.41.0, same-file 1.0.6, shlex 2.0.1, spin 0.10.1, termcolor 1.4.1, tracing-core 0.1.36, twox-hash 2.1.4, walkdir 2.5.0, winapi-util 0.1.11, xkbcommon-dl 0.4.2 carry it.

~~~~
The MIT License (MIT)


Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
~~~~

### Text 8

Unlicense, as aho-corasick 1.1.5, byteorder 1.5.0, byteorder-lite 0.1.0, memchr 2.8.3, same-file 1.0.6, termcolor 1.4.1, walkdir 2.5.0, winapi-util 0.1.11 carry it.

~~~~
This is free and unencumbered software released into the public domain.

Anyone is free to copy, modify, publish, use, compile, sell, or
distribute this software, either in source code form or as a compiled
binary, for any purpose, commercial or non-commercial, and by any
means.

In jurisdictions that recognize copyright laws, the author or authors
of this software dedicate any and all copyright interest in the
software to the public domain. We make this dedication for the benefit
of the public at large and to the detriment of our heirs and
successors. We intend this dedication to be an overt act of
relinquishment in perpetuity of all present and future rights to this
software under copyright law.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

For more information, please refer to <http://unlicense.org/>
~~~~

### Text 9

MIT, as alsa 0.11.0, alsa-sys 0.4.0, android-activity 0.6.1, android-properties 0.2.2, arboard 3.6.1, assert_type_match 0.1.1, atomicow 1.2.0, bevy 0.19.1, bevy_a11y 0.19.1, bevy_android 0.19.1, bevy_animation 0.19.1, bevy_animation_macros 0.19.1, bevy_anti_alias 0.19.1, bevy_app 0.19.1, bevy_asset 0.19.1, bevy_asset_macros 0.19.1, bevy_audio 0.19.1, bevy_camera 0.19.1, bevy_clipboard 0.19.1, bevy_color 0.19.1, bevy_core_pipeline 0.19.1, bevy_derive 0.19.1, bevy_diagnostic 0.19.1, bevy_ecs 0.19.1, bevy_ecs_macro_logic 0.19.1, bevy_ecs_macros 0.19.1, bevy_encase_derive 0.19.1, bevy_gilrs 0.19.1, bevy_gizmos 0.19.1, bevy_gizmos_macros 0.19.1, bevy_gizmos_render 0.19.1, bevy_gltf 0.19.1, bevy_image 0.19.1, bevy_input 0.19.1, bevy_input_focus 0.19.1, bevy_internal 0.19.1, bevy_light 0.19.1, bevy_log 0.19.1, bevy_macro_utils 0.19.1, bevy_material 0.19.1, bevy_material_macros 0.19.1, bevy_math 0.19.1, bevy_mesh 0.19.1, bevy_pbr 0.19.1, bevy_picking 0.19.1, bevy_platform 0.19.1, bevy_post_process 0.19.1, bevy_ptr 0.19.1, bevy_reflect 0.19.1, bevy_reflect_derive 0.19.1, bevy_render 0.19.1, bevy_render_macros 0.19.1, bevy_scene 0.19.1, bevy_shader 0.19.1, bevy_solari 0.19.1, bevy_sprite 0.19.1, bevy_sprite_render 0.19.1, bevy_state 0.19.1, bevy_state_macros 0.19.1, bevy_tasks 0.19.1, bevy_text 0.19.1, bevy_time 0.19.1, bevy_transform 0.19.1, bevy_ui 0.19.1, bevy_ui_render 0.19.1, bevy_ui_widgets 0.19.1, bevy_utils 0.19.1, bevy_weather 0.2.0, bevy_window 0.19.1, bevy_winit 0.19.1, bevy_world_serialization 0.19.1, bitvec 1.1.1, cargo-emit 0.2.1, cfg_aliases 0.2.2, const-fnv1a-hash 1.1.0, convert_case 0.10.0, core_maths 0.1.1, crc32fast 1.5.1, disqualified 1.0.0, fdeflate 0.3.7, funty 2.0.0, grid 1.0.1, guillotiere 0.6.2, half 2.7.1, image 0.25.6, image-webp 0.2.4, libredox 0.1.21, linebender_resource_handle 0.1.1, metis 0.2.2, miniz_oxide 0.8.9, miniz_oxide 0.9.1, naga 29.0.4, naga_oil 0.22.0, offset-allocator 0.2.0, radium 0.7.0, range-alloc 0.1.5, raw-window-handle 0.6.2, redox_syscall 0.4.1, redox_syscall 0.5.18, redox_syscall 0.9.3, ruzstd 0.8.3, sctk-adwaita 0.10.1, simd-adler32 0.3.10, simdutf8 0.1.5, static_assertions 1.1.0, sys-locale 0.3.2, tap 1.0.1, tinyvec_macros 0.1.1, tree_magic_mini 3.2.2, variadics_please 1.1.0, weak-table 0.3.2, web-time 1.1.0, wgpu 29.0.4, wgpu-core 29.0.4, wgpu-core-deps-apple 29.0.4, wgpu-core-deps-windows-linux-android 29.0.4, wgpu-hal 29.0.4, wgpu-naga-bridge 29.0.4, wgpu-types 29.0.4, wyz 0.5.1, xcursor 0.3.11 carry it.

~~~~
MIT License


Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
~~~~

### Text 10

Apache-2.0, as android-activity 0.6.1 carries it.

~~~~
# License

## GameActivity

The third-party glue code, under the game-activity-csrc/ directory is covered by
the Apache 2.0 license only:

Apache License, Version 2.0 (LICENSE-APACHE or <http://www.apache.org/licenses/LICENSE-2.0>)

## SDK Documentation

Documentation for APIs that are direct bindings of Android platform APIs are covered
by the Apache 2.0 license only:

Apache License, Version 2.0 (LICENSE-APACHE or <http://www.apache.org/licenses/LICENSE-2.0>)

## android-activity

All other code is dual-licensed under either

- MIT License (LICENSE-MIT or <http://opensource.org/licenses/MIT>)
- Apache License, Version 2.0 (LICENSE-APACHE or <http://www.apache.org/licenses/LICENSE-2.0>)

at your option.
~~~~

### Text 11

Apache-2.0, as android_log-sys 0.3.2 carries it.

~~~~
Apache License
Version 2.0, January 2004
http://www.apache.org/licenses/

TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

1. Definitions.

"License" shall mean the terms and conditions for use, reproduction,
and distribution as defined by Sections 1 through 9 of this document.

"Licensor" shall mean the copyright owner or entity authorized by
the copyright owner that is granting the License.

"Legal Entity" shall mean the union of the acting entity and all
other entities that control, are controlled by, or are under common
control with that entity. For the purposes of this definition,
"control" means (i) the power, direct or indirect, to cause the
direction or management of such entity, whether by contract or
otherwise, or (ii) ownership of fifty percent (50%) or more of the
outstanding shares, or (iii) beneficial ownership of such entity.

"You" (or "Your") shall mean an individual or Legal Entity
exercising permissions granted by this License.

"Source" form shall mean the preferred form for making modifications,
including but not limited to software source code, documentation
source, and configuration files.

"Object" form shall mean any form resulting from mechanical
transformation or translation of a Source form, including but
not limited to compiled object code, generated documentation,
and conversions to other media types.

"Work" shall mean the work of authorship, whether in Source or
Object form, made available under the License, as indicated by a
copyright notice that is included in or attached to the work
(an example is provided in the Appendix below).

"Derivative Works" shall mean any work, whether in Source or Object
form, that is based on (or derived from) the Work and for which the
editorial revisions, annotations, elaborations, or other modifications
represent, as a whole, an original work of authorship. For the purposes
of this License, Derivative Works shall not include works that remain
separable from, or merely link (or bind by name) to the interfaces of,
the Work and Derivative Works thereof.

"Contribution" shall mean any work of authorship, including
the original version of the Work and any modifications or additions
to that Work or Derivative Works thereof, that is intentionally
submitted to Licensor for inclusion in the Work by the copyright owner
or by an individual or Legal Entity authorized to submit on behalf of
the copyright owner. For the purposes of this definition, "submitted"
means any form of electronic, verbal, or written communication sent
to the Licensor or its representatives, including but not limited to
communication on electronic mailing lists, source code control systems,
and issue tracking systems that are managed by, or on behalf of, the
Licensor for the purpose of discussing and improving the Work, but
excluding communication that is conspicuously marked or otherwise
designated in writing by the copyright owner as "Not a Contribution."

"Contributor" shall mean Licensor and any individual or Legal Entity
on behalf of whom a Contribution has been received by Licensor and
subsequently incorporated within the Work.

2. Grant of Copyright License. Subject to the terms and conditions of
this License, each Contributor hereby grants to You a perpetual,
worldwide, non-exclusive, no-charge, royalty-free, irrevocable
copyright license to reproduce, prepare Derivative Works of,
publicly display, publicly perform, sublicense, and distribute the
Work and such Derivative Works in Source or Object form.

3. Grant of Patent License. Subject to the terms and conditions of
this License, each Contributor hereby grants to You a perpetual,
worldwide, non-exclusive, no-charge, royalty-free, irrevocable
(except as stated in this section) patent license to make, have made,
use, offer to sell, sell, import, and otherwise transfer the Work,
where such license applies only to those patent claims licensable
by such Contributor that are necessarily infringed by their
Contribution(s) alone or by combination of their Contribution(s)
with the Work to which such Contribution(s) was submitted. If You
institute patent litigation against any entity (including a
cross-claim or counterclaim in a lawsuit) alleging that the Work
or a Contribution incorporated within the Work constitutes direct
or contributory patent infringement, then any patent licenses
granted to You under this License for that Work shall terminate
as of the date such litigation is filed.

4. Redistribution. You may reproduce and distribute copies of the
Work or Derivative Works thereof in any medium, with or without
modifications, and in Source or Object form, provided that You
meet the following conditions:

(a) You must give any other recipients of the Work or
Derivative Works a copy of this License; and

(b) You must cause any modified files to carry prominent notices
stating that You changed the files; and

(c) You must retain, in the Source form of any Derivative Works
that You distribute, all copyright, patent, trademark, and
attribution notices from the Source form of the Work,
excluding those notices that do not pertain to any part of
the Derivative Works; and

(d) If the Work includes a "NOTICE" text file as part of its
distribution, then any Derivative Works that You distribute must
include a readable copy of the attribution notices contained
within such NOTICE file, excluding those notices that do not
pertain to any part of the Derivative Works, in at least one
of the following places: within a NOTICE text file distributed
as part of the Derivative Works; within the Source form or
documentation, if provided along with the Derivative Works; or,
within a display generated by the Derivative Works, if and
wherever such third-party notices normally appear. The contents
of the NOTICE file are for informational purposes only and
do not modify the License. You may add Your own attribution
notices within Derivative Works that You distribute, alongside
or as an addendum to the NOTICE text from the Work, provided
that such additional attribution notices cannot be construed
as modifying the License.

You may add Your own copyright statement to Your modifications and
may provide additional or different license terms and conditions
for use, reproduction, or distribution of Your modifications, or
for any such Derivative Works as a whole, provided Your use,
reproduction, and distribution of the Work otherwise complies with
the conditions stated in this License.

5. Submission of Contributions. Unless You explicitly state otherwise,
any Contribution intentionally submitted for inclusion in the Work
by You to the Licensor shall be under the terms and conditions of
this License, without any additional terms or conditions.
Notwithstanding the above, nothing herein shall supersede or modify
the terms of any separate license agreement you may have executed
with Licensor regarding such Contributions.

6. Trademarks. This License does not grant permission to use the trade
names, trademarks, service marks, or product names of the Licensor,
except as required for reasonable and customary use in describing the
origin of the Work and reproducing the content of the NOTICE file.

7. Disclaimer of Warranty. Unless required by applicable law or
agreed to in writing, Licensor provides the Work (and each
Contributor provides its Contributions) on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied, including, without limitation, any warranties or conditions
of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
PARTICULAR PURPOSE. You are solely responsible for determining the
appropriateness of using or redistributing the Work and assume any
risks associated with Your exercise of permissions under this License.

8. Limitation of Liability. In no event and under no legal theory,
whether in tort (including negligence), contract, or otherwise,
unless required by applicable law (such as deliberate and grossly
negligent acts) or agreed to in writing, shall any Contributor be
liable to You for damages, including any direct, indirect, special,
incidental, or consequential damages of any character arising as a
result of this License or out of the use or inability to use the
Work (including but not limited to damages for loss of goodwill,
work stoppage, computer failure or malfunction, or any and all
other commercial damages or losses), even if such Contributor
has been advised of the possibility of such damages.

9. Accepting Warranty or Additional Liability. While redistributing
the Work or Derivative Works thereof, You may choose to offer,
and charge a fee for, acceptance of support, warranty, indemnity,
or other liability obligations and/or rights consistent with this
License. However, in accepting such obligations, You may act only
on Your own behalf and on Your sole responsibility, not on behalf
of any other Contributor, and only if You agree to indemnify,
defend, and hold each Contributor harmless for any liability
incurred by, or claims asserted against, such Contributor by reason
of your accepting any such warranty or additional liability.

END OF TERMS AND CONDITIONS

APPENDIX: How to apply the Apache License to your work.

To apply the Apache License to your work, attach the following
boilerplate notice, with the fields enclosed by brackets "{}"
replaced with your own identifying information. (Don't include
the brackets!)  The text should be enclosed in the appropriate
comment syntax for the file format. We also recommend that a
file or class name and description of purpose be included on the
same "printed page" as the copyright notice for easier
identification within third-party archives.


Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
~~~~

### Text 12

Apache-2.0, as android_system_properties 0.1.6, shlex 2.0.1 carry it.

~~~~
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
~~~~

### Text 13

MIT, as android_system_properties 0.1.6, lz4_flex 0.13.1, version_check 0.9.5 carry it.

~~~~
The MIT License (MIT)


Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
~~~~

### Text 14

BSD-2-Clause, as arrayref 0.3.9, mach2 0.5.0 carry it.

~~~~
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:

1. Redistributions of source code must retain the above copyright
   notice, this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright
   notice, this list of conditions and the following disclaimer in the
   documentation and/or other materials provided with the
   distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
~~~~

### Text 15

Apache-2.0, as ash 0.38.0+1.3.281, font-types 0.11.3, font-types 0.12.4, read-fonts 0.39.2, read-fonts 0.41.0, skrifa 0.42.1, skrifa 0.44.0 carry it.

~~~~
Apache License

Version 2.0, January 2004

http://www.apache.org/licenses/

TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

1. Definitions.

"License" shall mean the terms and conditions for use, reproduction, and distribution as defined by Sections 1 through 9 of this document.

"Licensor" shall mean the copyright owner or entity authorized by the copyright owner that is granting the License.

"Legal Entity" shall mean the union of the acting entity and all other entities that control, are controlled by, or are under common control with that entity. For the purposes of this definition, "control" means (i) the power, direct or indirect, to cause the direction or management of such entity, whether by contract or otherwise, or (ii) ownership of fifty percent (50%) or more of the outstanding shares, or (iii) beneficial ownership of such entity.

"You" (or "Your") shall mean an individual or Legal Entity exercising permissions granted by this License.

"Source" form shall mean the preferred form for making modifications, including but not limited to software source code, documentation source, and configuration files.

"Object" form shall mean any form resulting from mechanical transformation or translation of a Source form, including but not limited to compiled object code, generated documentation, and conversions to other media types.

"Work" shall mean the work of authorship, whether in Source or Object form, made available under the License, as indicated by a copyright notice that is included in or attached to the work (an example is provided in the Appendix below).

"Derivative Works" shall mean any work, whether in Source or Object form, that is based on (or derived from) the Work and for which the editorial revisions, annotations, elaborations, or other modifications represent, as a whole, an original work of authorship. For the purposes of this License, Derivative Works shall not include works that remain separable from, or merely link (or bind by name) to the interfaces of, the Work and Derivative Works thereof.

"Contribution" shall mean any work of authorship, including the original version of the Work and any modifications or additions to that Work or Derivative Works thereof, that is intentionally submitted to Licensor for inclusion in the Work by the copyright owner or by an individual or Legal Entity authorized to submit on behalf of the copyright owner. For the purposes of this definition, "submitted" means any form of electronic, verbal, or written communication sent to the Licensor or its representatives, including but not limited to communication on electronic mailing lists, source code control systems, and issue tracking systems that are managed by, or on behalf of, the Licensor for the purpose of discussing and improving the Work, but excluding communication that is conspicuously marked or otherwise designated in writing by the copyright owner as "Not a Contribution."

"Contributor" shall mean Licensor and any individual or Legal Entity on behalf of whom a Contribution has been received by Licensor and subsequently incorporated within the Work.

2. Grant of Copyright License. Subject to the terms and conditions of this License, each Contributor hereby grants to You a perpetual, worldwide, non-exclusive, no-charge, royalty-free, irrevocable copyright license to reproduce, prepare Derivative Works of, publicly display, publicly perform, sublicense, and distribute the Work and such Derivative Works in Source or Object form.

3. Grant of Patent License. Subject to the terms and conditions of this License, each Contributor hereby grants to You a perpetual, worldwide, non-exclusive, no-charge, royalty-free, irrevocable (except as stated in this section) patent license to make, have made, use, offer to sell, sell, import, and otherwise transfer the Work, where such license applies only to those patent claims licensable by such Contributor that are necessarily infringed by their Contribution(s) alone or by combination of their Contribution(s) with the Work to which such Contribution(s) was submitted. If You institute patent litigation against any entity (including a cross-claim or counterclaim in a lawsuit) alleging that the Work or a Contribution incorporated within the Work constitutes direct or contributory patent infringement, then any patent licenses granted to You under this License for that Work shall terminate as of the date such litigation is filed.

4. Redistribution. You may reproduce and distribute copies of the Work or Derivative Works thereof in any medium, with or without modifications, and in Source or Object form, provided that You meet the following conditions:

You must give any other recipients of the Work or Derivative Works a copy of this License; and
You must cause any modified files to carry prominent notices stating that You changed the files; and
You must retain, in the Source form of any Derivative Works that You distribute, all copyright, patent, trademark, and attribution notices from the Source form of the Work, excluding those notices that do not pertain to any part of the Derivative Works; and
If the Work includes a "NOTICE" text file as part of its distribution, then any Derivative Works that You distribute must include a readable copy of the attribution notices contained within such NOTICE file, excluding those notices that do not pertain to any part of the Derivative Works, in at least one of the following places: within a NOTICE text file distributed as part of the Derivative Works; within the Source form or documentation, if provided along with the Derivative Works; or, within a display generated by the Derivative Works, if and wherever such third-party notices normally appear. The contents of the NOTICE file are for informational purposes only and do not modify the License. You may add Your own attribution notices within Derivative Works that You distribute, alongside or as an addendum to the NOTICE text from the Work, provided that such additional attribution notices cannot be construed as modifying the License.

You may add Your own copyright statement to Your modifications and may provide additional or different license terms and conditions for use, reproduction, or distribution of Your modifications, or for any such Derivative Works as a whole, provided Your use, reproduction, and distribution of the Work otherwise complies with the conditions stated in this License.
5. Submission of Contributions. Unless You explicitly state otherwise, any Contribution intentionally submitted for inclusion in the Work by You to the Licensor shall be under the terms and conditions of this License, without any additional terms or conditions. Notwithstanding the above, nothing herein shall supersede or modify the terms of any separate license agreement you may have executed with Licensor regarding such Contributions.

6. Trademarks. This License does not grant permission to use the trade names, trademarks, service marks, or product names of the Licensor, except as required for reasonable and customary use in describing the origin of the Work and reproducing the content of the NOTICE file.

7. Disclaimer of Warranty. Unless required by applicable law or agreed to in writing, Licensor provides the Work (and each Contributor provides its Contributions) on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied, including, without limitation, any warranties or conditions of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A PARTICULAR PURPOSE. You are solely responsible for determining the appropriateness of using or redistributing the Work and assume any risks associated with Your exercise of permissions under this License.

8. Limitation of Liability. In no event and under no legal theory, whether in tort (including negligence), contract, or otherwise, unless required by applicable law (such as deliberate and grossly negligent acts) or agreed to in writing, shall any Contributor be liable to You for damages, including any direct, indirect, special, incidental, or consequential damages of any character arising as a result of this License or out of the use or inability to use the Work (including but not limited to damages for loss of goodwill, work stoppage, computer failure or malfunction, or any and all other commercial damages or losses), even if such Contributor has been advised of the possibility of such damages.

9. Accepting Warranty or Additional Liability. While redistributing the Work or Derivative Works thereof, You may choose to offer, and charge a fee for, acceptance of support, warranty, indemnity, or other liability obligations and/or rights consistent with this License. However, in accepting such obligations, You may act only on Your own behalf and on Your sole responsibility, not on behalf of any other Contributor, and only if You agree to indemnify, defend, and hold each Contributor harmless for any liability incurred by, or claims asserted against, such Contributor by reason of your accepting any such warranty or additional liability.

END OF TERMS AND CONDITIONS


Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
~~~~

### Text 16

Apache-2.0, as async-broadcast 0.7.2 carries it.

~~~~
                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS


   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
~~~~

### Text 17

Apache-2.0, as atomic-waker 1.1.2, futures-lite 2.6.1 carry it.

~~~~
===============================================================================


Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

	http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

===============================================================================


Permission is hereby granted, free of charge, to any
person obtaining a copy of this software and associated
documentation files (the "Software"), to deal in the
Software without restriction, including without
limitation the rights to use, copy, modify, merge,
publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software
is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice
shall be included in all copies or substantial portions
of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF
ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT
SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.
~~~~

### Text 18

Apache-2.0, as blake3 1.8.7, const_soft_float 0.1.4, ctrlc 3.5.2, cursor-icon 1.2.0, file-id 0.2.3, futures-channel 0.3.34, futures-core 0.3.34, futures-io 0.3.34, futures-macro 0.3.34, futures-task 0.3.34, futures-util 0.3.34, glam 0.32.1, gpu-allocator 0.28.0, ktx2 0.5.0, memmap2 0.9.11, metis 0.2.2, notify-debouncer-full 0.7.0, notify-types 2.1.0, rectangle-pack 0.4.2, tinyvec_macros 0.1.1, web-time 1.1.0, windows 0.62.2, windows-collections 0.3.2, windows-core 0.62.2, windows-future 0.3.2, windows-implement 0.60.2, windows-interface 0.59.3, windows-link 0.2.1, windows-numerics 0.3.1, windows-result 0.4.1, windows-strings 0.5.1, windows-sys 0.45.0, windows-sys 0.52.0, windows-sys 0.59.0, windows-sys 0.60.2, windows-sys 0.61.2, windows-targets 0.42.2, windows-targets 0.52.6, windows-targets 0.53.5, windows-threading 0.2.1, windows_aarch64_gnullvm 0.42.2, windows_aarch64_gnullvm 0.52.6, windows_aarch64_gnullvm 0.53.1, windows_aarch64_msvc 0.42.2, windows_aarch64_msvc 0.52.6, windows_aarch64_msvc 0.53.1, windows_i686_gnu 0.42.2, windows_i686_gnu 0.52.6, windows_i686_gnu 0.53.1, windows_i686_gnullvm 0.52.6, windows_i686_gnullvm 0.53.1, windows_i686_msvc 0.42.2, windows_i686_msvc 0.52.6, windows_i686_msvc 0.53.1, windows_x86_64_gnu 0.42.2, windows_x86_64_gnu 0.52.6, windows_x86_64_gnu 0.53.1, windows_x86_64_gnullvm 0.42.2, windows_x86_64_gnullvm 0.52.6, windows_x86_64_gnullvm 0.53.1, windows_x86_64_msvc 0.42.2, windows_x86_64_msvc 0.52.6, windows_x86_64_msvc 0.53.1, xkeysym 0.2.1, zerocopy 0.8.56, zerocopy-derive 0.8.56 carry it.

~~~~
                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS

   APPENDIX: How to apply the Apache License to your work.

      To apply the Apache License to your work, attach the following
      boilerplate notice, with the fields enclosed by brackets "[]"
      replaced with your own identifying information. (Don't include
      the brackets!)  The text should be enclosed in the appropriate
      comment syntax for the file format. We also recommend that a
      file or class name and description of purpose be included on the
      same "printed page" as the copyright notice for easier
      identification within third-party archives.


   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
~~~~

### Text 19

Apache-2.0, as blake3 1.8.7 carries it.

~~~~
                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

    TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

    1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

    2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

    3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

    4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

    5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

    6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

    7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

    8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

    9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

    END OF TERMS AND CONDITIONS

    APPENDIX: How to apply the Apache License to your work.

      To apply the Apache License to your work, attach the following
      boilerplate notice, with the fields enclosed by brackets "[]"
      replaced with your own identifying information. (Don't include
      the brackets!)  The text should be enclosed in the appropriate
      comment syntax for the file format. We also recommend that a
      file or class name and description of purpose be included on the
      same "printed page" as the copyright notice for easier
      identification within third-party archives.

    Copyright 2019 Jack O'Connor and Samuel Neves

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.


---- LLVM Exceptions to the Apache 2.0 License ----

As an exception, if, as a result of your compiling your source code, portions
of this Software are embedded into an Object form of such source code, you
may redistribute such embedded portions in such Object form without complying
with the conditions of Sections 4(a), 4(b) and 4(d) of the License.

In addition, if you combine or link compiled forms of this Software with
software that is licensed under the GPLv2 ("Combined Software") and if a
court of competent jurisdiction determines that the patent provision (Section
3), the indemnity provision (Section 9) or other Section of the License
conflicts with the conditions of the GPLv2, you may retroactively and
prospectively choose to deem waived or otherwise exclude such Section(s) of
the License, but only in their entirety and only with respect to the Combined
Software.
~~~~

### Text 20

CC0-1.0, as blake3 1.8.7, constant_time_eq 0.4.2 carry it, and the text taken for hexf-parse 0.2.1, whose packages hold none.

~~~~
Creative Commons Legal Code

CC0 1.0 Universal

    CREATIVE COMMONS CORPORATION IS NOT A LAW FIRM AND DOES NOT PROVIDE
    LEGAL SERVICES. DISTRIBUTION OF THIS DOCUMENT DOES NOT CREATE AN
    ATTORNEY-CLIENT RELATIONSHIP. CREATIVE COMMONS PROVIDES THIS
    INFORMATION ON AN "AS-IS" BASIS. CREATIVE COMMONS MAKES NO WARRANTIES
    REGARDING THE USE OF THIS DOCUMENT OR THE INFORMATION OR WORKS
    PROVIDED HEREUNDER, AND DISCLAIMS LIABILITY FOR DAMAGES RESULTING FROM
    THE USE OF THIS DOCUMENT OR THE INFORMATION OR WORKS PROVIDED
    HEREUNDER.

Statement of Purpose

The laws of most jurisdictions throughout the world automatically confer
exclusive Copyright and Related Rights (defined below) upon the creator
and subsequent owner(s) (each and all, an "owner") of an original work of
authorship and/or a database (each, a "Work").

Certain owners wish to permanently relinquish those rights to a Work for
the purpose of contributing to a commons of creative, cultural and
scientific works ("Commons") that the public can reliably and without fear
of later claims of infringement build upon, modify, incorporate in other
works, reuse and redistribute as freely as possible in any form whatsoever
and for any purposes, including without limitation commercial purposes.
These owners may contribute to the Commons to promote the ideal of a free
culture and the further production of creative, cultural and scientific
works, or to gain reputation or greater distribution for their Work in
part through the use and efforts of others.

For these and/or other purposes and motivations, and without any
expectation of additional consideration or compensation, the person
associating CC0 with a Work (the "Affirmer"), to the extent that he or she
is an owner of Copyright and Related Rights in the Work, voluntarily
elects to apply CC0 to the Work and publicly distribute the Work under its
terms, with knowledge of his or her Copyright and Related Rights in the
Work and the meaning and intended legal effect of CC0 on those rights.

1. Copyright and Related Rights. A Work made available under CC0 may be
protected by copyright and related or neighboring rights ("Copyright and
Related Rights"). Copyright and Related Rights include, but are not
limited to, the following:

  i. the right to reproduce, adapt, distribute, perform, display,
     communicate, and translate a Work;
 ii. moral rights retained by the original author(s) and/or performer(s);
iii. publicity and privacy rights pertaining to a person's image or
     likeness depicted in a Work;
 iv. rights protecting against unfair competition in regards to a Work,
     subject to the limitations in paragraph 4(a), below;
  v. rights protecting the extraction, dissemination, use and reuse of data
     in a Work;
 vi. database rights (such as those arising under Directive 96/9/EC of the
     European Parliament and of the Council of 11 March 1996 on the legal
     protection of databases, and under any national implementation
     thereof, including any amended or successor version of such
     directive); and
vii. other similar, equivalent or corresponding rights throughout the
     world based on applicable law or treaty, and any national
     implementations thereof.

2. Waiver. To the greatest extent permitted by, but not in contravention
of, applicable law, Affirmer hereby overtly, fully, permanently,
irrevocably and unconditionally waives, abandons, and surrenders all of
Affirmer's Copyright and Related Rights and associated claims and causes
of action, whether now known or unknown (including existing as well as
future claims and causes of action), in the Work (i) in all territories
worldwide, (ii) for the maximum duration provided by applicable law or
treaty (including future time extensions), (iii) in any current or future
medium and for any number of copies, and (iv) for any purpose whatsoever,
including without limitation commercial, advertising or promotional
purposes (the "Waiver"). Affirmer makes the Waiver for the benefit of each
member of the public at large and to the detriment of Affirmer's heirs and
successors, fully intending that such Waiver shall not be subject to
revocation, rescission, cancellation, termination, or any other legal or
equitable action to disrupt the quiet enjoyment of the Work by the public
as contemplated by Affirmer's express Statement of Purpose.

3. Public License Fallback. Should any part of the Waiver for any reason
be judged legally invalid or ineffective under applicable law, then the
Waiver shall be preserved to the maximum extent permitted taking into
account Affirmer's express Statement of Purpose. In addition, to the
extent the Waiver is so judged Affirmer hereby grants to each affected
person a royalty-free, non transferable, non sublicensable, non exclusive,
irrevocable and unconditional license to exercise Affirmer's Copyright and
Related Rights in the Work (i) in all territories worldwide, (ii) for the
maximum duration provided by applicable law or treaty (including future
time extensions), (iii) in any current or future medium and for any number
of copies, and (iv) for any purpose whatsoever, including without
limitation commercial, advertising or promotional purposes (the
"License"). The License shall be deemed effective as of the date CC0 was
applied by Affirmer to the Work. Should any part of the License for any
reason be judged legally invalid or ineffective under applicable law, such
partial invalidity or ineffectiveness shall not invalidate the remainder
of the License, and in such case Affirmer hereby affirms that he or she
will not (i) exercise any of his or her remaining Copyright and Related
Rights in the Work or (ii) assert any associated claims and causes of
action with respect to the Work, in either case contrary to Affirmer's
express Statement of Purpose.

4. Limitations and Disclaimers.

 a. No trademark or patent rights held by Affirmer are waived, abandoned,
    surrendered, licensed or otherwise affected by this document.
 b. Affirmer offers the Work as-is and makes no representations or
    warranties of any kind concerning the Work, express, implied,
    statutory or otherwise, including without limitation warranties of
    title, merchantability, fitness for a particular purpose, non
    infringement, or the absence of latent or other defects, accuracy, or
    the present or absence of errors, whether or not discoverable, all to
    the greatest extent permissible under applicable law.
 c. Affirmer disclaims responsibility for clearing rights of other persons
    that may apply to the Work or any use thereof, including without
    limitation any person's Copyright and Related Rights in the Work.
    Further, Affirmer disclaims responsibility for obtaining any necessary
    consents, permissions or other rights required for any use of the
    Work.
 d. Affirmer understands and acknowledges that Creative Commons is not a
    party to this document and has no duty or obligation with respect to
    this CC0 or use of the Work.
~~~~

### Text 21

MIT, as bytemuck 1.25.2, bytemuck_derive 1.12.0 carry it.

~~~~
MIT License


Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (including the next paragraph) shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
~~~~

### Text 22

Zlib, as bytemuck 1.25.2, bytemuck_derive 1.12.0, const_panic 0.2.17, cursor-icon 1.2.0, foldhash 0.1.5, foldhash 0.2.0, gilrs 0.11.2, miniz_oxide 0.8.9, miniz_oxide 0.9.1, raw-window-handle 0.6.2, slotmap 1.1.1, tinyvec 1.12.0, typewit 1.15.2, xkeysym 0.2.1 carry it, and the text taken for dispatch2 0.3.1, objc2-app-kit 0.3.2, objc2-audio-toolbox 0.3.2, objc2-avf-audio 0.3.2, objc2-core-audio 0.3.2, objc2-core-audio-types 0.3.2, objc2-core-foundation 0.3.2, objc2-core-graphics 0.3.2, objc2-io-kit 0.3.2, objc2-io-surface 0.3.2, objc2-metal 0.3.2, objc2-quartz-core 0.3.2, zune-core 0.4.12, zune-jpeg 0.4.21, whose packages hold none.

~~~~
This software is provided 'as-is', without any express or implied warranty. In no event will the authors be held liable for any damages arising from the use of this software.

Permission is granted to anyone to use this software for any purpose, including commercial applications, and to alter it and redistribute it freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not claim that you wrote the original software. If you use this software in a product, an acknowledgment in the product documentation would be appreciated but is not required.

2. Altered source versions must be plainly marked as such, and must not be misrepresented as being the original software.

3. This notice may not be removed or altered from any source distribution.
~~~~

### Text 23

Apache-2.0, as cesu8 1.1.0 carries it.

~~~~
Short version for non-lawyers:

The Rust Project is dual-licensed under Apache 2.0 and MIT
terms.


Longer version:

The Rust Project is copyright 2014, The Rust Project
Developers (given in the file AUTHORS.txt).

Licensed under the Apache License, Version 2.0
<LICENSE-APACHE or
http://www.apache.org/licenses/LICENSE-2.0> or the MIT
license <LICENSE-MIT or http://opensource.org/licenses/MIT>,
at your option. All files in the project carrying such
notice may not be copied, modified, or distributed except
according to those terms.


The Rust Project includes packages written by third parties.
The following third party packages are included, and carry
their own copyright notices and license terms:

* Two header files that are part of the Valgrind
  package. These files are found at src/rt/vg/valgrind.h and
  src/rt/vg/memcheck.h, within this distribution. These files
  are redistributed under the following terms, as noted in
  them:

  for src/rt/vg/valgrind.h:

    This file is part of Valgrind, a dynamic binary
    instrumentation framework.

    Copyright (C) 2000-2010 Julian Seward.  All rights
    reserved.

    Redistribution and use in source and binary forms, with
    or without modification, are permitted provided that the
    following conditions are met:

    1. Redistributions of source code must retain the above
       copyright notice, this list of conditions and the
       following disclaimer.

    2. The origin of this software must not be
       misrepresented; you must not claim that you wrote the
       original software.  If you use this software in a
       product, an acknowledgment in the product
       documentation would be appreciated but is not
       required.

    3. Altered source versions must be plainly marked as
       such, and must not be misrepresented as being the
       original software.

    4. The name of the author may not be used to endorse or
       promote products derived from this software without
       specific prior written permission.

    THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND
    ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
    LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
    AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN
    NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT,
    INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
    CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
    PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
    USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
    CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
    CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
    NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
    USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
    OF SUCH DAMAGE.

  for src/rt/vg/memcheck.h:

    This file is part of MemCheck, a heavyweight Valgrind
    tool for detecting memory errors.

    Copyright (C) 2000-2010 Julian Seward.  All rights
    reserved.

    Redistribution and use in source and binary forms, with
    or without modification, are permitted provided that the
    following conditions are met:

    1. Redistributions of source code must retain the above
       copyright notice, this list of conditions and the
       following disclaimer.

    2. The origin of this software must not be
       misrepresented; you must not claim that you wrote the
       original software.  If you use this software in a
       product, an acknowledgment in the product
       documentation would be appreciated but is not
       required.

    3. Altered source versions must be plainly marked as
       such, and must not be misrepresented as being the
       original software.

    4. The name of the author may not be used to endorse or
       promote products derived from this software without
       specific prior written permission.

    THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND
    ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
    LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
    AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN
    NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT,
    INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
    CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
    PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
    USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
    CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
    CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
    NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
    USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
    OF SUCH DAMAGE.

* The auxiliary file src/etc/pkg/modpath.iss contains a
  library routine compiled, by Inno Setup, into the Windows
  installer binary. This file is licensed under the LGPL,
  version 3, but, in our legal interpretation, this does not
  affect the aggregate "collected work" license of the Rust
  distribution (MIT/ASL2) nor any other components of it. We
  believe that the terms governing distribution of the
  binary Windows installer built from modpath.iss are
  therefore LGPL, but not the terms governing distribution
  of any of the files installed by such an installer (such
  as the Rust compiler or runtime libraries themselves).

* The src/rt/miniz.c file, carrying an implementation of
  RFC1950/RFC1951 DEFLATE, by Rich Geldreich
  <richgel99@gmail.com>. All uses of this file are
  permitted by the embedded "unlicense" notice
  (effectively: public domain with warranty disclaimer).

* LLVM. Code for this package is found in src/llvm.

    Copyright (c) 2003-2013 University of Illinois at
    Urbana-Champaign.  All rights reserved.

    Developed by:

        LLVM Team

        University of Illinois at Urbana-Champaign

        http://llvm.org

    Permission is hereby granted, free of charge, to any
    person obtaining a copy of this software and associated
    documentation files (the "Software"), to deal with the
    Software without restriction, including without
    limitation the rights to use, copy, modify, merge,
    publish, distribute, sublicense, and/or sell copies of
    the Software, and to permit persons to whom the Software
    is furnished to do so, subject to the following
    conditions:

        * Redistributions of source code must retain the
          above copyright notice, this list of conditions
          and the following disclaimers.

        * Redistributions in binary form must reproduce the
          above copyright notice, this list of conditions
          and the following disclaimers in the documentation
          and/or other materials provided with the
          distribution.

        * Neither the names of the LLVM Team, University of
          Illinois at Urbana-Champaign, nor the names of its
          contributors may be used to endorse or promote
          products derived from this Software without
          specific prior written permission.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF
    ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
    TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
    PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT
    SHALL THE CONTRIBUTORS OR COPYRIGHT HOLDERS BE LIABLE
    FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
    ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
    OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
    OTHER DEALINGS WITH THE SOFTWARE.

* Additional libraries included in LLVM carry separate
  BSD-compatible licenses. See src/llvm/LICENSE.txt for
  details.

* compiler-rt, in src/compiler-rt is dual licensed under
  LLVM's license and MIT:

    Copyright (c) 2009-2014 by the contributors listed in
    CREDITS.TXT

    All rights reserved.

    Developed by:

        LLVM Team

        University of Illinois at Urbana-Champaign

        http://llvm.org

    Permission is hereby granted, free of charge, to any
    person obtaining a copy of this software and associated
    documentation files (the "Software"), to deal with the
    Software without restriction, including without
    limitation the rights to use, copy, modify, merge,
    publish, distribute, sublicense, and/or sell copies of
    the Software, and to permit persons to whom the Software
    is furnished to do so, subject to the following
    conditions:

        * Redistributions of source code must retain the
          above copyright notice, this list of conditions
          and the following disclaimers.

        * Redistributions in binary form must reproduce the
          above copyright notice, this list of conditions
          and the following disclaimers in the documentation
          and/or other materials provided with the
          distribution.

        * Neither the names of the LLVM Team, University of
          Illinois at Urbana-Champaign, nor the names of its
          contributors may be used to endorse or promote
          products derived from this Software without
          specific prior written permission.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF
    ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
    TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
    PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT
    SHALL THE CONTRIBUTORS OR COPYRIGHT HOLDERS BE LIABLE
    FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
    ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
    OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
    OTHER DEALINGS WITH THE SOFTWARE.

    ========================================================

    Copyright (c) 2009-2014 by the contributors listed in
    CREDITS.TXT

    Permission is hereby granted, free of charge, to any
    person obtaining a copy of this software and associated
    documentation files (the "Software"), to deal in the
    Software without restriction, including without
    limitation the rights to use, copy, modify, merge,
    publish, distribute, sublicense, and/or sell copies of
    the Software, and to permit persons to whom the Software
    is furnished to do so, subject to the following
    conditions:

    The above copyright notice and this permission notice
    shall be included in all copies or substantial portions
    of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF
    ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
    TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
    PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT
    SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
    CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
    OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
    IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
    DEALINGS IN THE SOFTWARE.

* Portions of the FFI code for interacting with the native ABI
  is derived from the Clay programming language, which carries
  the following license.

    Copyright (C) 2008-2010 Tachyon Technologies.
    All rights reserved.

    Redistribution and use in source and binary forms, with
    or without modification, are permitted provided that the
    following conditions are met:

    1. Redistributions of source code must retain the above
       copyright notice, this list of conditions and the
       following disclaimer.

    2. Redistributions in binary form must reproduce the
       above copyright notice, this list of conditions and
       the following disclaimer in the documentation and/or
       other materials provided with the distribution.

    THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESS OR
    IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
    IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
    PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE
    DEVELOPERS AND CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
    INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
    CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
    PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
    USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
    CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
    CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
    NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
    USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
    OF SUCH DAMAGE.

* Hoedown, the markdown parser, under src/rt/hoedown, is
  licensed as follows.

    Copyright (c) 2008, Natacha Porté
    Copyright (c) 2011, Vicent Martí
    Copyright (c) 2013, Devin Torres and the Hoedown authors

    Permission to use, copy, modify, and distribute this
    software for any purpose with or without fee is hereby
    granted, provided that the above copyright notice and
    this permission notice appear in all copies.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR
    DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS SOFTWARE
    INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
    FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY
    SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR
    ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA
    OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE
    OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN
    CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

* libbacktrace, under src/libbacktrace:

    Copyright (C) 2012-2014 Free Software Foundation, Inc.
    Written by Ian Lance Taylor, Google.

    Redistribution and use in source and binary forms, with
    or without modification, are permitted provided that the
    following conditions are met:

        (1) Redistributions of source code must retain the
        above copyright notice, this list of conditions and
        the following disclaimer.

        (2) Redistributions in binary form must reproduce
        the above copyright notice, this list of conditions
        and the following disclaimer in the documentation
        and/or other materials provided with the
        distribution.

        (3) The name of the author may not be used to
        endorse or promote products derived from this
        software without specific prior written permission.

    THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND
    ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
    LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
    AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN
    NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT,
    INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
    CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
    PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
    USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
    CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
    CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
    NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
    USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
    OF SUCH DAMAGE.  */

* jemalloc, under src/jemalloc:

    Copyright (C) 2002-2014 Jason Evans
    <jasone@canonware.com>. All rights reserved.
    Copyright (C) 2007-2012 Mozilla Foundation.
    All rights reserved.
    Copyright (C) 2009-2014 Facebook, Inc.
    All rights reserved.

    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions are met:
    1. Redistributions of source code must retain the above copyright notice(s),
       this list of conditions and the following disclaimer.
    2. Redistributions in binary form must reproduce the above copyright notice(s),
       this list of conditions and the following disclaimer in the documentation
       and/or other materials provided with the distribution.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDER(S)
    ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES,
    INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
    MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
    DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT HOLDER(S)
    BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
    EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
    LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
    LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
    HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER
    IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
    NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
    USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
    OF SUCH DAMAGE.

* Additional copyright may be retained by contributors other
  than Mozilla, the Rust Project Developers, or the parties
  enumerated in this file. Such copyright can be determined
  on a case-by-case basis by examining the author of each
  portion of a file in the revision-control commit records
  of the project, or by consulting representative comments
  claiming copyright ownership for a file.

  For example, the text:

      "Copyright (c) 2011 Google Inc."

  appears in some files, and these files thereby denote
  that their author and copyright-holder is Google Inc.

  In all such cases, the absence of explicit licensing text
  indicates that the contributor chose to license their work
  for distribution under identical terms to those Mozilla
  has chosen for the collective work, enumerated at the top
  of this file. The only difference is the retention of
~~~~

### Text 24

MIT, as cfg_aliases 0.2.2 carries it.

~~~~
# 3rd Party Notices

The `cfg_aliases!` macro uses a lot of the code from [`tectonic_cfg_support::target_cfg!`] macro which is under the following license:

[`tectonic_cfg_support::target_cfg!`]: https://github.com/tectonic-typesetting/tectonic/blob/f2439b936470ad27bdf92882064bc4702ee01899/cfg_support/src/lib.rs#L166

    tectonic_cfg_support is licensed under the MIT License.

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the “Software”), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
---
~~~~

### Text 25

MIT-0, as constant_time_eq 0.4.2 carries it, and the text taken for encase_derive 0.12.1, encase_derive_impl 0.12.1, whose packages hold none.

~~~~
Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
~~~~

### Text 26

Apache-2.0, as core-graphics 0.23.2, euclid 0.22.14, unicode-segmentation 1.13.3, unicode-width 0.2.2, unicode-xid 0.2.6 carry it.

~~~~
Licensed under the Apache License, Version 2.0 <LICENSE-APACHE or
http://www.apache.org/licenses/LICENSE-2.0> or the MIT license
<LICENSE-MIT or http://opensource.org/licenses/MIT>, at your
option. All files in the project carrying such notice may not be
copied, modified, or distributed except according to those terms.
~~~~

### Text 27

Apache-2.0, as crc32fast 1.5.1, dpi 0.1.2, foreign-types 0.5.0, foreign-types-macros 0.2.4, foreign-types-shared 0.3.1, jni-sys 0.3.1, jni-sys 0.4.1, quick-error 2.0.1, toml_datetime 1.1.1+spec-1.1.0, toml_edit 0.25.13+spec-1.1.0, toml_parser 1.1.3+spec-1.1.0, winit 0.30.13 carry it.

~~~~
                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS

   APPENDIX: How to apply the Apache License to your work.

      To apply the Apache License to your work, attach the following
      boilerplate notice, with the fields enclosed by brackets "{}"
      replaced with your own identifying information. (Don't include
      the brackets!)  The text should be enclosed in the appropriate
      comment syntax for the file format. We also recommend that a
      file or class name and description of purpose be included on the
      same "printed page" as the copyright notice for easier
      identification within third-party archives.

   Copyright {yyyy} {name of copyright owner}

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
~~~~

### Text 28

Apache-2.0, as crossbeam-channel 0.5.16 carries it.

~~~~
===============================================================================

matching.go
https://creativecommons.org/licenses/by/3.0/legalcode

Creative Commons Legal Code

Attribution 3.0 Unported

    CREATIVE COMMONS CORPORATION IS NOT A LAW FIRM AND DOES NOT PROVIDE
    LEGAL SERVICES. DISTRIBUTION OF THIS LICENSE DOES NOT CREATE AN
    ATTORNEY-CLIENT RELATIONSHIP. CREATIVE COMMONS PROVIDES THIS
    INFORMATION ON AN "AS-IS" BASIS. CREATIVE COMMONS MAKES NO WARRANTIES
    REGARDING THE INFORMATION PROVIDED, AND DISCLAIMS LIABILITY FOR
    DAMAGES RESULTING FROM ITS USE.

License

THE WORK (AS DEFINED BELOW) IS PROVIDED UNDER THE TERMS OF THIS CREATIVE
COMMONS PUBLIC LICENSE ("CCPL" OR "LICENSE"). THE WORK IS PROTECTED BY
AUTHORIZED UNDER THIS LICENSE OR COPYRIGHT LAW IS PROHIBITED.

BY EXERCISING ANY RIGHTS TO THE WORK PROVIDED HERE, YOU ACCEPT AND AGREE
TO BE BOUND BY THE TERMS OF THIS LICENSE. TO THE EXTENT THIS LICENSE MAY
BE CONSIDERED TO BE A CONTRACT, THE LICENSOR GRANTS YOU THE RIGHTS
CONTAINED HERE IN CONSIDERATION OF YOUR ACCEPTANCE OF SUCH TERMS AND
CONDITIONS.

1. Definitions

 a. "Adaptation" means a work based upon the Work, or upon the Work and
    other pre-existing works, such as a translation, adaptation,
    derivative work, arrangement of music or other alterations of a
    literary or artistic work, or phonogram or performance and includes
    cinematographic adaptations or any other form in which the Work may be
    recast, transformed, or adapted including in any form recognizably
    derived from the original, except that a work that constitutes a
    Collection will not be considered an Adaptation for the purpose of
    this License. For the avoidance of doubt, where the Work is a musical
    work, performance or phonogram, the synchronization of the Work in
    timed-relation with a moving image ("synching") will be considered an
    Adaptation for the purpose of this License.
 b. "Collection" means a collection of literary or artistic works, such as
    encyclopedias and anthologies, or performances, phonograms or
    broadcasts, or other works or subject matter other than works listed
    in Section 1(f) below, which, by reason of the selection and
    arrangement of their contents, constitute intellectual creations, in
    which the Work is included in its entirety in unmodified form along
    with one or more other contributions, each constituting separate and
    independent works in themselves, which together are assembled into a
    collective whole. A work that constitutes a Collection will not be
    considered an Adaptation (as defined above) for the purposes of this
    License.
 c. "Distribute" means to make available to the public the original and
    copies of the Work or Adaptation, as appropriate, through sale or
    other transfer of ownership.
 d. "Licensor" means the individual, individuals, entity or entities that
    offer(s) the Work under the terms of this License.
 e. "Original Author" means, in the case of a literary or artistic work,
    the individual, individuals, entity or entities who created the Work
    or if no individual or entity can be identified, the publisher; and in
    addition (i) in the case of a performance the actors, singers,
    musicians, dancers, and other persons who act, sing, deliver, declaim,
    play in, interpret or otherwise perform literary or artistic works or
    expressions of folklore; (ii) in the case of a phonogram the producer
    being the person or legal entity who first fixes the sounds of a
    performance or other sounds; and, (iii) in the case of broadcasts, the
    organization that transmits the broadcast.
 f. "Work" means the literary and/or artistic work offered under the terms
    of this License including without limitation any production in the
    literary, scientific and artistic domain, whatever may be the mode or
    form of its expression including digital form, such as a book,
    pamphlet and other writing; a lecture, address, sermon or other work
    of the same nature; a dramatic or dramatico-musical work; a
    choreographic work or entertainment in dumb show; a musical
    composition with or without words; a cinematographic work to which are
    assimilated works expressed by a process analogous to cinematography;
    a work of drawing, painting, architecture, sculpture, engraving or
    lithography; a photographic work to which are assimilated works
    expressed by a process analogous to photography; a work of applied
    art; an illustration, map, plan, sketch or three-dimensional work
    relative to geography, topography, architecture or science; a
    performance; a broadcast; a phonogram; a compilation of data to the
    extent it is protected as a copyrightable work; or a work performed by
    a variety or circus performer to the extent it is not otherwise
    considered a literary or artistic work.
 g. "You" means an individual or entity exercising rights under this
    License who has not previously violated the terms of this License with
    respect to the Work, or who has received express permission from the
    Licensor to exercise rights under this License despite a previous
    violation.
 h. "Publicly Perform" means to perform public recitations of the Work and
    to communicate to the public those public recitations, by any means or
    process, including by wire or wireless means or public digital
    performances; to make available to the public Works in such a way that
    members of the public may access these Works from a place and at a
    place individually chosen by them; to perform the Work to the public
    by any means or process and the communication to the public of the
    performances of the Work, including by public digital performance; to
    broadcast and rebroadcast the Work by any means including signs,
    sounds or images.
 i. "Reproduce" means to make copies of the Work by any means including
    without limitation by sound or visual recordings and the right of
    fixation and reproducing fixations of the Work, including storage of a
    protected performance or phonogram in digital form or other electronic
    medium.

2. Fair Dealing Rights. Nothing in this License is intended to reduce,
limit, or restrict any uses free from copyright or rights arising from
limitations or exceptions that are provided for in connection with the

3. License Grant. Subject to the terms and conditions of this License,
Licensor hereby grants You a worldwide, royalty-free, non-exclusive,
perpetual (for the duration of the applicable copyright) license to
exercise the rights in the Work as stated below:

 a. to Reproduce the Work, to incorporate the Work into one or more
    Collections, and to Reproduce the Work as incorporated in the
    Collections;
 b. to create and Reproduce Adaptations provided that any such Adaptation,
    including any translation in any medium, takes reasonable steps to
    clearly label, demarcate or otherwise identify that changes were made
    to the original Work. For example, a translation could be marked "The
    original work was translated from English to Spanish," or a
    modification could indicate "The original work has been modified.";
 c. to Distribute and Publicly Perform the Work including as incorporated
    in Collections; and,
 d. to Distribute and Publicly Perform Adaptations.
 e. For the avoidance of doubt:

     i. Non-waivable Compulsory License Schemes. In those jurisdictions in
        which the right to collect royalties through any statutory or
        compulsory licensing scheme cannot be waived, the Licensor
        reserves the exclusive right to collect such royalties for any
        exercise by You of the rights granted under this License;
    ii. Waivable Compulsory License Schemes. In those jurisdictions in
        which the right to collect royalties through any statutory or
        compulsory licensing scheme can be waived, the Licensor waives the
        exclusive right to collect such royalties for any exercise by You
        of the rights granted under this License; and,
   iii. Voluntary License Schemes. The Licensor waives the right to
        collect royalties, whether individually or, in the event that the
        Licensor is a member of a collecting society that administers
        voluntary licensing schemes, via that society, from any exercise
        by You of the rights granted under this License.

The above rights may be exercised in all media and formats whether now
known or hereafter devised. The above rights include the right to make
such modifications as are technically necessary to exercise the rights in
other media and formats. Subject to Section 8(f), all rights not expressly
granted by Licensor are hereby reserved.

4. Restrictions. The license granted in Section 3 above is expressly made
subject to and limited by the following restrictions:

 a. You may Distribute or Publicly Perform the Work only under the terms
    of this License. You must include a copy of, or the Uniform Resource
    Identifier (URI) for, this License with every copy of the Work You
    Distribute or Publicly Perform. You may not offer or impose any terms
    on the Work that restrict the terms of this License or the ability of
    the recipient of the Work to exercise the rights granted to that
    recipient under the terms of the License. You may not sublicense the
    Work. You must keep intact all notices that refer to this License and
    to the disclaimer of warranties with every copy of the Work You
    Distribute or Publicly Perform. When You Distribute or Publicly
    Perform the Work, You may not impose any effective technological
    measures on the Work that restrict the ability of a recipient of the
    Work from You to exercise the rights granted to that recipient under
    the terms of the License. This Section 4(a) applies to the Work as
    incorporated in a Collection, but this does not require the Collection
    apart from the Work itself to be made subject to the terms of this
    License. If You create a Collection, upon notice from any Licensor You
    must, to the extent practicable, remove from the Collection any credit
    as required by Section 4(b), as requested. If You create an
    Adaptation, upon notice from any Licensor You must, to the extent
    practicable, remove from the Adaptation any credit as required by
    Section 4(b), as requested.
 b. If You Distribute, or Publicly Perform the Work or any Adaptations or
    Collections, You must, unless a request has been made pursuant to
    Section 4(a), keep intact all copyright notices for the Work and
    provide, reasonable to the medium or means You are utilizing: (i) the
    name of the Original Author (or pseudonym, if applicable) if supplied,
    and/or if the Original Author and/or Licensor designate another party
    or parties (e.g., a sponsor institute, publishing entity, journal) for
    attribution ("Attribution Parties") in Licensor's copyright notice,
    terms of service or by other reasonable means, the name of such party
    or parties; (ii) the title of the Work if supplied; (iii) to the
    extent reasonably practicable, the URI, if any, that Licensor
    specifies to be associated with the Work, unless such URI does not
    refer to the copyright notice or licensing information for the Work;
    and (iv) , consistent with Section 3(b), in the case of an Adaptation,
    a credit identifying the use of the Work in the Adaptation (e.g.,
    "French translation of the Work by Original Author," or "Screenplay
    based on original Work by Original Author"). The credit required by
    this Section 4 (b) may be implemented in any reasonable manner;
    provided, however, that in the case of a Adaptation or Collection, at
    a minimum such credit will appear, if a credit for all contributing
    authors of the Adaptation or Collection appears, then as part of these
    credits and in a manner at least as prominent as the credits for the
    other contributing authors. For the avoidance of doubt, You may only
    use the credit required by this Section for the purpose of attribution
    in the manner set out above and, by exercising Your rights under this
    License, You may not implicitly or explicitly assert or imply any
    connection with, sponsorship or endorsement by the Original Author,
    Licensor and/or Attribution Parties, as appropriate, of You or Your
    use of the Work, without the separate, express prior written
    permission of the Original Author, Licensor and/or Attribution
    Parties.
 c. Except as otherwise agreed in writing by the Licensor or as may be
    otherwise permitted by applicable law, if You Reproduce, Distribute or
    Publicly Perform the Work either by itself or as part of any
    Adaptations or Collections, You must not distort, mutilate, modify or
    take other derogatory action in relation to the Work which would be
    prejudicial to the Original Author's honor or reputation. Licensor
    agrees that in those jurisdictions (e.g. Japan), in which any exercise
    of the right granted in Section 3(b) of this License (the right to
    make Adaptations) would be deemed to be a distortion, mutilation,
    modification or other derogatory action prejudicial to the Original
    Author's honor and reputation, the Licensor will waive or not assert,
    as appropriate, this Section, to the fullest extent permitted by the
    applicable national law, to enable You to reasonably exercise Your
    right under Section 3(b) of this License (right to make Adaptations)
    but not otherwise.

5. Representations, Warranties and Disclaimer

UNLESS OTHERWISE MUTUALLY AGREED TO BY THE PARTIES IN WRITING, LICENSOR
OFFERS THE WORK AS-IS AND MAKES NO REPRESENTATIONS OR WARRANTIES OF ANY
KIND CONCERNING THE WORK, EXPRESS, IMPLIED, STATUTORY OR OTHERWISE,
INCLUDING, WITHOUT LIMITATION, WARRANTIES OF TITLE, MERCHANTIBILITY,
FITNESS FOR A PARTICULAR PURPOSE, NONINFRINGEMENT, OR THE ABSENCE OF
LATENT OR OTHER DEFECTS, ACCURACY, OR THE PRESENCE OF ABSENCE OF ERRORS,
WHETHER OR NOT DISCOVERABLE. SOME JURISDICTIONS DO NOT ALLOW THE EXCLUSION
OF IMPLIED WARRANTIES, SO SUCH EXCLUSION MAY NOT APPLY TO YOU.

6. Limitation on Liability. EXCEPT TO THE EXTENT REQUIRED BY APPLICABLE
LAW, IN NO EVENT WILL LICENSOR BE LIABLE TO YOU ON ANY LEGAL THEORY FOR
ANY SPECIAL, INCIDENTAL, CONSEQUENTIAL, PUNITIVE OR EXEMPLARY DAMAGES
ARISING OUT OF THIS LICENSE OR THE USE OF THE WORK, EVEN IF LICENSOR HAS
BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGES.

7. Termination

 a. This License and the rights granted hereunder will terminate
    automatically upon any breach by You of the terms of this License.
    Individuals or entities who have received Adaptations or Collections
    from You under this License, however, will not have their licenses
    terminated provided such individuals or entities remain in full
    compliance with those licenses. Sections 1, 2, 5, 6, 7, and 8 will
    survive any termination of this License.
 b. Subject to the above terms and conditions, the license granted here is
    perpetual (for the duration of the applicable copyright in the Work).
    Notwithstanding the above, Licensor reserves the right to release the
    Work under different license terms or to stop distributing the Work at
    any time; provided, however that any such election will not serve to
    withdraw this License (or any other license that has been, or is
    required to be, granted under the terms of this License), and this
    License will continue in full force and effect unless terminated as
    stated above.

8. Miscellaneous

 a. Each time You Distribute or Publicly Perform the Work or a Collection,
    the Licensor offers to the recipient a license to the Work on the same
    terms and conditions as the license granted to You under this License.
 b. Each time You Distribute or Publicly Perform an Adaptation, Licensor
    offers to the recipient a license to the original Work on the same
    terms and conditions as the license granted to You under this License.
 c. If any provision of this License is invalid or unenforceable under
    applicable law, it shall not affect the validity or enforceability of
    the remainder of the terms of this License, and without further action
    by the parties to this agreement, such provision shall be reformed to
    the minimum extent necessary to make such provision valid and
    enforceable.
 d. No term or provision of this License shall be deemed waived and no
    breach consented to unless such waiver or consent shall be in writing
    and signed by the party to be charged with such waiver or consent.
 e. This License constitutes the entire agreement between the parties with
    respect to the Work licensed here. There are no understandings,
    agreements or representations with respect to the Work not specified
    here. Licensor shall not be bound by any additional provisions that
    may appear in any communication from You. This License may not be
    modified without the mutual written agreement of the Licensor and You.
 f. The rights granted under, and the subject matter referenced, in this
    License were drafted utilizing the terminology of the Berne Convention
    for the Protection of Literary and Artistic Works (as amended on
    September 28, 1979), the Rome Convention of 1961, the WIPO Copyright
    Treaty of 1996, the WIPO Performances and Phonograms Treaty of 1996
    and the Universal Copyright Convention (as revised on July 24, 1971).
    These rights and subject matter take effect in the relevant
    jurisdiction in which the License terms are sought to be enforced
    according to the corresponding provisions of the implementation of
    those treaty provisions in the applicable national law. If the
    standard suite of rights granted under applicable copyright law
    includes additional rights not granted under this License, such
    additional rights are deemed to be included in the License; this
    License is not intended to restrict the license of any rights under
    applicable law.


Creative Commons Notice

    Creative Commons is not a party to this License, and makes no warranty
    whatsoever in connection with the Work. Creative Commons will not be
    liable to You or any party on any legal theory for any damages
    whatsoever, including without limitation any general, special,
    incidental or consequential damages arising in connection to this
    license. Notwithstanding the foregoing two (2) sentences, if Creative
    Commons has expressly identified itself as the Licensor hereunder, it
    shall have all rights and obligations of Licensor.

    Except for the limited purpose of indicating to the public that the
    Work is licensed under the CCPL, Creative Commons does not authorize
    the use by either party of the trademark "Creative Commons" or any
    related trademark or logo of Creative Commons without the prior
    written consent of Creative Commons. Any permitted use will be in
    compliance with Creative Commons' then-current trademark usage
    guidelines, as may be published on its website or otherwise made
    available upon request from time to time. For the avoidance of doubt,
    this trademark restriction does not form part of this License.

    Creative Commons may be contacted at https://creativecommons.org/.

===============================================================================

The Go Programming Language
https://golang.org/LICENSE


Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:

   * Redistributions of source code must retain the above copyright
notice, this list of conditions and the following disclaimer.
   * Redistributions in binary form must reproduce the above
copyright notice, this list of conditions and the following disclaimer
in the documentation and/or other materials provided with the
distribution.
   * Neither the name of Google Inc. nor the names of its
contributors may be used to endorse or promote products derived from
this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

===============================================================================

The Rust Programming Language
https://github.com/rust-lang/rust/blob/master/LICENSE-MIT

Permission is hereby granted, free of charge, to any
person obtaining a copy of this software and associated
documentation files (the "Software"), to deal in the
Software without restriction, including without
limitation the rights to use, copy, modify, merge,
publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software
is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice
shall be included in all copies or substantial portions
of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF
ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT
SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR
IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.

===============================================================================

The Rust Programming Language
https://github.com/rust-lang/rust/blob/master/LICENSE-APACHE

                              Apache License
                        Version 2.0, January 2004
                     http://www.apache.org/licenses/

TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

1. Definitions.

   "License" shall mean the terms and conditions for use, reproduction,
   and distribution as defined by Sections 1 through 9 of this document.

   "Licensor" shall mean the copyright owner or entity authorized by
   the copyright owner that is granting the License.

   "Legal Entity" shall mean the union of the acting entity and all
   other entities that control, are controlled by, or are under common
   control with that entity. For the purposes of this definition,
   "control" means (i) the power, direct or indirect, to cause the
   direction or management of such entity, whether by contract or
   otherwise, or (ii) ownership of fifty percent (50%) or more of the
   outstanding shares, or (iii) beneficial ownership of such entity.

   "You" (or "Your") shall mean an individual or Legal Entity
   exercising permissions granted by this License.

   "Source" form shall mean the preferred form for making modifications,
   including but not limited to software source code, documentation
   source, and configuration files.

   "Object" form shall mean any form resulting from mechanical
   transformation or translation of a Source form, including but
   not limited to compiled object code, generated documentation,
   and conversions to other media types.

   "Work" shall mean the work of authorship, whether in Source or
   Object form, made available under the License, as indicated by a
   copyright notice that is included in or attached to the work
   (an example is provided in the Appendix below).

   "Derivative Works" shall mean any work, whether in Source or Object
   form, that is based on (or derived from) the Work and for which the
   editorial revisions, annotations, elaborations, or other modifications
   represent, as a whole, an original work of authorship. For the purposes
   of this License, Derivative Works shall not include works that remain
   separable from, or merely link (or bind by name) to the interfaces of,
   the Work and Derivative Works thereof.

   "Contribution" shall mean any work of authorship, including
   the original version of the Work and any modifications or additions
   to that Work or Derivative Works thereof, that is intentionally
   submitted to Licensor for inclusion in the Work by the copyright owner
   or by an individual or Legal Entity authorized to submit on behalf of
   the copyright owner. For the purposes of this definition, "submitted"
   means any form of electronic, verbal, or written communication sent
   to the Licensor or its representatives, including but not limited to
   communication on electronic mailing lists, source code control systems,
   and issue tracking systems that are managed by, or on behalf of, the
   Licensor for the purpose of discussing and improving the Work, but
   excluding communication that is conspicuously marked or otherwise
   designated in writing by the copyright owner as "Not a Contribution."

   "Contributor" shall mean Licensor and any individual or Legal Entity
   on behalf of whom a Contribution has been received by Licensor and
   subsequently incorporated within the Work.

2. Grant of Copyright License. Subject to the terms and conditions of
   this License, each Contributor hereby grants to You a perpetual,
   worldwide, non-exclusive, no-charge, royalty-free, irrevocable
   copyright license to reproduce, prepare Derivative Works of,
   publicly display, publicly perform, sublicense, and distribute the
   Work and such Derivative Works in Source or Object form.

3. Grant of Patent License. Subject to the terms and conditions of
   this License, each Contributor hereby grants to You a perpetual,
   worldwide, non-exclusive, no-charge, royalty-free, irrevocable
   (except as stated in this section) patent license to make, have made,
   use, offer to sell, sell, import, and otherwise transfer the Work,
   where such license applies only to those patent claims licensable
   by such Contributor that are necessarily infringed by their
   Contribution(s) alone or by combination of their Contribution(s)
   with the Work to which such Contribution(s) was submitted. If You
   institute patent litigation against any entity (including a
   cross-claim or counterclaim in a lawsuit) alleging that the Work
   or a Contribution incorporated within the Work constitutes direct
   or contributory patent infringement, then any patent licenses
   granted to You under this License for that Work shall terminate
   as of the date such litigation is filed.

4. Redistribution. You may reproduce and distribute copies of the
   Work or Derivative Works thereof in any medium, with or without
   modifications, and in Source or Object form, provided that You
   meet the following conditions:

   (a) You must give any other recipients of the Work or
       Derivative Works a copy of this License; and

   (b) You must cause any modified files to carry prominent notices
       stating that You changed the files; and

   (c) You must retain, in the Source form of any Derivative Works
       that You distribute, all copyright, patent, trademark, and
       attribution notices from the Source form of the Work,
       excluding those notices that do not pertain to any part of
       the Derivative Works; and

   (d) If the Work includes a "NOTICE" text file as part of its
       distribution, then any Derivative Works that You distribute must
       include a readable copy of the attribution notices contained
       within such NOTICE file, excluding those notices that do not
       pertain to any part of the Derivative Works, in at least one
       of the following places: within a NOTICE text file distributed
       as part of the Derivative Works; within the Source form or
       documentation, if provided along with the Derivative Works; or,
       within a display generated by the Derivative Works, if and
       wherever such third-party notices normally appear. The contents
       of the NOTICE file are for informational purposes only and
       do not modify the License. You may add Your own attribution
       notices within Derivative Works that You distribute, alongside
       or as an addendum to the NOTICE text from the Work, provided
       that such additional attribution notices cannot be construed
       as modifying the License.

   You may add Your own copyright statement to Your modifications and
   may provide additional or different license terms and conditions
   for use, reproduction, or distribution of Your modifications, or
   for any such Derivative Works as a whole, provided Your use,
   reproduction, and distribution of the Work otherwise complies with
   the conditions stated in this License.

5. Submission of Contributions. Unless You explicitly state otherwise,
   any Contribution intentionally submitted for inclusion in the Work
   by You to the Licensor shall be under the terms and conditions of
   this License, without any additional terms or conditions.
   Notwithstanding the above, nothing herein shall supersede or modify
   the terms of any separate license agreement you may have executed
   with Licensor regarding such Contributions.

6. Trademarks. This License does not grant permission to use the trade
   names, trademarks, service marks, or product names of the Licensor,
   except as required for reasonable and customary use in describing the
   origin of the Work and reproducing the content of the NOTICE file.

7. Disclaimer of Warranty. Unless required by applicable law or
   agreed to in writing, Licensor provides the Work (and each
   Contributor provides its Contributions) on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
   implied, including, without limitation, any warranties or conditions
   of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
   PARTICULAR PURPOSE. You are solely responsible for determining the
   appropriateness of using or redistributing the Work and assume any
   risks associated with Your exercise of permissions under this License.

8. Limitation of Liability. In no event and under no legal theory,
   whether in tort (including negligence), contract, or otherwise,
   unless required by applicable law (such as deliberate and grossly
   negligent acts) or agreed to in writing, shall any Contributor be
   liable to You for damages, including any direct, indirect, special,
   incidental, or consequential damages of any character arising as a
   result of this License or out of the use or inability to use the
   Work (including but not limited to damages for loss of goodwill,
   work stoppage, computer failure or malfunction, or any and all
   other commercial damages or losses), even if such Contributor
   has been advised of the possibility of such damages.

9. Accepting Warranty or Additional Liability. While redistributing
   the Work or Derivative Works thereof, You may choose to offer,
   and charge a fee for, acceptance of support, warranty, indemnity,
   or other liability obligations and/or rights consistent with this
   License. However, in accepting such obligations, You may act only
   on Your own behalf and on Your sole responsibility, not on behalf
   of any other Contributor, and only if You agree to indemnify,
   defend, and hold each Contributor harmless for any liability
   incurred by, or claims asserted against, such Contributor by reason
   of your accepting any such warranty or additional liability.

END OF TERMS AND CONDITIONS

APPENDIX: How to apply the Apache License to your work.

   To apply the Apache License to your work, attach the following
   boilerplate notice, with the fields enclosed by brackets "[]"
   replaced with your own identifying information. (Don't include
   the brackets!)  The text should be enclosed in the appropriate
   comment syntax for the file format. We also recommend that a
   file or class name and description of purpose be included on the
   same "printed page" as the copyright notice for easier
   identification within third-party archives.

Copyright [yyyy] [name of copyright owner]

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

	http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
~~~~

### Text 29

MIT, as dpi 0.1.2 carries it.

~~~~
rust-lang/libm as a whole is available for use under the MIT license:

------------------------------------------------------------------------------
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
------------------------------------------------------------------------------

This Rust library contains the following copyrights:

    Copyright (c) 2018 Jorge Aparicio

Portions of this software are derived from third-party works licensed under
terms compatible with the above MIT license:

* musl libc https://www.musl-libc.org/. This library contains the following

      Copyright © 2005-2020 Rich Felker, et al.

* The CORE-MATH project https://core-math.gitlabpages.inria.fr/. CORE-MATH
  routines are available under the MIT license on a per-file basis.

The musl libc COPYRIGHT file also includes the following notice relevant to
math portions of the library:

------------------------------------------------------------------------------
Much of the math library code (src/math/* and src/complex/*) is
and labelled as such in comments in the individual source files. All
have been licensed under extremely permissive terms.
------------------------------------------------------------------------------
~~~~

### Text 30

MIT-0, as encase 0.12.1 carries it.

~~~~
MIT No Attribution

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
~~~~

### Text 31

Apache-2.0, as encoding_rs 0.8.35 carries it.

~~~~
encoding_rs is copyright Mozilla Foundation.

Licensed under the Apache License, Version 2.0
<LICENSE-APACHE or
https://www.apache.org/licenses/LICENSE-2.0> or the MIT
license <LICENSE-MIT or https://opensource.org/licenses/MIT>,
at your option. All files in the project carrying such
notice may not be copied, modified, or distributed except
according to those terms.

This crate includes data derived from the data files supplied
with the WHATWG Encoding Standard, which, when incorporated into
source code, are licensed under the BSD 3-Clause License
<LICENSE-WHATWG>.

Test code within encoding_rs is dedicated to the Public Domain when so
designated (see the individual files for PD/CC0-dedicated sections).
~~~~

### Text 32

BSD-3-Clause, as encoding_rs 0.8.35 carries it.

~~~~
Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
~~~~

### Text 33

BSL-1.0, as error-code 3.4.0 carries it, and the text taken for clipboard-win 5.4.1, whose packages hold none.

~~~~
Boost Software License - Version 1.0 - August 17th, 2003

Permission is hereby granted, free of charge, to any person or organization
obtaining a copy of the software and accompanying documentation covered by
this license (the "Software") to use, reproduce, display, distribute,
execute, and transmit the Software, and to prepare derivative works of the
Software, and to permit third-parties to whom the Software is furnished to
do so, all subject to the following:

The copyright notices in the Software and this entire statement, including
the above license grant, this restriction and the following disclaimer,
must be included in all copies of the Software, in whole or in part, and
all derivative works of the Software, unless such copies or derivative
works are solely in the form of machine-executable object code generated by
a source language processor.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE, TITLE AND NON-INFRINGEMENT. IN NO EVENT
SHALL THE COPYRIGHT HOLDERS OR ANYONE DISTRIBUTING THE SOFTWARE BE LIABLE
FOR ANY DAMAGES OR OTHER LIABILITY, WHETHER IN CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.
~~~~

### Text 34

Apache-2.0, as getrandom 0.3.4, getrandom 0.4.3 carry it.

~~~~
                              Apache License
                        Version 2.0, January 2004
                     https://www.apache.org/licenses/

TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

1. Definitions.

   "License" shall mean the terms and conditions for use, reproduction,
   and distribution as defined by Sections 1 through 9 of this document.

   "Licensor" shall mean the copyright owner or entity authorized by
   the copyright owner that is granting the License.

   "Legal Entity" shall mean the union of the acting entity and all
   other entities that control, are controlled by, or are under common
   control with that entity. For the purposes of this definition,
   "control" means (i) the power, direct or indirect, to cause the
   direction or management of such entity, whether by contract or
   otherwise, or (ii) ownership of fifty percent (50%) or more of the
   outstanding shares, or (iii) beneficial ownership of such entity.

   "You" (or "Your") shall mean an individual or Legal Entity
   exercising permissions granted by this License.

   "Source" form shall mean the preferred form for making modifications,
   including but not limited to software source code, documentation
   source, and configuration files.

   "Object" form shall mean any form resulting from mechanical
   transformation or translation of a Source form, including but
   not limited to compiled object code, generated documentation,
   and conversions to other media types.

   "Work" shall mean the work of authorship, whether in Source or
   Object form, made available under the License, as indicated by a
   copyright notice that is included in or attached to the work
   (an example is provided in the Appendix below).

   "Derivative Works" shall mean any work, whether in Source or Object
   form, that is based on (or derived from) the Work and for which the
   editorial revisions, annotations, elaborations, or other modifications
   represent, as a whole, an original work of authorship. For the purposes
   of this License, Derivative Works shall not include works that remain
   separable from, or merely link (or bind by name) to the interfaces of,
   the Work and Derivative Works thereof.

   "Contribution" shall mean any work of authorship, including
   the original version of the Work and any modifications or additions
   to that Work or Derivative Works thereof, that is intentionally
   submitted to Licensor for inclusion in the Work by the copyright owner
   or by an individual or Legal Entity authorized to submit on behalf of
   the copyright owner. For the purposes of this definition, "submitted"
   means any form of electronic, verbal, or written communication sent
   to the Licensor or its representatives, including but not limited to
   communication on electronic mailing lists, source code control systems,
   and issue tracking systems that are managed by, or on behalf of, the
   Licensor for the purpose of discussing and improving the Work, but
   excluding communication that is conspicuously marked or otherwise
   designated in writing by the copyright owner as "Not a Contribution."

   "Contributor" shall mean Licensor and any individual or Legal Entity
   on behalf of whom a Contribution has been received by Licensor and
   subsequently incorporated within the Work.

2. Grant of Copyright License. Subject to the terms and conditions of
   this License, each Contributor hereby grants to You a perpetual,
   worldwide, non-exclusive, no-charge, royalty-free, irrevocable
   copyright license to reproduce, prepare Derivative Works of,
   publicly display, publicly perform, sublicense, and distribute the
   Work and such Derivative Works in Source or Object form.

3. Grant of Patent License. Subject to the terms and conditions of
   this License, each Contributor hereby grants to You a perpetual,
   worldwide, non-exclusive, no-charge, royalty-free, irrevocable
   (except as stated in this section) patent license to make, have made,
   use, offer to sell, sell, import, and otherwise transfer the Work,
   where such license applies only to those patent claims licensable
   by such Contributor that are necessarily infringed by their
   Contribution(s) alone or by combination of their Contribution(s)
   with the Work to which such Contribution(s) was submitted. If You
   institute patent litigation against any entity (including a
   cross-claim or counterclaim in a lawsuit) alleging that the Work
   or a Contribution incorporated within the Work constitutes direct
   or contributory patent infringement, then any patent licenses
   granted to You under this License for that Work shall terminate
   as of the date such litigation is filed.

4. Redistribution. You may reproduce and distribute copies of the
   Work or Derivative Works thereof in any medium, with or without
   modifications, and in Source or Object form, provided that You
   meet the following conditions:

   (a) You must give any other recipients of the Work or
       Derivative Works a copy of this License; and

   (b) You must cause any modified files to carry prominent notices
       stating that You changed the files; and

   (c) You must retain, in the Source form of any Derivative Works
       that You distribute, all copyright, patent, trademark, and
       attribution notices from the Source form of the Work,
       excluding those notices that do not pertain to any part of
       the Derivative Works; and

   (d) If the Work includes a "NOTICE" text file as part of its
       distribution, then any Derivative Works that You distribute must
       include a readable copy of the attribution notices contained
       within such NOTICE file, excluding those notices that do not
       pertain to any part of the Derivative Works, in at least one
       of the following places: within a NOTICE text file distributed
       as part of the Derivative Works; within the Source form or
       documentation, if provided along with the Derivative Works; or,
       within a display generated by the Derivative Works, if and
       wherever such third-party notices normally appear. The contents
       of the NOTICE file are for informational purposes only and
       do not modify the License. You may add Your own attribution
       notices within Derivative Works that You distribute, alongside
       or as an addendum to the NOTICE text from the Work, provided
       that such additional attribution notices cannot be construed
       as modifying the License.

   You may add Your own copyright statement to Your modifications and
   may provide additional or different license terms and conditions
   for use, reproduction, or distribution of Your modifications, or
   for any such Derivative Works as a whole, provided Your use,
   reproduction, and distribution of the Work otherwise complies with
   the conditions stated in this License.

5. Submission of Contributions. Unless You explicitly state otherwise,
   any Contribution intentionally submitted for inclusion in the Work
   by You to the Licensor shall be under the terms and conditions of
   this License, without any additional terms or conditions.
   Notwithstanding the above, nothing herein shall supersede or modify
   the terms of any separate license agreement you may have executed
   with Licensor regarding such Contributions.

6. Trademarks. This License does not grant permission to use the trade
   names, trademarks, service marks, or product names of the Licensor,
   except as required for reasonable and customary use in describing the
   origin of the Work and reproducing the content of the NOTICE file.

7. Disclaimer of Warranty. Unless required by applicable law or
   agreed to in writing, Licensor provides the Work (and each
   Contributor provides its Contributions) on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
   implied, including, without limitation, any warranties or conditions
   of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
   PARTICULAR PURPOSE. You are solely responsible for determining the
   appropriateness of using or redistributing the Work and assume any
   risks associated with Your exercise of permissions under this License.

8. Limitation of Liability. In no event and under no legal theory,
   whether in tort (including negligence), contract, or otherwise,
   unless required by applicable law (such as deliberate and grossly
   negligent acts) or agreed to in writing, shall any Contributor be
   liable to You for damages, including any direct, indirect, special,
   incidental, or consequential damages of any character arising as a
   result of this License or out of the use or inability to use the
   Work (including but not limited to damages for loss of goodwill,
   work stoppage, computer failure or malfunction, or any and all
   other commercial damages or losses), even if such Contributor
   has been advised of the possibility of such damages.

9. Accepting Warranty or Additional Liability. While redistributing
   the Work or Derivative Works thereof, You may choose to offer,
   and charge a fee for, acceptance of support, warranty, indemnity,
   or other liability obligations and/or rights consistent with this
   License. However, in accepting such obligations, You may act only
   on Your own behalf and on Your sole responsibility, not on behalf
   of any other Contributor, and only if You agree to indemnify,
   defend, and hold each Contributor harmless for any liability
   incurred by, or claims asserted against, such Contributor by reason
   of your accepting any such warranty or additional liability.

END OF TERMS AND CONDITIONS

APPENDIX: How to apply the Apache License to your work.

   To apply the Apache License to your work, attach the following
   boilerplate notice, with the fields enclosed by brackets "[]"
   replaced with your own identifying information. (Don't include
   the brackets!)  The text should be enclosed in the appropriate
   comment syntax for the file format. We also recommend that a
   file or class name and description of purpose be included on the
   same "printed page" as the copyright notice for easier
   identification within third-party archives.

Copyright [yyyy] [name of copyright owner]

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

	https://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
~~~~

### Text 35

MIT, as gltf-derive 1.4.1 carries it.

~~~~
`gltf_derive` was adapted from `validator_derive` (https://github.com/Keats/validator)
which has the following license:

The MIT License (MIT)


Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
~~~~

### Text 36

Unicode-3.0, as icu_collections 2.3.0, icu_locale_core 2.3.0, icu_locale_fallback 2.3.0, icu_locale_fallback_data 2.3.0, icu_normalizer 2.3.0, icu_normalizer_data 2.3.0, icu_properties 2.3.0, icu_properties_data 2.3.0, icu_provider 2.3.1, icu_segmenter 2.3.0, icu_segmenter_data 2.3.0, litemap 0.8.3, potential_utf 0.1.6, tinystr 0.8.4, writeable 0.6.4, yoke 0.8.3, yoke-derive 0.8.2, zerofrom 0.1.8, zerofrom-derive 0.1.7, zerotrie 0.2.5, zerovec 0.11.8, zerovec-derive 0.11.6 carry it.

~~~~
UNICODE LICENSE V3

COPYRIGHT AND PERMISSION NOTICE


NOTICE TO USER: Carefully read the following legal agreement. BY
DOWNLOADING, INSTALLING, COPYING OR OTHERWISE USING DATA FILES, AND/OR
SOFTWARE, YOU UNEQUIVOCALLY ACCEPT, AND AGREE TO BE BOUND BY, ALL OF THE
TERMS AND CONDITIONS OF THIS AGREEMENT. IF YOU DO NOT AGREE, DO NOT
DOWNLOAD, INSTALL, COPY, DISTRIBUTE OR USE THE DATA FILES OR SOFTWARE.

Permission is hereby granted, free of charge, to any person obtaining a
copy of data files and any associated documentation (the "Data Files") or
software and any associated documentation (the "Software") to deal in the
Data Files or Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, and/or sell
copies of the Data Files or Software, and to permit persons to whom the
Data Files or Software are furnished to do so, provided that either (a)
this copyright and permission notice appear with all copies of the Data
Files or Software, or (b) this copyright and permission notice appear in
associated Documentation.

THE DATA FILES AND SOFTWARE ARE PROVIDED "AS IS", WITHOUT WARRANTY OF ANY
KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT OF
THIRD PARTY RIGHTS.

IN NO EVENT SHALL THE COPYRIGHT HOLDER OR HOLDERS INCLUDED IN THIS NOTICE
BE LIABLE FOR ANY CLAIM, OR ANY SPECIAL INDIRECT OR CONSEQUENTIAL DAMAGES,
OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS,
WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION,
ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THE DATA
FILES OR SOFTWARE.

Except as contained in this notice, the name of a copyright holder shall
not be used in advertising or otherwise to promote the sale, use or other
dealings in these Data Files or Software without prior written
authorization of the copyright holder.

SPDX-License-Identifier: Unicode-3.0

—

Portions of ICU4X may have been adapted from ICU4C and/or ICU4J.
ICU 1.8.1 to ICU 57.1 © 1995-2016 International Business Machines Corporation and others.
~~~~

### Text 37

ISC, as inotify 0.11.5, inotify-sys 0.1.8, libloading 0.8.9 carry it.

~~~~
Permission to use, copy, modify, and/or distribute this software for any purpose
with or without fee is hereby granted, provided that the above copyright notice
and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
THIS SOFTWARE.
~~~~

### Text 38

Apache-2.0, as lewton 0.10.2 carries it.

~~~~
Licensed under MIT or Apache License 2.0,
at your option.

The full list of contributors can be obtained by looking
at the VCS log (originally, this crate was git versioned,
there you can do "git shortlog -sn" for this task).

MIT License
-----------

The MIT License (MIT)


Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.



Apache License, version 2.0
---------------------------
                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS
~~~~

### Text 39

Apache-2.0, as libm 0.2.16 carries it.

~~~~
rust-lang/libm as a whole is available for use under the MIT license:

------------------------------------------------------------------------------
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
------------------------------------------------------------------------------

As a contributor, you agree that your code can be used under either the MIT
license or the Apache-2.0 license:

------------------------------------------------------------------------------
                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS

   APPENDIX: How to apply the Apache License to your work.

      To apply the Apache License to your work, attach the following
      boilerplate notice, with the fields enclosed by brackets "[]"
      replaced with your own identifying information. (Don't include
      the brackets!)  The text should be enclosed in the appropriate
      comment syntax for the file format. We also recommend that a
      file or class name and description of purpose be included on the
      same "printed page" as the copyright notice for easier
      identification within third-party archives.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
------------------------------------------------------------------------------

This Rust library contains the following copyrights:

    Copyright (c) 2018 Jorge Aparicio

Portions of this software are derived from third-party works licensed under
terms compatible with the above MIT license:

* musl libc https://www.musl-libc.org/. This library contains the following

      Copyright © 2005-2020 Rich Felker, et al.

* The CORE-MATH project https://core-math.gitlabpages.inria.fr/. CORE-MATH
  routines are available under the MIT license on a per-file basis.

The musl libc COPYRIGHT file also includes the following notice relevant to
math portions of the library:

------------------------------------------------------------------------------
Much of the math library code (src/math/* and src/complex/*) is
and labelled as such in comments in the individual source files. All
have been licensed under extremely permissive terms.
------------------------------------------------------------------------------

Copyright notices are retained in src/* files where relevant.
~~~~

### Text 40

Apache-2.0, as linux-raw-sys 0.4.15, linux-raw-sys 0.12.1 carry it.

~~~~
Short version for non-lawyers:

`linux-raw-sys` is triple-licensed under Apache 2.0 with the LLVM Exception,
Apache 2.0, and MIT terms.


Longer version:

Copyrights in the `linux-raw-sys` project are retained by their contributors.
No copyright assignment is required to contribute to the `linux-raw-sys`
project.

Some files include code derived from Rust's `libstd`; see the comments in
the code for details.

Except as otherwise noted (below and/or in individual files), `linux-raw-sys`
is licensed under:

 - the Apache License, Version 2.0, with the LLVM Exception
   <LICENSE-Apache-2.0_WITH_LLVM-exception> or
   <http://llvm.org/foundation/relicensing/LICENSE.txt>
 - the Apache License, Version 2.0
   <LICENSE-APACHE> or
   <http://www.apache.org/licenses/LICENSE-2.0>,
 - or the MIT license
   <LICENSE-MIT> or
   <http://opensource.org/licenses/MIT>,

at your option.
~~~~

### Text 41

Apache-2.0, as linux-raw-sys 0.4.15, linux-raw-sys 0.12.1, rustix 0.38.44, rustix 1.1.4, wasi 0.11.1+wasi-snapshot-preview1, wasip2 1.0.4+wasi-0.2.12, wit-bindgen 0.57.1 carry it.

~~~~
                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS

   APPENDIX: How to apply the Apache License to your work.

      To apply the Apache License to your work, attach the following
      boilerplate notice, with the fields enclosed by brackets "[]"
      replaced with your own identifying information. (Don't include
      the brackets!)  The text should be enclosed in the appropriate
      comment syntax for the file format. We also recommend that a
      file or class name and description of purpose be included on the
      same "printed page" as the copyright notice for easier
      identification within third-party archives.

   Copyright [yyyy] [name of copyright owner]

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.


--- LLVM Exceptions to the Apache 2.0 License ----

As an exception, if, as a result of your compiling your source code, portions
of this Software are embedded into an Object form of such source code, you
may redistribute such embedded portions in such Object form without complying
with the conditions of Sections 4(a), 4(b) and 4(d) of the License.

In addition, if you combine or link compiled forms of this Software with
software that is licensed under the GPLv2 ("Combined Software") and if a
court of competent jurisdiction determines that the patent provision (Section
3), the indemnity provision (Section 9) or other Section of the License
conflicts with the conditions of the GPLv2, you may retroactively and
prospectively choose to deem waived or otherwise exclude such Section(s) of
the License, but only in their entirety and only with respect to the Combined
Software.
~~~~

### Text 42

Apache-2.0, as metis-sys 0.3.2 carries it.

~~~~
Copyright & License Notice
---------------------------


Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied. See the License for the specific language governing
permissions and limitations under the License.
~~~~

### Text 43

Apache-2.0, as nonmax 0.5.5 carries it.

~~~~
i                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS

   APPENDIX: How to apply the Apache License to your work.

      To apply the Apache License to your work, attach the following
      boilerplate notice, with the fields enclosed by brackets "{}"
      replaced with your own identifying information. (Don't include
      the brackets!)  The text should be enclosed in the appropriate
      comment syntax for the file format. We also recommend that a
      file or class name and description of purpose be included on the
      same "printed page" as the copyright notice for easier
      identification within third-party archives.

   Copyright {yyyy} {name of copyright owner}

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
~~~~

### Text 44

CC0-1.0, as notify 8.2.0 carries it.

~~~~
Creative Commons CC0 1.0 Universal

<<beginOptional;name=ccOptionalIntro>> CREATIVE COMMONS CORPORATION IS NOT A LAW FIRM AND DOES NOT PROVIDE LEGAL SERVICES. DISTRIBUTION OF THIS DOCUMENT DOES NOT CREATE AN ATTORNEY-CLIENT RELATIONSHIP. CREATIVE COMMONS PROVIDES THIS INFORMATION ON AN "AS-IS" BASIS. CREATIVE COMMONS MAKES NO WARRANTIES REGARDING THE USE OF THIS DOCUMENT OR THE INFORMATION OR WORKS PROVIDED HEREUNDER, AND DISCLAIMS LIABILITY FOR DAMAGES RESULTING FROM THE USE OF THIS DOCUMENT OR THE INFORMATION OR WORKS PROVIDED HEREUNDER.  <<endOptional>>

Statement of Purpose

The laws of most jurisdictions throughout the world automatically confer exclusive Copyright and Related Rights (defined below) upon the creator and subsequent owner(s) (each and all, an "owner") of an original work of authorship and/or a database (each, a "Work").

Certain owners wish to permanently relinquish those rights to a Work for the purpose of contributing to a commons of creative, cultural and scientific works ("Commons") that the public can reliably and without fear of later claims of infringement build upon, modify, incorporate in other works, reuse and redistribute as freely as possible in any form whatsoever and for any purposes, including without limitation commercial purposes. These owners may contribute to the Commons to promote the ideal of a free culture and the further production of creative, cultural and scientific works, or to gain reputation or greater distribution for their Work in part through the use and efforts of others.

For these and/or other purposes and motivations, and without any expectation of additional consideration or compensation, the person associating CC0 with a Work (the "Affirmer"), to the extent that he or she is an owner of Copyright and Related Rights in the Work, voluntarily elects to apply CC0 to the Work and publicly distribute the Work under its terms, with knowledge of his or her Copyright and Related Rights in the Work and the meaning and intended legal effect of CC0 on those rights.

1. Copyright and Related Rights. A Work made available under CC0 may be protected by copyright and related or neighboring rights ("Copyright and Related Rights"). Copyright and Related Rights include, but are not limited to, the following:

     i. the right to reproduce, adapt, distribute, perform, display, communicate, and translate a Work;

     ii. moral rights retained by the original author(s) and/or performer(s);

     iii. publicity and privacy rights pertaining to a person's image or likeness depicted in a Work;

     iv. rights protecting against unfair competition in regards to a Work, subject to the limitations in paragraph 4(a), below;

     v. rights protecting the extraction, dissemination, use and reuse of data in a Work;

     vi. database rights (such as those arising under Directive 96/9/EC of the European Parliament and of the Council of 11 March 1996 on the legal protection of databases, and under any national implementation thereof, including any amended or successor version of such directive); and

     vii. other similar, equivalent or corresponding rights throughout the world based on applicable law or treaty, and any national implementations thereof.

2. Waiver. To the greatest extent permitted by, but not in contravention of, applicable law, Affirmer hereby overtly, fully, permanently, irrevocably and unconditionally waives, abandons, and surrenders all of Affirmer's Copyright and Related Rights and associated claims and causes of action, whether now known or unknown (including existing as well as future claims and causes of action), in the Work (i) in all territories worldwide, (ii) for the maximum duration provided by applicable law or treaty (including future time extensions), (iii) in any current or future medium and for any number of copies, and (iv) for any purpose whatsoever, including without limitation commercial, advertising or promotional purposes (the "Waiver"). Affirmer makes the Waiver for the benefit of each member of the public at large and to the detriment of Affirmer's heirs and successors, fully intending that such Waiver shall not be subject to revocation, rescission, cancellation, termination, or any other legal or equitable action to disrupt the quiet enjoyment of the Work by the public as contemplated by Affirmer's express Statement of Purpose.

3. Public License Fallback. Should any part of the Waiver for any reason be judged legally invalid or ineffective under applicable law, then the Waiver shall be preserved to the maximum extent permitted taking into account Affirmer's express Statement of Purpose. In addition, to the extent the Waiver is so judged Affirmer hereby grants to each affected person a royalty-free, non transferable, non sublicensable, non exclusive, irrevocable and unconditional license to exercise Affirmer's Copyright and Related Rights in the Work (i) in all territories worldwide, (ii) for the maximum duration provided by applicable law or treaty (including future time extensions), (iii) in any current or future medium and for any number of copies, and (iv) for any purpose whatsoever, including without limitation commercial, advertising or promotional purposes (the "License"). The License shall be deemed effective as of the date CC0 was applied by Affirmer to the Work. Should any part of the License for any reason be judged legally invalid or ineffective under applicable law, such partial invalidity or ineffectiveness shall not invalidate the remainder of the License, and in such case Affirmer hereby affirms that he or she will not (i) exercise any of his or her remaining Copyright and Related Rights in the Work or (ii) assert any associated claims and causes of action with respect to the Work, in either case contrary to Affirmer's express Statement of Purpose.

4. Limitations and Disclaimers.

     a. No trademark or patent rights held by Affirmer are waived, abandoned, surrendered, licensed or otherwise affected by this document.

     b. Affirmer offers the Work as-is and makes no representations or warranties of any kind concerning the Work, express, implied, statutory or otherwise, including without limitation warranties of title, merchantability, fitness for a particular purpose, non infringement, or the absence of latent or other defects, accuracy, or the present or absence of errors, whether or not discoverable, all to the greatest extent permissible under applicable law.

     c. Affirmer disclaims responsibility for clearing rights of other persons that may apply to the Work or any use thereof, including without limitation any person's Copyright and Related Rights in the Work. Further, Affirmer disclaims responsibility for obtaining any necessary consents, permissions or other rights required for any use of the Work.

     d. Affirmer understands and acknowledges that Creative Commons is not a party to this document and has no duty or obligation with respect to this CC0 or use of the Work.
~~~~

### Text 45

BSD-3-Clause, as num_enum 0.7.6, num_enum_derive 0.7.6 carry it.

~~~~
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this
  list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.

* Neither the name of num_enum nor the names of its
  contributors may be used to endorse or promote products derived from
  this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
~~~~

### Text 46

BSD-3-Clause, as ogg 0.8.0 carries it.

~~~~
NOTE:
The full list of contributors can be obtained by looking
at the VCS log (originally, this crate was git versioned,
there you can do "git shortlog -sn" for this task).

License text
------------

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions
are met:

- Redistributions of source code must retain the above copyright
notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above copyright
notice, this list of conditions and the following disclaimer in the
documentation and/or other materials provided with the distribution.

- Neither the name of the Xiph.org Foundation nor the names of its
contributors may be used to endorse or promote products derived from
this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL THE FOUNDATION
OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
~~~~

### Text 47

LGPL-2.1, as orbclient 0.3.55 carries it.

~~~~
LICENSE
-------
The source code for everything except the compiled fonts in this current
release is licensed as follows:

     License for this current distribution of program source
     files (i.e., everything except the fonts) is released under
     the terms of the GNU General Public License version 2,
     or (at your option) a later version.

     See the section below for a copy of the GNU General Public License
     version 2.

The license for the compiled fonts is covered by the above GPL terms
with the GNU font embedding exception, as follows:

     As a special exception, if you create a document which uses this font,
     and embed this font or unaltered portions of this font into the document,
     this font does not by itself cause the resulting document to be covered
     by the GNU General Public License. This exception does not however
     invalidate any other reasons why the document might be covered by the
     GNU General Public License. If you modify this font, you may extend
     this exception to your version of the font, but you are not obligated
     to do so. If you do not wish to do so, delete this exception statement
     from your version.

See "http://www.gnu.org/licenses/gpl-faq.html#FontException" for more details.


GPL VERSION 2
-------------

                    GNU GENERAL PUBLIC LICENSE
                       Version 2, June 1991

 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
 Everyone is permitted to copy and distribute verbatim copies
 of this license document, but changing it is not allowed.

                            Preamble

  The licenses for most software are designed to take away your
freedom to share and change it.  By contrast, the GNU General Public
License is intended to guarantee your freedom to share and change free
software--to make sure the software is free for all its users.  This
General Public License applies to most of the Free Software
Foundation's software and to any other program whose authors commit to
using it.  (Some other Free Software Foundation software is covered by
the GNU Lesser General Public License instead.)  You can apply it to
your programs, too.

  When we speak of free software, we are referring to freedom, not
price.  Our General Public Licenses are designed to make sure that you
have the freedom to distribute copies of free software (and charge for
this service if you wish), that you receive source code or can get it
if you want it, that you can change the software or use pieces of it
in new free programs; and that you know you can do these things.

  To protect your rights, we need to make restrictions that forbid
anyone to deny you these rights or to ask you to surrender the rights.
These restrictions translate to certain responsibilities for you if you
distribute copies of the software, or if you modify it.

  For example, if you distribute copies of such a program, whether
gratis or for a fee, you must give the recipients all the rights that
you have.  You must make sure that they, too, receive or can get the
source code.  And you must show them these terms so they know their
rights.

  We protect your rights with two steps: (1) copyright the software, and
(2) offer you this license which gives you legal permission to copy,
distribute and/or modify the software.

  Also, for each author's protection and ours, we want to make certain
that everyone understands that there is no warranty for this free
software.  If the software is modified by someone else and passed on, we
want its recipients to know that what they have is not the original, so
that any problems introduced by others will not reflect on the original
authors' reputations.

  Finally, any free program is threatened constantly by software
patents.  We wish to avoid the danger that redistributors of a free
program will individually obtain patent licenses, in effect making the
program proprietary.  To prevent this, we have made it clear that any
patent must be licensed for everyone's free use or not licensed at all.

  The precise terms and conditions for copying, distribution and
modification follow.

                    GNU GENERAL PUBLIC LICENSE
   TERMS AND CONDITIONS FOR COPYING, DISTRIBUTION AND MODIFICATION

  0. This License applies to any program or other work which contains
a notice placed by the copyright holder saying it may be distributed
under the terms of this General Public License.  The "Program", below,
refers to any such program or work, and a "work based on the Program"
means either the Program or any derivative work under copyright law:
that is to say, a work containing the Program or a portion of it,
either verbatim or with modifications and/or translated into another
language.  (Hereinafter, translation is included without limitation in
the term "modification".)  Each licensee is addressed as "you".

Activities other than copying, distribution and modification are not
covered by this License; they are outside its scope.  The act of
running the Program is not restricted, and the output from the Program
is covered only if its contents constitute a work based on the
Program (independent of having been made by running the Program).
Whether that is true depends on what the Program does.

  1. You may copy and distribute verbatim copies of the Program's
source code as you receive it, in any medium, provided that you
conspicuously and appropriately publish on each copy an appropriate
copyright notice and disclaimer of warranty; keep intact all the
notices that refer to this License and to the absence of any warranty;
and give any other recipients of the Program a copy of this License
along with the Program.

You may charge a fee for the physical act of transferring a copy, and
you may at your option offer warranty protection in exchange for a fee.

  2. You may modify your copy or copies of the Program or any portion
of it, thus forming a work based on the Program, and copy and
distribute such modifications or work under the terms of Section 1
above, provided that you also meet all of these conditions:

    a) You must cause the modified files to carry prominent notices
    stating that you changed the files and the date of any change.

    b) You must cause any work that you distribute or publish, that in
    whole or in part contains or is derived from the Program or any
    part thereof, to be licensed as a whole at no charge to all third
    parties under the terms of this License.

    c) If the modified program normally reads commands interactively
    when run, you must cause it, when started running for such
    interactive use in the most ordinary way, to print or display an
    announcement including an appropriate copyright notice and a
    notice that there is no warranty (or else, saying that you provide
    a warranty) and that users may redistribute the program under
    these conditions, and telling the user how to view a copy of this
    License.  (Exception: if the Program itself is interactive but
    does not normally print such an announcement, your work based on
    the Program is not required to print an announcement.)

These requirements apply to the modified work as a whole.  If
identifiable sections of that work are not derived from the Program,
and can be reasonably considered independent and separate works in
themselves, then this License, and its terms, do not apply to those
sections when you distribute them as separate works.  But when you
distribute the same sections as part of a whole which is a work based
on the Program, the distribution of the whole must be on the terms of
this License, whose permissions for other licensees extend to the
entire whole, and thus to each and every part regardless of who wrote it.

Thus, it is not the intent of this section to claim rights or contest
your rights to work written entirely by you; rather, the intent is to
exercise the right to control the distribution of derivative or
collective works based on the Program.

In addition, mere aggregation of another work not based on the Program
with the Program (or with a work based on the Program) on a volume of
a storage or distribution medium does not bring the other work under
the scope of this License.

  3. You may copy and distribute the Program (or a work based on it,
under Section 2) in object code or executable form under the terms of
Sections 1 and 2 above provided that you also do one of the following:

    a) Accompany it with the complete corresponding machine-readable
    source code, which must be distributed under the terms of Sections
    1 and 2 above on a medium customarily used for software interchange; or,

    b) Accompany it with a written offer, valid for at least three
    years, to give any third party, for a charge no more than your
    cost of physically performing source distribution, a complete
    machine-readable copy of the corresponding source code, to be
    distributed under the terms of Sections 1 and 2 above on a medium
    customarily used for software interchange; or,

    c) Accompany it with the information you received as to the offer
    to distribute corresponding source code.  (This alternative is
    allowed only for noncommercial distribution and only if you
    received the program in object code or executable form with such
    an offer, in accord with Subsection b above.)

The source code for a work means the preferred form of the work for
making modifications to it.  For an executable work, complete source
code means all the source code for all modules it contains, plus any
associated interface definition files, plus the scripts used to
control compilation and installation of the executable.  However, as a
special exception, the source code distributed need not include
anything that is normally distributed (in either source or binary
form) with the major components (compiler, kernel, and so on) of the
operating system on which the executable runs, unless that component
itself accompanies the executable.

If distribution of executable or object code is made by offering
access to copy from a designated place, then offering equivalent
access to copy the source code from the same place counts as
distribution of the source code, even though third parties are not
compelled to copy the source along with the object code.

  4. You may not copy, modify, sublicense, or distribute the Program
except as expressly provided under this License.  Any attempt
otherwise to copy, modify, sublicense or distribute the Program is
void, and will automatically terminate your rights under this License.
However, parties who have received copies, or rights, from you under
this License will not have their licenses terminated so long as such
parties remain in full compliance.

  5. You are not required to accept this License, since you have not
signed it.  However, nothing else grants you permission to modify or
distribute the Program or its derivative works.  These actions are
prohibited by law if you do not accept this License.  Therefore, by
modifying or distributing the Program (or any work based on the
Program), you indicate your acceptance of this License to do so, and
all its terms and conditions for copying, distributing or modifying
the Program or works based on it.

  6. Each time you redistribute the Program (or any work based on the
Program), the recipient automatically receives a license from the
original licensor to copy, distribute or modify the Program subject to
these terms and conditions.  You may not impose any further
restrictions on the recipients' exercise of the rights granted herein.
You are not responsible for enforcing compliance by third parties to
this License.

  7. If, as a consequence of a court judgment or allegation of patent
infringement or for any other reason (not limited to patent issues),
conditions are imposed on you (whether by court order, agreement or
otherwise) that contradict the conditions of this License, they do not
excuse you from the conditions of this License.  If you cannot
distribute so as to satisfy simultaneously your obligations under this
License and any other pertinent obligations, then as a consequence you
may not distribute the Program at all.  For example, if a patent
license would not permit royalty-free redistribution of the Program by
all those who receive copies directly or indirectly through you, then
the only way you could satisfy both it and this License would be to
refrain entirely from distribution of the Program.

If any portion of this section is held invalid or unenforceable under
any particular circumstance, the balance of the section is intended to
apply and the section as a whole is intended to apply in other
circumstances.

It is not the purpose of this section to induce you to infringe any
patents or other property right claims or to contest validity of any
such claims; this section has the sole purpose of protecting the
integrity of the free software distribution system, which is
implemented by public license practices.  Many people have made
generous contributions to the wide range of software distributed
through that system in reliance on consistent application of that
system; it is up to the author/donor to decide if he or she is willing
to distribute software through any other system and a licensee cannot
impose that choice.

This section is intended to make thoroughly clear what is believed to
be a consequence of the rest of this License.

  8. If the distribution and/or use of the Program is restricted in
certain countries either by patents or by copyrighted interfaces, the
original copyright holder who places the Program under this License
may add an explicit geographical distribution limitation excluding
those countries, so that distribution is permitted only in or among
countries not thus excluded.  In such case, this License incorporates
the limitation as if written in the body of this License.

  9. The Free Software Foundation may publish revised and/or new versions
of the General Public License from time to time.  Such new versions will
be similar in spirit to the present version, but may differ in detail to
address new problems or concerns.

Each version is given a distinguishing version number.  If the Program
specifies a version number of this License which applies to it and "any
later version", you have the option of following the terms and conditions
either of that version or of any later version published by the Free
Software Foundation.  If the Program does not specify a version number of
this License, you may choose any version ever published by the Free Software
Foundation.

  10. If you wish to incorporate parts of the Program into other free
programs whose distribution conditions are different, write to the author
to ask for permission.  For software which is copyrighted by the Free
Software Foundation, write to the Free Software Foundation; we sometimes
make exceptions for this.  Our decision will be guided by the two goals
of preserving the free status of all derivatives of our free software and
of promoting the sharing and reuse of software generally.

                            NO WARRANTY

  11. BECAUSE THE PROGRAM IS LICENSED FREE OF CHARGE, THERE IS NO WARRANTY
FOR THE PROGRAM, TO THE EXTENT PERMITTED BY APPLICABLE LAW.  EXCEPT WHEN
OTHERWISE STATED IN WRITING THE COPYRIGHT HOLDERS AND/OR OTHER PARTIES
PROVIDE THE PROGRAM "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED
OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE.  THE ENTIRE RISK AS
TO THE QUALITY AND PERFORMANCE OF THE PROGRAM IS WITH YOU.  SHOULD THE
PROGRAM PROVE DEFECTIVE, YOU ASSUME THE COST OF ALL NECESSARY SERVICING,
REPAIR OR CORRECTION.

  12. IN NO EVENT UNLESS REQUIRED BY APPLICABLE LAW OR AGREED TO IN WRITING
WILL ANY COPYRIGHT HOLDER, OR ANY OTHER PARTY WHO MAY MODIFY AND/OR
REDISTRIBUTE THE PROGRAM AS PERMITTED ABOVE, BE LIABLE TO YOU FOR DAMAGES,
INCLUDING ANY GENERAL, SPECIAL, INCIDENTAL OR CONSEQUENTIAL DAMAGES ARISING
OUT OF THE USE OR INABILITY TO USE THE PROGRAM (INCLUDING BUT NOT LIMITED
TO LOSS OF DATA OR DATA BEING RENDERED INACCURATE OR LOSSES SUSTAINED BY
YOU OR THIRD PARTIES OR A FAILURE OF THE PROGRAM TO OPERATE WITH ANY OTHER
PROGRAMS), EVEN IF SUCH HOLDER OR OTHER PARTY HAS BEEN ADVISED OF THE
POSSIBILITY OF SUCH DAMAGES.

                     END OF TERMS AND CONDITIONS

            How to Apply These Terms to Your New Programs

  If you develop a new program, and you want it to be of the greatest
possible use to the public, the best way to achieve this is to make it
free software which everyone can redistribute and change under these terms.

  To do so, attach the following notices to the program.  It is safest
to attach them to the start of each source file to most effectively
convey the exclusion of warranty; and each file should have at least
the "copyright" line and a pointer to where the full notice is found.

    <one line to give the program's name and a brief idea of what it does.>
    Copyright (C) <year>  <name of author>

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

Also add information on how to contact you by electronic and paper mail.

If the program is interactive, make it output a short notice like this
when it starts in an interactive mode:

    Gnomovision version 69, Copyright (C) year name of author
    Gnomovision comes with ABSOLUTELY NO WARRANTY; for details type `show w'.
    This is free software, and you are welcome to redistribute it
    under certain conditions; type `show c' for details.

The hypothetical commands `show w' and `show c' should show the appropriate
parts of the General Public License.  Of course, the commands you use may
be called something other than `show w' and `show c'; they could even be
mouse-clicks or menu items--whatever suits your program.

You should also get your employer (if you work as a programmer) or your
school, if any, to sign a "copyright disclaimer" for the program, if
necessary.  Here is a sample; alter the names:

  Yoyodyne, Inc., hereby disclaims all copyright interest in the program
  `Gnomovision' (which makes passes at compilers) written by James Hacker.

  <signature of Ty Coon>, 1 April 1989
  Ty Coon, President of Vice

This General Public License does not permit incorporating your program into
proprietary programs.  If your program is a subroutine library, you may
consider it more useful to permit linking proprietary applications with the
library.  If this is what you want to do, use the GNU Lesser General
Public License instead of this License.
~~~~

### Text 48

Apache-2.0, as parking 2.2.1 carries it.

~~~~
===============================================================================


Licensed under the Apache License, Version 2.0 <LICENSE-APACHE or
http://www.apache.org/licenses/LICENSE-2.0> or the MIT license
<LICENSE-MIT or http://opensource.org/licenses/MIT>, at your
option. All files in the project carrying such notice may not be
copied, modified, or distributed except according to those terms.
~~~~

### Text 49

A notice, as petgraph 0.8.3 carries it.

~~~~
Graphosaurus (c) by the petgraph project.

Graphosaurus is licensed under a
Creative Commons Attribution-ShareAlike 4.0 International License.

You should have received a copy of the license along with this
work.  If not, see <http://creativecommons.org/licenses/by-sa/4.0/>.
~~~~

### Text 50

BSD-3-Clause, as pp-rs 0.2.1 carries it.

~~~~
BSD 3-Clause License

All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its
   contributors may be used to endorse or promote products derived from
   this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
~~~~

### Text 51

Apache-2.0, as rand 0.10.2, rand_distr 0.6.0 carry it.

~~~~
Copyrights in the Rand project are retained by their contributors. No

For full authorship information, see the version control history.

Except as otherwise noted (below and/or in individual files), Rand is
licensed under the Apache License, Version 2.0 <LICENSE-APACHE> or
<http://www.apache.org/licenses/LICENSE-2.0> or the MIT license
<LICENSE-MIT> or <http://opensource.org/licenses/MIT>, at your option.

The Rand project includes code from the Rust project
published under these same licenses.
~~~~

### Text 52

Apache-2.0, as rand 0.10.2 carries it.

~~~~
                              Apache License
                        Version 2.0, January 2004
                     https://www.apache.org/licenses/

TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

1. Definitions.

   "License" shall mean the terms and conditions for use, reproduction,
   and distribution as defined by Sections 1 through 9 of this document.

   "Licensor" shall mean the copyright owner or entity authorized by
   the copyright owner that is granting the License.

   "Legal Entity" shall mean the union of the acting entity and all
   other entities that control, are controlled by, or are under common
   control with that entity. For the purposes of this definition,
   "control" means (i) the power, direct or indirect, to cause the
   direction or management of such entity, whether by contract or
   otherwise, or (ii) ownership of fifty percent (50%) or more of the
   outstanding shares, or (iii) beneficial ownership of such entity.

   "You" (or "Your") shall mean an individual or Legal Entity
   exercising permissions granted by this License.

   "Source" form shall mean the preferred form for making modifications,
   including but not limited to software source code, documentation
   source, and configuration files.

   "Object" form shall mean any form resulting from mechanical
   transformation or translation of a Source form, including but
   not limited to compiled object code, generated documentation,
   and conversions to other media types.

   "Work" shall mean the work of authorship, whether in Source or
   Object form, made available under the License, as indicated by a
   copyright notice that is included in or attached to the work
   (an example is provided in the Appendix below).

   "Derivative Works" shall mean any work, whether in Source or Object
   form, that is based on (or derived from) the Work and for which the
   editorial revisions, annotations, elaborations, or other modifications
   represent, as a whole, an original work of authorship. For the purposes
   of this License, Derivative Works shall not include works that remain
   separable from, or merely link (or bind by name) to the interfaces of,
   the Work and Derivative Works thereof.

   "Contribution" shall mean any work of authorship, including
   the original version of the Work and any modifications or additions
   to that Work or Derivative Works thereof, that is intentionally
   submitted to Licensor for inclusion in the Work by the copyright owner
   or by an individual or Legal Entity authorized to submit on behalf of
   the copyright owner. For the purposes of this definition, "submitted"
   means any form of electronic, verbal, or written communication sent
   to the Licensor or its representatives, including but not limited to
   communication on electronic mailing lists, source code control systems,
   and issue tracking systems that are managed by, or on behalf of, the
   Licensor for the purpose of discussing and improving the Work, but
   excluding communication that is conspicuously marked or otherwise
   designated in writing by the copyright owner as "Not a Contribution."

   "Contributor" shall mean Licensor and any individual or Legal Entity
   on behalf of whom a Contribution has been received by Licensor and
   subsequently incorporated within the Work.

2. Grant of Copyright License. Subject to the terms and conditions of
   this License, each Contributor hereby grants to You a perpetual,
   worldwide, non-exclusive, no-charge, royalty-free, irrevocable
   copyright license to reproduce, prepare Derivative Works of,
   publicly display, publicly perform, sublicense, and distribute the
   Work and such Derivative Works in Source or Object form.

3. Grant of Patent License. Subject to the terms and conditions of
   this License, each Contributor hereby grants to You a perpetual,
   worldwide, non-exclusive, no-charge, royalty-free, irrevocable
   (except as stated in this section) patent license to make, have made,
   use, offer to sell, sell, import, and otherwise transfer the Work,
   where such license applies only to those patent claims licensable
   by such Contributor that are necessarily infringed by their
   Contribution(s) alone or by combination of their Contribution(s)
   with the Work to which such Contribution(s) was submitted. If You
   institute patent litigation against any entity (including a
   cross-claim or counterclaim in a lawsuit) alleging that the Work
   or a Contribution incorporated within the Work constitutes direct
   or contributory patent infringement, then any patent licenses
   granted to You under this License for that Work shall terminate
   as of the date such litigation is filed.

4. Redistribution. You may reproduce and distribute copies of the
   Work or Derivative Works thereof in any medium, with or without
   modifications, and in Source or Object form, provided that You
   meet the following conditions:

   (a) You must give any other recipients of the Work or
       Derivative Works a copy of this License; and

   (b) You must cause any modified files to carry prominent notices
       stating that You changed the files; and

   (c) You must retain, in the Source form of any Derivative Works
       that You distribute, all copyright, patent, trademark, and
       attribution notices from the Source form of the Work,
       excluding those notices that do not pertain to any part of
       the Derivative Works; and

   (d) If the Work includes a "NOTICE" text file as part of its
       distribution, then any Derivative Works that You distribute must
       include a readable copy of the attribution notices contained
       within such NOTICE file, excluding those notices that do not
       pertain to any part of the Derivative Works, in at least one
       of the following places: within a NOTICE text file distributed
       as part of the Derivative Works; within the Source form or
       documentation, if provided along with the Derivative Works; or,
       within a display generated by the Derivative Works, if and
       wherever such third-party notices normally appear. The contents
       of the NOTICE file are for informational purposes only and
       do not modify the License. You may add Your own attribution
       notices within Derivative Works that You distribute, alongside
       or as an addendum to the NOTICE text from the Work, provided
       that such additional attribution notices cannot be construed
       as modifying the License.

   You may add Your own copyright statement to Your modifications and
   may provide additional or different license terms and conditions
   for use, reproduction, or distribution of Your modifications, or
   for any such Derivative Works as a whole, provided Your use,
   reproduction, and distribution of the Work otherwise complies with
   the conditions stated in this License.

5. Submission of Contributions. Unless You explicitly state otherwise,
   any Contribution intentionally submitted for inclusion in the Work
   by You to the Licensor shall be under the terms and conditions of
   this License, without any additional terms or conditions.
   Notwithstanding the above, nothing herein shall supersede or modify
   the terms of any separate license agreement you may have executed
   with Licensor regarding such Contributions.

6. Trademarks. This License does not grant permission to use the trade
   names, trademarks, service marks, or product names of the Licensor,
   except as required for reasonable and customary use in describing the
   origin of the Work and reproducing the content of the NOTICE file.

7. Disclaimer of Warranty. Unless required by applicable law or
   agreed to in writing, Licensor provides the Work (and each
   Contributor provides its Contributions) on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
   implied, including, without limitation, any warranties or conditions
   of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
   PARTICULAR PURPOSE. You are solely responsible for determining the
   appropriateness of using or redistributing the Work and assume any
   risks associated with Your exercise of permissions under this License.

8. Limitation of Liability. In no event and under no legal theory,
   whether in tort (including negligence), contract, or otherwise,
   unless required by applicable law (such as deliberate and grossly
   negligent acts) or agreed to in writing, shall any Contributor be
   liable to You for damages, including any direct, indirect, special,
   incidental, or consequential damages of any character arising as a
   result of this License or out of the use or inability to use the
   Work (including but not limited to damages for loss of goodwill,
   work stoppage, computer failure or malfunction, or any and all
   other commercial damages or losses), even if such Contributor
   has been advised of the possibility of such damages.

9. Accepting Warranty or Additional Liability. While redistributing
   the Work or Derivative Works thereof, You may choose to offer,
   and charge a fee for, acceptance of support, warranty, indemnity,
   or other liability obligations and/or rights consistent with this
   License. However, in accepting such obligations, You may act only
   on Your own behalf and on Your sole responsibility, not on behalf
   of any other Contributor, and only if You agree to indemnify,
   defend, and hold each Contributor harmless for any liability
   incurred by, or claims asserted against, such Contributor by reason
   of your accepting any such warranty or additional liability.

END OF TERMS AND CONDITIONS
~~~~

### Text 53

Apache-2.0, as rand_core 0.10.1 carries it.

~~~~
Copyrights in the Rand project are retained by their contributors. No

For full authorship information, see the version control history.

Except as otherwise noted (below and/or in individual files), Rand is
licensed under the Apache License, Version 2.0 <LICENSE-APACHE> or
<http://www.apache.org/licenses/LICENSE-2.0> or the MIT license
<LICENSE-MIT> or <http://opensource.org/licenses/MIT>, at your option.
~~~~

### Text 54

Apache-2.0, as rand_core 0.10.1, rand_distr 0.6.0 carry it.

~~~~
                              Apache License
                        Version 2.0, January 2004
                     https://www.apache.org/licenses/

TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

1. Definitions.

   "License" shall mean the terms and conditions for use, reproduction,
   and distribution as defined by Sections 1 through 9 of this document.

   "Licensor" shall mean the copyright owner or entity authorized by
   the copyright owner that is granting the License.

   "Legal Entity" shall mean the union of the acting entity and all
   other entities that control, are controlled by, or are under common
   control with that entity. For the purposes of this definition,
   "control" means (i) the power, direct or indirect, to cause the
   direction or management of such entity, whether by contract or
   otherwise, or (ii) ownership of fifty percent (50%) or more of the
   outstanding shares, or (iii) beneficial ownership of such entity.

   "You" (or "Your") shall mean an individual or Legal Entity
   exercising permissions granted by this License.

   "Source" form shall mean the preferred form for making modifications,
   including but not limited to software source code, documentation
   source, and configuration files.

   "Object" form shall mean any form resulting from mechanical
   transformation or translation of a Source form, including but
   not limited to compiled object code, generated documentation,
   and conversions to other media types.

   "Work" shall mean the work of authorship, whether in Source or
   Object form, made available under the License, as indicated by a
   copyright notice that is included in or attached to the work
   (an example is provided in the Appendix below).

   "Derivative Works" shall mean any work, whether in Source or Object
   form, that is based on (or derived from) the Work and for which the
   editorial revisions, annotations, elaborations, or other modifications
   represent, as a whole, an original work of authorship. For the purposes
   of this License, Derivative Works shall not include works that remain
   separable from, or merely link (or bind by name) to the interfaces of,
   the Work and Derivative Works thereof.

   "Contribution" shall mean any work of authorship, including
   the original version of the Work and any modifications or additions
   to that Work or Derivative Works thereof, that is intentionally
   submitted to Licensor for inclusion in the Work by the copyright owner
   or by an individual or Legal Entity authorized to submit on behalf of
   the copyright owner. For the purposes of this definition, "submitted"
   means any form of electronic, verbal, or written communication sent
   to the Licensor or its representatives, including but not limited to
   communication on electronic mailing lists, source code control systems,
   and issue tracking systems that are managed by, or on behalf of, the
   Licensor for the purpose of discussing and improving the Work, but
   excluding communication that is conspicuously marked or otherwise
   designated in writing by the copyright owner as "Not a Contribution."

   "Contributor" shall mean Licensor and any individual or Legal Entity
   on behalf of whom a Contribution has been received by Licensor and
   subsequently incorporated within the Work.

2. Grant of Copyright License. Subject to the terms and conditions of
   this License, each Contributor hereby grants to You a perpetual,
   worldwide, non-exclusive, no-charge, royalty-free, irrevocable
   copyright license to reproduce, prepare Derivative Works of,
   publicly display, publicly perform, sublicense, and distribute the
   Work and such Derivative Works in Source or Object form.

3. Grant of Patent License. Subject to the terms and conditions of
   this License, each Contributor hereby grants to You a perpetual,
   worldwide, non-exclusive, no-charge, royalty-free, irrevocable
   (except as stated in this section) patent license to make, have made,
   use, offer to sell, sell, import, and otherwise transfer the Work,
   where such license applies only to those patent claims licensable
   by such Contributor that are necessarily infringed by their
   Contribution(s) alone or by combination of their Contribution(s)
   with the Work to which such Contribution(s) was submitted. If You
   institute patent litigation against any entity (including a
   cross-claim or counterclaim in a lawsuit) alleging that the Work
   or a Contribution incorporated within the Work constitutes direct
   or contributory patent infringement, then any patent licenses
   granted to You under this License for that Work shall terminate
   as of the date such litigation is filed.

4. Redistribution. You may reproduce and distribute copies of the
   Work or Derivative Works thereof in any medium, with or without
   modifications, and in Source or Object form, provided that You
   meet the following conditions:

   (a) You must give any other recipients of the Work or
       Derivative Works a copy of this License; and

   (b) You must cause any modified files to carry prominent notices
       stating that You changed the files; and

   (c) You must retain, in the Source form of any Derivative Works
       that You distribute, all copyright, patent, trademark, and
       attribution notices from the Source form of the Work,
       excluding those notices that do not pertain to any part of
       the Derivative Works; and

   (d) If the Work includes a "NOTICE" text file as part of its
       distribution, then any Derivative Works that You distribute must
       include a readable copy of the attribution notices contained
       within such NOTICE file, excluding those notices that do not
       pertain to any part of the Derivative Works, in at least one
       of the following places: within a NOTICE text file distributed
       as part of the Derivative Works; within the Source form or
       documentation, if provided along with the Derivative Works; or,
       within a display generated by the Derivative Works, if and
       wherever such third-party notices normally appear. The contents
       of the NOTICE file are for informational purposes only and
       do not modify the License. You may add Your own attribution
       notices within Derivative Works that You distribute, alongside
       or as an addendum to the NOTICE text from the Work, provided
       that such additional attribution notices cannot be construed
       as modifying the License.

   You may add Your own copyright statement to Your modifications and
   may provide additional or different license terms and conditions
   for use, reproduction, or distribution of Your modifications, or
   for any such Derivative Works as a whole, provided Your use,
   reproduction, and distribution of the Work otherwise complies with
   the conditions stated in this License.

5. Submission of Contributions. Unless You explicitly state otherwise,
   any Contribution intentionally submitted for inclusion in the Work
   by You to the Licensor shall be under the terms and conditions of
   this License, without any additional terms or conditions.
   Notwithstanding the above, nothing herein shall supersede or modify
   the terms of any separate license agreement you may have executed
   with Licensor regarding such Contributions.

6. Trademarks. This License does not grant permission to use the trade
   names, trademarks, service marks, or product names of the Licensor,
   except as required for reasonable and customary use in describing the
   origin of the Work and reproducing the content of the NOTICE file.

7. Disclaimer of Warranty. Unless required by applicable law or
   agreed to in writing, Licensor provides the Work (and each
   Contributor provides its Contributions) on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
   implied, including, without limitation, any warranties or conditions
   of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
   PARTICULAR PURPOSE. You are solely responsible for determining the
   appropriateness of using or redistributing the Work and assume any
   risks associated with Your exercise of permissions under this License.

8. Limitation of Liability. In no event and under no legal theory,
   whether in tort (including negligence), contract, or otherwise,
   unless required by applicable law (such as deliberate and grossly
   negligent acts) or agreed to in writing, shall any Contributor be
   liable to You for damages, including any direct, indirect, special,
   incidental, or consequential damages of any character arising as a
   result of this License or out of the use or inability to use the
   Work (including but not limited to damages for loss of goodwill,
   work stoppage, computer failure or malfunction, or any and all
   other commercial damages or losses), even if such Contributor
   has been advised of the possibility of such damages.

9. Accepting Warranty or Additional Liability. While redistributing
   the Work or Derivative Works thereof, You may choose to offer,
   and charge a fee for, acceptance of support, warranty, indemnity,
   or other liability obligations and/or rights consistent with this
   License. However, in accepting such obligations, You may act only
   on Your own behalf and on Your sole responsibility, not on behalf
   of any other Contributor, and only if You agree to indemnify,
   defend, and hold each Contributor harmless for any liability
   incurred by, or claims asserted against, such Contributor by reason
   of your accepting any such warranty or additional liability.

END OF TERMS AND CONDITIONS

APPENDIX: How to apply the Apache License to your work.

   To apply the Apache License to your work, attach the following
   boilerplate notice, with the fields enclosed by brackets "[]"
   replaced with your own identifying information. (Don't include
   the brackets!)  The text should be enclosed in the appropriate
   comment syntax for the file format. We also recommend that a
   file or class name and description of purpose be included on the
   same "printed page" as the copyright notice for easier
   identification within third-party archives.
~~~~

### Text 55

Unicode-DFS-2016, as regex-syntax 0.8.11 carries it.

~~~~
UNICODE, INC. LICENSE AGREEMENT - DATA FILES AND SOFTWARE

Unicode Data Files include all data files under the directories
http://www.unicode.org/Public/, http://www.unicode.org/reports/,
http://www.unicode.org/cldr/data/, http://source.icu-project.org/repos/icu/, and
http://www.unicode.org/utility/trac/browser/.

Unicode Data Files do not include PDF online code charts under the
directory http://www.unicode.org/Public/.

Software includes any source code published in the Unicode Standard
or under the directories
http://www.unicode.org/Public/, http://www.unicode.org/reports/,
http://www.unicode.org/cldr/data/, http://source.icu-project.org/repos/icu/, and
http://www.unicode.org/utility/trac/browser/.

NOTICE TO USER: Carefully read the following legal agreement.
BY DOWNLOADING, INSTALLING, COPYING OR OTHERWISE USING UNICODE INC.'S
DATA FILES ("DATA FILES"), AND/OR SOFTWARE ("SOFTWARE"),
YOU UNEQUIVOCALLY ACCEPT, AND AGREE TO BE BOUND BY, ALL OF THE
TERMS AND CONDITIONS OF THIS AGREEMENT.
IF YOU DO NOT AGREE, DO NOT DOWNLOAD, INSTALL, COPY, DISTRIBUTE OR USE
THE DATA FILES OR SOFTWARE.

COPYRIGHT AND PERMISSION NOTICE

Distributed under the Terms of Use in http://www.unicode.org/copyright.html.

Permission is hereby granted, free of charge, to any person obtaining
a copy of the Unicode data files and any associated documentation
(the "Data Files") or Unicode software and any associated documentation
(the "Software") to deal in the Data Files or Software
without restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, and/or sell copies of
the Data Files or Software, and to permit persons to whom the Data Files
or Software are furnished to do so, provided that either
(a) this copyright and permission notice appear with all copies
of the Data Files or Software, or
(b) this copyright and permission notice appear in associated
Documentation.

THE DATA FILES AND SOFTWARE ARE PROVIDED "AS IS", WITHOUT WARRANTY OF
ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT OF THIRD PARTY RIGHTS.
IN NO EVENT SHALL THE COPYRIGHT HOLDER OR HOLDERS INCLUDED IN THIS
NOTICE BE LIABLE FOR ANY CLAIM, OR ANY SPECIAL INDIRECT OR CONSEQUENTIAL
DAMAGES, OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE,
DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
PERFORMANCE OF THE DATA FILES OR SOFTWARE.

Except as contained in this notice, the name of a copyright holder
shall not be used in advertising or otherwise to promote the sale,
use or other dealings in these Data Files or Software without prior
written authorization of the copyright holder.
~~~~

### Text 56

Apache-2.0, as rustix 0.38.44, rustix 1.1.4 carry it.

~~~~
Short version for non-lawyers:

`rustix` is triple-licensed under Apache 2.0 with the LLVM Exception,
Apache 2.0, and MIT terms.


Longer version:

Copyrights in the `rustix` project are retained by their contributors.
No copyright assignment is required to contribute to the `rustix`
project.

Some files include code derived from Rust's `libstd`; see the comments in
the code for details.

Except as otherwise noted (below and/or in individual files), `rustix`
is licensed under:

 - the Apache License, Version 2.0, with the LLVM Exception
   <LICENSE-Apache-2.0_WITH_LLVM-exception> or
   <http://llvm.org/foundation/relicensing/LICENSE.txt>
 - the Apache License, Version 2.0
   <LICENSE-APACHE> or
   <http://www.apache.org/licenses/LICENSE-2.0>,
 - or the MIT license
   <LICENSE-MIT> or
   <http://opensource.org/licenses/MIT>,

at your option.
~~~~

### Text 57

MIT, as stackfuture 0.3.1 carries it.

~~~~
StackFuture

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
~~~~

### Text 58

BSD-3-Clause, as tiny-skia 0.11.4, tiny-skia-path 0.11.4 carry it.

~~~~
Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:

  * Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.

  * Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in
    the documentation and/or other materials provided with the
    distribution.

  * Neither the name of the copyright holder nor the names of its
    contributors may be used to endorse or promote products derived
    from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
~~~~

### Text 59

Zlib, as tinyvec_macros 0.1.1 carries it.

~~~~
zlib License

(C) 2020 Tomasz "Soveu" Marx

This software is provided 'as-is', without any express or implied
warranty.  In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software
   in a product, an acknowledgment in the product documentation would be
   appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be
   misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
~~~~

### Text 60

Zlib, as tracing-oslog 0.3.0 carries it.

~~~~
The zlib/libpng License
=======================


This software is provided 'as-is', without any express or implied warranty. In
no event will the authors be held liable for any damages arising from the use of
this software.

Permission is granted to anyone to use this software for any purpose, including
commercial applications, and to alter it and redistribute it freely, subject to
the following restrictions:

1.  The origin of this software must not be misrepresented; you must not claim
    that you wrote the original software. If you use this software in a product,
    an acknowledgment in the product documentation would be appreciated but is
    not required.

2.  Altered source versions must be plainly marked as such, and must not be
    misrepresented as being the original software.

3.  This notice may not be removed or altered from any source distribution.
~~~~

### Text 61

Unicode-3.0, as unicode-ident 1.0.24 carries it.

~~~~
UNICODE LICENSE V3

COPYRIGHT AND PERMISSION NOTICE


NOTICE TO USER: Carefully read the following legal agreement. BY
DOWNLOADING, INSTALLING, COPYING OR OTHERWISE USING DATA FILES, AND/OR
SOFTWARE, YOU UNEQUIVOCALLY ACCEPT, AND AGREE TO BE BOUND BY, ALL OF THE
TERMS AND CONDITIONS OF THIS AGREEMENT. IF YOU DO NOT AGREE, DO NOT
DOWNLOAD, INSTALL, COPY, DISTRIBUTE OR USE THE DATA FILES OR SOFTWARE.

Permission is hereby granted, free of charge, to any person obtaining a
copy of data files and any associated documentation (the "Data Files") or
software and any associated documentation (the "Software") to deal in the
Data Files or Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, and/or sell
copies of the Data Files or Software, and to permit persons to whom the
Data Files or Software are furnished to do so, provided that either (a)
this copyright and permission notice appear with all copies of the Data
Files or Software, or (b) this copyright and permission notice appear in
associated Documentation.

THE DATA FILES AND SOFTWARE ARE PROVIDED "AS IS", WITHOUT WARRANTY OF ANY
KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT OF
THIRD PARTY RIGHTS.

IN NO EVENT SHALL THE COPYRIGHT HOLDER OR HOLDERS INCLUDED IN THIS NOTICE
BE LIABLE FOR ANY CLAIM, OR ANY SPECIAL INDIRECT OR CONSEQUENTIAL DAMAGES,
OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS,
WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION,
ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THE DATA
FILES OR SOFTWARE.

Except as contained in this notice, the name of a copyright holder shall
not be used in advertising or otherwise to promote the sale, use or other
dealings in these Data Files or Software without prior written
authorization of the copyright holder.
~~~~

### Text 62

Apache-2.0, as utf8_iter 1.0.4 carries it.

~~~~
Licensed under the Apache License (Version 2.0), or the MIT license,
(the "Licenses") at your option. You may not use this file except in
compliance with one of the Licenses. You may obtain copies of the
Licenses at:

   https://www.apache.org/licenses/LICENSE-2.0
   https://opensource.org/licenses/MIT

Unless required by applicable law or agreed to in writing, software
distributed under the Licenses is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the Licenses for the specific language governing permissions and
limitations under the Licenses.

--

Test code is dedicated to the Public Domain when so designated (see
the individual files for PD/CC0-dedicated sections).

--

The implementation for Utf8CharIndices was adapted from the
CharIndices implementation of the Rust standard library at revision
ab32548539ec38a939c1b58599249f3b54130026
(https://github.com/rust-lang/rust/blob/ab32548539ec38a939c1b58599249f3b54130026/library/core/src/str/iter.rs).

Excerpt from https://github.com/rust-lang/rust/blob/ab32548539ec38a939c1b58599249f3b54130026/COPYRIGHT ,
which refers to
https://github.com/rust-lang/rust/blob/ab32548539ec38a939c1b58599249f3b54130026/LICENSE-APACHE
and
https://github.com/rust-lang/rust/blob/ab32548539ec38a939c1b58599249f3b54130026/LICENSE-MIT
:

For full authorship information, see the version control history or
https://thanks.rust-lang.org

Except as otherwise noted (below and/or in individual files), Rust is
licensed under the Apache License, Version 2.0 <LICENSE-APACHE> or
<http://www.apache.org/licenses/LICENSE-2.0> or the MIT license
<LICENSE-MIT> or <http://opensource.org/licenses/MIT>, at your option.
~~~~

### Text 63

MIT, as wayland-protocols 0.32.13 carries it.

~~~~
Permission is hereby granted, free of charge, to any person obtaining a
copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (including the next
paragraph) shall be included in all copies or substantial portions of the
Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.

---

The above is the version of the MIT "Expat" License used by X.org:

    http://cgit.freedesktop.org/xorg/xserver/tree/COPYING
~~~~

### Text 64

LGPL-2.1, as wayland-protocols-plasma 0.3.12 carries it.

~~~~
                  GNU LESSER GENERAL PUBLIC LICENSE
                       Version 2.1, February 1999

	51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 Everyone is permitted to copy and distribute verbatim copies
 of this license document, but changing it is not allowed.

[This is the first released version of the Lesser GPL.  It also counts
 as the successor of the GNU Library Public License, version 2, hence
 the version number 2.1.]

                            Preamble

  The licenses for most software are designed to take away your
freedom to share and change it.  By contrast, the GNU General Public
Licenses are intended to guarantee your freedom to share and change
free software--to make sure the software is free for all its users.

  This license, the Lesser General Public License, applies to some
specially designated software packages--typically libraries--of the
Free Software Foundation and other authors who decide to use it.  You
can use it too, but we suggest you first think carefully about whether
this license or the ordinary General Public License is the better
strategy to use in any particular case, based on the explanations
below.

  When we speak of free software, we are referring to freedom of use,
not price.  Our General Public Licenses are designed to make sure that
you have the freedom to distribute copies of free software (and charge
for this service if you wish); that you receive source code or can get
it if you want it; that you can change the software and use pieces of
it in new free programs; and that you are informed that you can do
these things.

  To protect your rights, we need to make restrictions that forbid
distributors to deny you these rights or to ask you to surrender these
rights.  These restrictions translate to certain responsibilities for
you if you distribute copies of the library or if you modify it.

  For example, if you distribute copies of the library, whether gratis
or for a fee, you must give the recipients all the rights that we gave
you.  You must make sure that they, too, receive or can get the source
code.  If you link other code with the library, you must provide
complete object files to the recipients, so that they can relink them
with the library after making changes to the library and recompiling
it.  And you must show them these terms so they know their rights.

  We protect your rights with a two-step method: (1) we copyright the
library, and (2) we offer you this license, which gives you legal
permission to copy, distribute and/or modify the library.

  To protect each distributor, we want to make it very clear that
there is no warranty for the free library.  Also, if the library is
modified by someone else and passed on, the recipients should know
that what they have is not the original version, so that the original
author's reputation will not be affected by problems that might be
introduced by others.


  Finally, software patents pose a constant threat to the existence of
any free program.  We wish to make sure that a company cannot
effectively restrict the users of a free program by obtaining a
restrictive license from a patent holder.  Therefore, we insist that
any patent license obtained for a version of the library must be
consistent with the full freedom of use specified in this license.

  Most GNU software, including some libraries, is covered by the
ordinary GNU General Public License.  This license, the GNU Lesser
General Public License, applies to certain designated libraries, and
is quite different from the ordinary General Public License.  We use
this license for certain libraries in order to permit linking those
libraries into non-free programs.

  When a program is linked with a library, whether statically or using
a shared library, the combination of the two is legally speaking a
combined work, a derivative of the original library.  The ordinary
General Public License therefore permits such linking only if the
entire combination fits its criteria of freedom.  The Lesser General
Public License permits more lax criteria for linking other code with
the library.

  We call this license the "Lesser" General Public License because it
does Less to protect the user's freedom than the ordinary General
Public License.  It also provides other free software developers Less
of an advantage over competing non-free programs.  These disadvantages
are the reason we use the ordinary General Public License for many
libraries.  However, the Lesser license provides advantages in certain
special circumstances.

  For example, on rare occasions, there may be a special need to
encourage the widest possible use of a certain library, so that it
becomes a de-facto standard.  To achieve this, non-free programs must
be allowed to use the library.  A more frequent case is that a free
library does the same job as widely used non-free libraries.  In this
case, there is little to gain by limiting the free library to free
software only, so we use the Lesser General Public License.

  In other cases, permission to use a particular library in non-free
programs enables a greater number of people to use a large body of
free software.  For example, permission to use the GNU C Library in
non-free programs enables many more people to use the whole GNU
operating system, as well as its variant, the GNU/Linux operating
system.

  Although the Lesser General Public License is Less protective of the
users' freedom, it does ensure that the user of a program that is
linked with the Library has the freedom and the wherewithal to run
that program using a modified version of the Library.

  The precise terms and conditions for copying, distribution and
modification follow.  Pay close attention to the difference between a
"work based on the library" and a "work that uses the library".  The
former contains code derived from the library, whereas the latter must
be combined with the library in order to run.


                  GNU LESSER GENERAL PUBLIC LICENSE
   TERMS AND CONDITIONS FOR COPYING, DISTRIBUTION AND MODIFICATION

  0. This License Agreement applies to any software library or other
program which contains a notice placed by the copyright holder or
other authorized party saying it may be distributed under the terms of
this Lesser General Public License (also called "this License").
Each licensee is addressed as "you".

  A "library" means a collection of software functions and/or data
prepared so as to be conveniently linked with application programs
(which use some of those functions and data) to form executables.

  The "Library", below, refers to any such software library or work
which has been distributed under these terms.  A "work based on the
Library" means either the Library or any derivative work under
portion of it, either verbatim or with modifications and/or translated
straightforwardly into another language.  (Hereinafter, translation is
included without limitation in the term "modification".)

  "Source code" for a work means the preferred form of the work for
making modifications to it.  For a library, complete source code means
all the source code for all modules it contains, plus any associated
interface definition files, plus the scripts used to control
compilation and installation of the library.

  Activities other than copying, distribution and modification are not
covered by this License; they are outside its scope.  The act of
running a program using the Library is not restricted, and output from
such a program is covered only if its contents constitute a work based
on the Library (independent of the use of the Library in a tool for
writing it).  Whether that is true depends on what the Library does
and what the program that uses the Library does.

  1. You may copy and distribute verbatim copies of the Library's
complete source code as you receive it, in any medium, provided that
you conspicuously and appropriately publish on each copy an
appropriate copyright notice and disclaimer of warranty; keep intact
all the notices that refer to this License and to the absence of any
warranty; and distribute a copy of this License along with the
Library.

  You may charge a fee for the physical act of transferring a copy,
and you may at your option offer warranty protection in exchange for a
fee.


  2. You may modify your copy or copies of the Library or any portion
of it, thus forming a work based on the Library, and copy and
distribute such modifications or work under the terms of Section 1
above, provided that you also meet all of these conditions:

    a) The modified work must itself be a software library.

    b) You must cause the files modified to carry prominent notices
    stating that you changed the files and the date of any change.

    c) You must cause the whole of the work to be licensed at no
    charge to all third parties under the terms of this License.

    d) If a facility in the modified Library refers to a function or a
    table of data to be supplied by an application program that uses
    the facility, other than as an argument passed when the facility
    is invoked, then you must make a good faith effort to ensure that,
    in the event an application does not supply such function or
    table, the facility still operates, and performs whatever part of
    its purpose remains meaningful.

    (For example, a function in a library to compute square roots has
    a purpose that is entirely well-defined independent of the
    application.  Therefore, Subsection 2d requires that any
    application-supplied function or table used by this function must
    be optional: if the application does not supply it, the square
    root function must still compute square roots.)

These requirements apply to the modified work as a whole.  If
identifiable sections of that work are not derived from the Library,
and can be reasonably considered independent and separate works in
themselves, then this License, and its terms, do not apply to those
sections when you distribute them as separate works.  But when you
distribute the same sections as part of a whole which is a work based
on the Library, the distribution of the whole must be on the terms of
this License, whose permissions for other licensees extend to the
entire whole, and thus to each and every part regardless of who wrote
it.

Thus, it is not the intent of this section to claim rights or contest
your rights to work written entirely by you; rather, the intent is to
exercise the right to control the distribution of derivative or
collective works based on the Library.

In addition, mere aggregation of another work not based on the Library
with the Library (or with a work based on the Library) on a volume of
a storage or distribution medium does not bring the other work under
the scope of this License.

  3. You may opt to apply the terms of the ordinary GNU General Public
License instead of this License to a given copy of the Library.  To do
this, you must alter all the notices that refer to this License, so
that they refer to the ordinary GNU General Public License, version 2,
instead of to this License.  (If a newer version than version 2 of the
ordinary GNU General Public License has appeared, then you can specify
that version instead if you wish.)  Do not make any other change in
these notices.


  Once this change is made in a given copy, it is irreversible for
that copy, so the ordinary GNU General Public License applies to all
subsequent copies and derivative works made from that copy.

  This option is useful when you wish to copy part of the code of
the Library into a program that is not a library.

  4. You may copy and distribute the Library (or a portion or
derivative of it, under Section 2) in object code or executable form
under the terms of Sections 1 and 2 above provided that you accompany
it with the complete corresponding machine-readable source code, which
must be distributed under the terms of Sections 1 and 2 above on a
medium customarily used for software interchange.

  If distribution of object code is made by offering access to copy
from a designated place, then offering equivalent access to copy the
source code from the same place satisfies the requirement to
distribute the source code, even though third parties are not
compelled to copy the source along with the object code.

  5. A program that contains no derivative of any portion of the
Library, but is designed to work with the Library by being compiled or
linked with it, is called a "work that uses the Library".  Such a
work, in isolation, is not a derivative work of the Library, and
therefore falls outside the scope of this License.

  However, linking a "work that uses the Library" with the Library
creates an executable that is a derivative of the Library (because it
contains portions of the Library), rather than a "work that uses the
library".  The executable is therefore covered by this License.
Section 6 states terms for distribution of such executables.

  When a "work that uses the Library" uses material from a header file
that is part of the Library, the object code for the work may be a
derivative work of the Library even though the source code is not.
Whether this is true is especially significant if the work can be
linked without the Library, or if the work is itself a library.  The
threshold for this to be true is not precisely defined by law.

  If such an object file uses only numerical parameters, data
structure layouts and accessors, and small macros and small inline
functions (ten lines or less in length), then the use of the object
file is unrestricted, regardless of whether it is legally a derivative
work.  (Executables containing this object code plus portions of the
Library will still fall under Section 6.)

  Otherwise, if the work is a derivative of the Library, you may
distribute the object code for the work under the terms of Section 6.
Any executables containing that work also fall under Section 6,
whether or not they are linked directly with the Library itself.


  6. As an exception to the Sections above, you may also combine or
link a "work that uses the Library" with the Library to produce a
work containing portions of the Library, and distribute that work
under terms of your choice, provided that the terms permit
modification of the work for the customer's own use and reverse
engineering for debugging such modifications.

  You must give prominent notice with each copy of the work that the
Library is used in it and that the Library and its use are covered by
this License.  You must supply a copy of this License.  If the work
during execution displays copyright notices, you must include the
copyright notice for the Library among them, as well as a reference
directing the user to the copy of this License.  Also, you must do one
of these things:

    a) Accompany the work with the complete corresponding
    machine-readable source code for the Library including whatever
    changes were used in the work (which must be distributed under
    Sections 1 and 2 above); and, if the work is an executable linked
    with the Library, with the complete machine-readable "work that
    uses the Library", as object code and/or source code, so that the
    user can modify the Library and then relink to produce a modified
    executable containing the modified Library.  (It is understood
    that the user who changes the contents of definitions files in the
    Library will not necessarily be able to recompile the application
    to use the modified definitions.)

    b) Use a suitable shared library mechanism for linking with the
    Library.  A suitable mechanism is one that (1) uses at run time a
    copy of the library already present on the user's computer system,
    rather than copying library functions into the executable, and (2)
    will operate properly with a modified version of the library, if
    the user installs one, as long as the modified version is
    interface-compatible with the version that the work was made with.

    c) Accompany the work with a written offer, valid for at least
    three years, to give the same user the materials specified in
    Subsection 6a, above, for a charge no more than the cost of
    performing this distribution.

    d) If distribution of the work is made by offering access to copy
    from a designated place, offer equivalent access to copy the above
    specified materials from the same place.

    e) Verify that the user has already received a copy of these
    materials or that you have already sent this user a copy.

  For an executable, the required form of the "work that uses the
Library" must include any data and utility programs needed for
reproducing the executable from it.  However, as a special exception,
the materials to be distributed need not include anything that is
normally distributed (in either source or binary form) with the major
components (compiler, kernel, and so on) of the operating system on
which the executable runs, unless that component itself accompanies
the executable.

  It may happen that this requirement contradicts the license
restrictions of other proprietary libraries that do not normally
accompany the operating system.  Such a contradiction means you cannot
use both them and the Library together in an executable that you
distribute.


  7. You may place library facilities that are a work based on the
Library side-by-side in a single library together with other library
facilities not covered by this License, and distribute such a combined
library, provided that the separate distribution of the work based on
the Library and of the other library facilities is otherwise
permitted, and provided that you do these two things:

    a) Accompany the combined library with a copy of the same work
    based on the Library, uncombined with any other library
    facilities.  This must be distributed under the terms of the
    Sections above.

    b) Give prominent notice with the combined library of the fact
    that part of it is a work based on the Library, and explaining
    where to find the accompanying uncombined form of the same work.

  8. You may not copy, modify, sublicense, link with, or distribute
the Library except as expressly provided under this License.  Any
attempt otherwise to copy, modify, sublicense, link with, or
distribute the Library is void, and will automatically terminate your
rights under this License.  However, parties who have received copies,
or rights, from you under this License will not have their licenses
terminated so long as such parties remain in full compliance.

  9. You are not required to accept this License, since you have not
signed it.  However, nothing else grants you permission to modify or
distribute the Library or its derivative works.  These actions are
prohibited by law if you do not accept this License.  Therefore, by
modifying or distributing the Library (or any work based on the
Library), you indicate your acceptance of this License to do so, and
all its terms and conditions for copying, distributing or modifying
the Library or works based on it.

  10. Each time you redistribute the Library (or any work based on the
Library), the recipient automatically receives a license from the
original licensor to copy, distribute, link with or modify the Library
subject to these terms and conditions.  You may not impose any further
restrictions on the recipients' exercise of the rights granted herein.
You are not responsible for enforcing compliance by third parties with
this License.


  11. If, as a consequence of a court judgment or allegation of patent
infringement or for any other reason (not limited to patent issues),
conditions are imposed on you (whether by court order, agreement or
otherwise) that contradict the conditions of this License, they do not
excuse you from the conditions of this License.  If you cannot
distribute so as to satisfy simultaneously your obligations under this
License and any other pertinent obligations, then as a consequence you
may not distribute the Library at all.  For example, if a patent
license would not permit royalty-free redistribution of the Library by
all those who receive copies directly or indirectly through you, then
the only way you could satisfy both it and this License would be to
refrain entirely from distribution of the Library.

If any portion of this section is held invalid or unenforceable under
any particular circumstance, the balance of the section is intended to
apply, and the section as a whole is intended to apply in other
circumstances.

It is not the purpose of this section to induce you to infringe any
patents or other property right claims or to contest validity of any
such claims; this section has the sole purpose of protecting the
integrity of the free software distribution system which is
implemented by public license practices.  Many people have made
generous contributions to the wide range of software distributed
through that system in reliance on consistent application of that
system; it is up to the author/donor to decide if he or she is willing
to distribute software through any other system and a licensee cannot
impose that choice.

This section is intended to make thoroughly clear what is believed to
be a consequence of the rest of this License.

  12. If the distribution and/or use of the Library is restricted in
certain countries either by patents or by copyrighted interfaces, the
original copyright holder who places the Library under this License
may add an explicit geographical distribution limitation excluding those
countries, so that distribution is permitted only in or among
countries not thus excluded.  In such case, this License incorporates
the limitation as if written in the body of this License.

  13. The Free Software Foundation may publish revised and/or new
versions of the Lesser General Public License from time to time.
Such new versions will be similar in spirit to the present version,
but may differ in detail to address new problems or concerns.

Each version is given a distinguishing version number.  If the Library
specifies a version number of this License which applies to it and
"any later version", you have the option of following the terms and
conditions either of that version or of any later version published by
the Free Software Foundation.  If the Library does not specify a
license version number, you may choose any version ever published by
the Free Software Foundation.


  14. If you wish to incorporate parts of the Library into other free
programs whose distribution conditions are incompatible with these,
write to the author to ask for permission.  For software which is
copyrighted by the Free Software Foundation, write to the Free
Software Foundation; we sometimes make exceptions for this.  Our
decision will be guided by the two goals of preserving the free status
of all derivatives of our free software and of promoting the sharing
and reuse of software generally.

                            NO WARRANTY

  15. BECAUSE THE LIBRARY IS LICENSED FREE OF CHARGE, THERE IS NO
WARRANTY FOR THE LIBRARY, TO THE EXTENT PERMITTED BY APPLICABLE LAW.
EXCEPT WHEN OTHERWISE STATED IN WRITING THE COPYRIGHT HOLDERS AND/OR
OTHER PARTIES PROVIDE THE LIBRARY "AS IS" WITHOUT WARRANTY OF ANY
KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
PURPOSE.  THE ENTIRE RISK AS TO THE QUALITY AND PERFORMANCE OF THE
LIBRARY IS WITH YOU.  SHOULD THE LIBRARY PROVE DEFECTIVE, YOU ASSUME
THE COST OF ALL NECESSARY SERVICING, REPAIR OR CORRECTION.

  16. IN NO EVENT UNLESS REQUIRED BY APPLICABLE LAW OR AGREED TO IN
WRITING WILL ANY COPYRIGHT HOLDER, OR ANY OTHER PARTY WHO MAY MODIFY
AND/OR REDISTRIBUTE THE LIBRARY AS PERMITTED ABOVE, BE LIABLE TO YOU
FOR DAMAGES, INCLUDING ANY GENERAL, SPECIAL, INCIDENTAL OR
CONSEQUENTIAL DAMAGES ARISING OUT OF THE USE OR INABILITY TO USE THE
LIBRARY (INCLUDING BUT NOT LIMITED TO LOSS OF DATA OR DATA BEING
RENDERED INACCURATE OR LOSSES SUSTAINED BY YOU OR THIRD PARTIES OR A
FAILURE OF THE LIBRARY TO OPERATE WITH ANY OTHER SOFTWARE), EVEN IF
SUCH HOLDER OR OTHER PARTY HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH
DAMAGES.

                     END OF TERMS AND CONDITIONS


           How to Apply These Terms to Your New Libraries

  If you develop a new library, and you want it to be of the greatest
possible use to the public, we recommend making it free software that
everyone can redistribute and change.  You can do so by permitting
redistribution under these terms (or, alternatively, under the terms
of the ordinary General Public License).

  To apply these terms, attach the following notices to the library.
It is safest to attach them to the start of each source file to most
effectively convey the exclusion of warranty; and each file should
have at least the "copyright" line and a pointer to where the full
notice is found.


    <one line to give the library's name and a brief idea of what it does.>
    Copyright (C) <year>  <name of author>

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA

Also add information on how to contact you by electronic and paper mail.

You should also get your employer (if you work as a programmer) or
your school, if any, to sign a "copyright disclaimer" for the library,
if necessary.  Here is a sample; alter the names:

  Yoyodyne, Inc., hereby disclaims all copyright interest in the
  library `Frob' (a library for tweaking knobs) written by James
  Random Hacker.

  <signature of Ty Coon>, 1 April 1990
  Ty Coon, President of Vice

That's all there is to it!
~~~~

### Text 65

MIT, as windows 0.62.2, windows-collections 0.3.2, windows-core 0.62.2, windows-future 0.3.2, windows-implement 0.60.2, windows-interface 0.59.3, windows-link 0.2.1, windows-numerics 0.3.1, windows-result 0.4.1, windows-strings 0.5.1, windows-sys 0.45.0, windows-sys 0.52.0, windows-sys 0.59.0, windows-sys 0.60.2, windows-sys 0.61.2, windows-targets 0.42.2, windows-targets 0.52.6, windows-targets 0.53.5, windows-threading 0.2.1, windows_aarch64_gnullvm 0.42.2, windows_aarch64_gnullvm 0.52.6, windows_aarch64_gnullvm 0.53.1, windows_aarch64_msvc 0.42.2, windows_aarch64_msvc 0.52.6, windows_aarch64_msvc 0.53.1, windows_i686_gnu 0.42.2, windows_i686_gnu 0.52.6, windows_i686_gnu 0.53.1, windows_i686_gnullvm 0.52.6, windows_i686_gnullvm 0.53.1, windows_i686_msvc 0.42.2, windows_i686_msvc 0.52.6, windows_i686_msvc 0.53.1, windows_x86_64_gnu 0.42.2, windows_x86_64_gnu 0.52.6, windows_x86_64_gnu 0.53.1, windows_x86_64_gnullvm 0.42.2, windows_x86_64_gnullvm 0.52.6, windows_x86_64_gnullvm 0.53.1, windows_x86_64_msvc 0.42.2, windows_x86_64_msvc 0.52.6, windows_x86_64_msvc 0.53.1 carry it.

~~~~
    MIT License

    Copyright (c) Microsoft Corporation.

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE
~~~~

### Text 66

BSD-2-Clause, as zerocopy 0.8.56, zerocopy-derive 0.8.56 carry it.

~~~~
Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:

   * Redistributions of source code must retain the above copyright
notice, this list of conditions and the following disclaimer.
   * Redistributions in binary form must reproduce the above
copyright notice, this list of conditions and the following disclaimer
in the documentation and/or other materials provided with the
distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
~~~~

### Text 67

Zlib, as zlib-rs 0.6.7 carries it.

~~~~
(C) 2024 Trifecta Tech Foundation

This software is provided 'as-is', without any express or implied
warranty. In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software
   in a product, an acknowledgment in the product documentation would be
   appreciated but is not required.

2. Altered source versions must be plainly marked as such, and must not be
   misrepresented as being the original software.

3. This notice may not be removed or altered from any source distribution.
~~~~

### Text 68

MPL-2.0, from this repository's own LICENSE, and the text taken for symphonia 0.5.5, symphonia-bundle-mp3 0.5.5, symphonia-core 0.5.5, symphonia-metadata 0.5.5, whose packages hold none.

~~~~
Mozilla Public License Version 2.0
==================================

1. Definitions
--------------

1.1. "Contributor"
    means each individual or legal entity that creates, contributes to
    the creation of, or owns Covered Software.

1.2. "Contributor Version"
    means the combination of the Contributions of others (if any) used
    by a Contributor and that particular Contributor's Contribution.

1.3. "Contribution"
    means Covered Software of a particular Contributor.

1.4. "Covered Software"
    means Source Code Form to which the initial Contributor has attached
    the notice in Exhibit A, the Executable Form of such Source Code
    Form, and Modifications of such Source Code Form, in each case
    including portions thereof.

1.5. "Incompatible With Secondary Licenses"
    means

    (a) that the initial Contributor has attached the notice described
        in Exhibit B to the Covered Software; or

    (b) that the Covered Software was made available under the terms of
        version 1.1 or earlier of the License, but not also under the
        terms of a Secondary License.

1.6. "Executable Form"
    means any form of the work other than Source Code Form.

1.7. "Larger Work"
    means a work that combines Covered Software with other material, in
    a separate file or files, that is not Covered Software.

1.8. "License"
    means this document.

1.9. "Licensable"
    means having the right to grant, to the maximum extent possible,
    whether at the time of the initial grant or subsequently, any and
    all of the rights conveyed by this License.

1.10. "Modifications"
    means any of the following:

    (a) any file in Source Code Form that results from an addition to,
        deletion from, or modification of the contents of Covered
        Software; or

    (b) any new file in Source Code Form that contains any Covered
        Software.

1.11. "Patent Claims" of a Contributor
    means any patent claim(s), including without limitation, method,
    process, and apparatus claims, in any patent Licensable by such
    Contributor that would be infringed, but for the grant of the
    License, by the making, using, selling, offering for sale, having
    made, import, or transfer of either its Contributions or its
    Contributor Version.

1.12. "Secondary License"
    means either the GNU General Public License, Version 2.0, the GNU
    Lesser General Public License, Version 2.1, the GNU Affero General
    Public License, Version 3.0, or any later versions of those
    licenses.

1.13. "Source Code Form"
    means the form of the work preferred for making modifications.

1.14. "You" (or "Your")
    means an individual or a legal entity exercising rights under this
    License. For legal entities, "You" includes any entity that
    controls, is controlled by, or is under common control with You. For
    purposes of this definition, "control" means (a) the power, direct
    or indirect, to cause the direction or management of such entity,
    whether by contract or otherwise, or (b) ownership of more than
    fifty percent (50%) of the outstanding shares or beneficial
    ownership of such entity.

2. License Grants and Conditions
--------------------------------

2.1. Grants

Each Contributor hereby grants You a world-wide, royalty-free,
non-exclusive license:

(a) under intellectual property rights (other than patent or trademark)
    Licensable by such Contributor to use, reproduce, make available,
    modify, display, perform, distribute, and otherwise exploit its
    Contributions, either on an unmodified basis, with Modifications, or
    as part of a Larger Work; and

(b) under Patent Claims of such Contributor to make, use, sell, offer
    for sale, have made, import, and otherwise transfer either its
    Contributions or its Contributor Version.

2.2. Effective Date

The licenses granted in Section 2.1 with respect to any Contribution
become effective for each Contribution on the date the Contributor first
distributes such Contribution.

2.3. Limitations on Grant Scope

The licenses granted in this Section 2 are the only rights granted under
this License. No additional rights or licenses will be implied from the
distribution or licensing of Covered Software under this License.
Notwithstanding Section 2.1(b) above, no patent license is granted by a
Contributor:

(a) for any code that a Contributor has removed from Covered Software;
    or

(b) for infringements caused by: (i) Your and any other third party's
    modifications of Covered Software, or (ii) the combination of its
    Contributions with other software (except as part of its Contributor
    Version); or

(c) under Patent Claims infringed by Covered Software in the absence of
    its Contributions.

This License does not grant any rights in the trademarks, service marks,
or logos of any Contributor (except as may be necessary to comply with
the notice requirements in Section 3.4).

2.4. Subsequent Licenses

No Contributor makes additional grants as a result of Your choice to
distribute the Covered Software under a subsequent version of this
License (see Section 10.2) or under the terms of a Secondary License (if
permitted under the terms of Section 3.3).

2.5. Representation

Each Contributor represents that the Contributor believes its
Contributions are its original creation(s) or it has sufficient rights
to grant the rights to its Contributions conveyed by this License.

2.6. Fair Use

This License is not intended to limit any rights You have under
applicable copyright doctrines of fair use, fair dealing, or other
equivalents.

2.7. Conditions

Sections 3.1, 3.2, 3.3, and 3.4 are conditions of the licenses granted
in Section 2.1.

3. Responsibilities
-------------------

3.1. Distribution of Source Form

All distribution of Covered Software in Source Code Form, including any
Modifications that You create or to which You contribute, must be under
the terms of this License. You must inform recipients that the Source
Code Form of the Covered Software is governed by the terms of this
License, and how they can obtain a copy of this License. You may not
attempt to alter or restrict the recipients' rights in the Source Code
Form.

3.2. Distribution of Executable Form

If You distribute Covered Software in Executable Form then:

(a) such Covered Software must also be made available in Source Code
    Form, as described in Section 3.1, and You must inform recipients of
    the Executable Form how they can obtain a copy of such Source Code
    Form by reasonable means in a timely manner, at a charge no more
    than the cost of distribution to the recipient; and

(b) You may distribute such Executable Form under the terms of this
    License, or sublicense it under different terms, provided that the
    license for the Executable Form does not attempt to limit or alter
    the recipients' rights in the Source Code Form under this License.

3.3. Distribution of a Larger Work

You may create and distribute a Larger Work under terms of Your choice,
provided that You also comply with the requirements of this License for
the Covered Software. If the Larger Work is a combination of Covered
Software with a work governed by one or more Secondary Licenses, and the
Covered Software is not Incompatible With Secondary Licenses, this
License permits You to additionally distribute such Covered Software
under the terms of such Secondary License(s), so that the recipient of
the Larger Work may, at their option, further distribute the Covered
Software under the terms of either this License or such Secondary
License(s).

3.4. Notices

You may not remove or alter the substance of any license notices
(including copyright notices, patent notices, disclaimers of warranty,
or limitations of liability) contained within the Source Code Form of
the Covered Software, except that You may alter any license notices to
the extent required to remedy known factual inaccuracies.

3.5. Application of Additional Terms

You may choose to offer, and to charge a fee for, warranty, support,
indemnity or liability obligations to one or more recipients of Covered
Software. However, You may do so only on Your own behalf, and not on
behalf of any Contributor. You must make it absolutely clear that any
such warranty, support, indemnity, or liability obligation is offered by
You alone, and You hereby agree to indemnify every Contributor for any
liability incurred by such Contributor as a result of warranty, support,
indemnity or liability terms You offer. You may include additional
disclaimers of warranty and limitations of liability specific to any
jurisdiction.

4. Inability to Comply Due to Statute or Regulation
---------------------------------------------------

If it is impossible for You to comply with any of the terms of this
License with respect to some or all of the Covered Software due to
statute, judicial order, or regulation then You must: (a) comply with
the terms of this License to the maximum extent possible; and (b)
describe the limitations and the code they affect. Such description must
be placed in a text file included with all distributions of the Covered
Software under this License. Except to the extent prohibited by statute
or regulation, such description must be sufficiently detailed for a
recipient of ordinary skill to be able to understand it.

5. Termination
--------------

5.1. The rights granted under this License will terminate automatically
if You fail to comply with any of its terms. However, if You become
compliant, then the rights granted under this License from a particular
Contributor are reinstated (a) provisionally, unless and until such
Contributor explicitly and finally terminates Your grants, and (b) on an
ongoing basis, if such Contributor fails to notify You of the
non-compliance by some reasonable means prior to 60 days after You have
come back into compliance. Moreover, Your grants from a particular
Contributor are reinstated on an ongoing basis if such Contributor
notifies You of the non-compliance by some reasonable means, this is the
first time You have received notice of non-compliance with this License
from such Contributor, and You become compliant prior to 30 days after
Your receipt of the notice.

5.2. If You initiate litigation against any entity by asserting a patent
infringement claim (excluding declaratory judgment actions,
counter-claims, and cross-claims) alleging that a Contributor Version
directly or indirectly infringes any patent, then the rights granted to
You by any and all Contributors for the Covered Software under Section
2.1 of this License shall terminate.

5.3. In the event of termination under Sections 5.1 or 5.2 above, all
end user license agreements (excluding distributors and resellers) which
have been validly granted by You or Your distributors under this License
prior to termination shall survive termination.

************************************************************************
*                                                                      *
*  6. Disclaimer of Warranty                                           *
*  -------------------------                                           *
*                                                                      *
*  Covered Software is provided under this License on an "as is"       *
*  basis, without warranty of any kind, either expressed, implied, or  *
*  statutory, including, without limitation, warranties that the       *
*  Covered Software is free of defects, merchantable, fit for a        *
*  particular purpose or non-infringing. The entire risk as to the     *
*  quality and performance of the Covered Software is with You.        *
*  Should any Covered Software prove defective in any respect, You     *
*  (not any Contributor) assume the cost of any necessary servicing,   *
*  repair, or correction. This disclaimer of warranty constitutes an   *
*  essential part of this License. No use of any Covered Software is   *
*  authorized under this License except under this disclaimer.         *
*                                                                      *
************************************************************************

************************************************************************
*                                                                      *
*  7. Limitation of Liability                                          *
*  --------------------------                                          *
*                                                                      *
*  Under no circumstances and under no legal theory, whether tort      *
*  (including negligence), contract, or otherwise, shall any           *
*  Contributor, or anyone who distributes Covered Software as          *
*  permitted above, be liable to You for any direct, indirect,         *
*  special, incidental, or consequential damages of any character      *
*  including, without limitation, damages for lost profits, loss of    *
*  goodwill, work stoppage, computer failure or malfunction, or any    *
*  and all other commercial damages or losses, even if such party      *
*  shall have been informed of the possibility of such damages. This   *
*  limitation of liability shall not apply to liability for death or   *
*  personal injury resulting from such party's negligence to the       *
*  extent applicable law prohibits such limitation. Some               *
*  jurisdictions do not allow the exclusion or limitation of           *
*  incidental or consequential damages, so this exclusion and          *
*  limitation may not apply to You.                                    *
*                                                                      *
************************************************************************

8. Litigation
-------------

Any litigation relating to this License may be brought only in the
courts of a jurisdiction where the defendant maintains its principal
place of business and such litigation shall be governed by laws of that
jurisdiction, without reference to its conflict-of-law provisions.
Nothing in this Section shall prevent a party's ability to bring
cross-claims or counter-claims.

9. Miscellaneous
----------------

This License represents the complete agreement concerning the subject
matter hereof. If any provision of this License is held to be
unenforceable, such provision shall be reformed only to the extent
necessary to make it enforceable. Any law or regulation which provides
that the language of a contract shall be construed against the drafter
shall not be used to construe this License against a Contributor.

10. Versions of the License
---------------------------

10.1. New Versions

Mozilla Foundation is the license steward. Except as provided in Section
10.3, no one other than the license steward has the right to modify or
publish new versions of this License. Each version will be given a
distinguishing version number.

10.2. Effect of New Versions

You may distribute the Covered Software under the terms of the version
of the License under which You originally received the Covered Software,
or under the terms of any subsequent version published by the license
steward.

10.3. Modified Versions

If you create software not governed by this License, and you want to
create a new license for such software, you may create and use a
modified version of this License if you rename the license and remove
any references to the name of the license steward (except to note that
such modified license differs from this License).

10.4. Distributing Source Code Form that is Incompatible With Secondary
Licenses

If You choose to distribute Source Code Form that is Incompatible With
Secondary Licenses under the terms of this version of the License, the
notice described in Exhibit B of this License must be attached.

Exhibit A - Source Code Form License Notice
-------------------------------------------

  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.

If it is not possible or desirable to put the notice in a particular
file, then You may include the notice in a location (such as a LICENSE
file in a relevant directory) where a recipient would be likely to look
for such a notice.

You may add additional accurate notices of copyright ownership.

Exhibit B - "Incompatible With Secondary Licenses" Notice
---------------------------------------------------------

  This Source Code Form is "Incompatible With Secondary Licenses", as
  defined by the Mozilla Public License, v. 2.0.
~~~~

### Text 69

OFL-1.1, as Fira Mono, Bevy's default font, inside bevy_text carries it.

~~~~
Digitized data copyright (c) 2012-2015, The Mozilla Foundation and Telefonica S.A.

This Font Software is licensed under the SIL Open Font License, Version 1.1.
This license is copied below, and is also available with a FAQ at:
http://scripts.sil.org/OFL


-----------------------------------------------------------
SIL OPEN FONT LICENSE Version 1.1 - 26 February 2007
-----------------------------------------------------------

PREAMBLE
The goals of the Open Font License (OFL) are to stimulate worldwide
development of collaborative font projects, to support the font creation
efforts of academic and linguistic communities, and to provide a free and
open framework in which fonts may be shared and improved in partnership
with others.

The OFL allows the licensed fonts to be used, studied, modified and
redistributed freely as long as they are not sold by themselves. The
fonts, including any derivative works, can be bundled, embedded, 
redistributed and/or sold with any software provided that any reserved
names are not used by derivative works. The fonts and derivatives,
however, cannot be released under any other type of license. The
requirement for fonts to remain under this license does not apply
to any document created using the fonts or their derivatives.

DEFINITIONS
"Font Software" refers to the set of files released by the Copyright
Holder(s) under this license and clearly marked as such. This may
include source files, build scripts and documentation.

"Reserved Font Name" refers to any names specified as such after the
copyright statement(s).

"Original Version" refers to the collection of Font Software components as
distributed by the Copyright Holder(s).

"Modified Version" refers to any derivative made by adding to, deleting,
or substituting -- in part or in whole -- any of the components of the
Original Version, by changing formats or by porting the Font Software to a
new environment.

"Author" refers to any designer, engineer, programmer, technical
writer or other person who contributed to the Font Software.

PERMISSION & CONDITIONS
Permission is hereby granted, free of charge, to any person obtaining
a copy of the Font Software, to use, study, copy, merge, embed, modify,
redistribute, and sell modified and unmodified copies of the Font
Software, subject to the following conditions:

1) Neither the Font Software nor any of its individual components,
in Original or Modified Versions, may be sold by itself.

2) Original or Modified Versions of the Font Software may be bundled,
redistributed and/or sold with any software, provided that each copy
contains the above copyright notice and this license. These can be
included either as stand-alone text files, human-readable headers or
in the appropriate machine-readable metadata fields within text or
binary files as long as those fields can be easily viewed by the user.

3) No Modified Version of the Font Software may use the Reserved Font
Name(s) unless explicit written permission is granted by the corresponding
Copyright Holder. This restriction only applies to the primary font name as
presented to the users.

4) The name(s) of the Copyright Holder(s) or the Author(s) of the Font
Software shall not be used to promote, endorse or advertise any
Modified Version, except to acknowledge the contribution(s) of the
Copyright Holder(s) and the Author(s) or with their explicit written
permission.

5) The Font Software, modified or unmodified, in part or in whole,
must be distributed entirely under this license, and must not be
distributed under any other license. The requirement for fonts to
remain under this license does not apply to any document created
using the Font Software.

TERMINATION
This license becomes null and void if any of the above conditions are
not met.

DISCLAIMER
THE FONT SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO ANY WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT
OF COPYRIGHT, PATENT, TRADEMARK, OR OTHER RIGHT. IN NO EVENT SHALL THE
COPYRIGHT HOLDER BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
INCLUDING ANY GENERAL, SPECIAL, INDIRECT, INCIDENTAL, OR CONSEQUENTIAL
DAMAGES, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF THE USE OR INABILITY TO USE THE FONT SOFTWARE OR FROM
OTHER DEALINGS IN THE FONT SOFTWARE.
~~~~
