# Where this came from

This crate began as a copy of [`bevy_extended_ui`](https://github.com/exepta/bevy_extended_ui)
by Daniel Ramke, taken at the `main` branch and used under the Apache License 2.0, which is in
`LICENSE` beside this file. The original is the work of somebody else and the good parts of what
follows are theirs.

## Why it was copied rather than depended on

The editor is built on it, and the editor kept running into the same wall: a shortcoming in the
interface meant a workaround in the editor, and the workaround made the next thing harder. A panel
that could not hide a row without leaving a ghost, a color that could not be written without
undoing a layout, a class that could not be changed while the program ran. None of that is fixable
from outside, and all of it is fixable from inside.

Copying it in also makes it a thing a game can use. A game written against this bridge draws its
own interface with the same documents and stylesheets the editor uses, which is only worth offering
if the interface can be fixed when it is wrong.

## What was changed

Renamed from `bevy_extended_ui` to `bcs_ui`, and from `bevy_extended_ui_macros` to `bcs_ui_macros`.

Left out: the parts that need a system library or a service of their own, so that nothing here can
pull SQLite, a native file dialog or an SVG rasteriser into a build by accident. Those are the
translation, dialog, provider and vector image features, and the experimental component framework.

Spelled `color` throughout, including the embedded icon that was `colour.png`, to match this
repository's rule and the public API it is used from. That is the one renamed file; everything else
keeps the name it arrived with.

Everything else that changes is recorded in the file it changed, in the ordinary way, and the
changes worth knowing about are listed in `.github/EDITOR.md` under what the documents can and
cannot do.
