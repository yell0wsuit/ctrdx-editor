# Cut the Rope DX: Level Editor

![Screenshot of Cut the Rope DX: Level Editor](./misc/ctrdx-editor.webp)

## About

_Cut the Rope DX: Level Editor_ is a standalone app for creating and editing levels for _Cut the Rope: DX_.

Inspired by @adriandrummis's [Cut the Rope Level Editor](https://adriandrummis.github.io/CutTheRopeEditor/), it aims to be portable, lightweight, and streamlined to edit with.

This project is a part of the [_Cut the Rope Home_](https://ctrhome.github.io/fan-projects/) fan project, created by [yell0wsuit](https://github.com/yell0wsuit), with help from [contributors](https://github.com/yell0wsuit/ctrdx-editor/graphs/contributors).

> [!NOTE]
> This project is not, and will never be affiliated with or endorsed by ZeptoLab. All rights to the original game and its assets belong to ZeptoLab.

### Related projects

- [Cut the Rope: DX](https://github.com/yell0wsuit/cuttherope-dx): a fan-made enhancement of the PC version of Cut the Rope, aims to improve the original game's codebase, add new features, and enhance the overall gaming experience.
- [Cut the Rope Level Editor](https://adriandrummis.github.io/CutTheRopeEditor/): a Turbowarp-based level editor for Cut the Rope.

### Download

Coming soon.

## Features

### Editing

- **Drag-and-drop palette**: searchable object list, grouped by _Cut the Rope_ and _Cut the Rope: Experiments_, with the objects unavailable to the current level gated out.
- **Full object roster**: Om Nom, candy (whole and split halves), stars, rope hooks, bubbles, air cushions, gravity buttons, light bulbs, lanterns, spikes, electric sparks, magic hats, bouncers, ghosts, steam pipes, vinyls, mice, conveyors, ant conveyors, bamboo tubes, rockets, snails, mechanical hands, and tutorial icons and text.
- **Direct canvas manipulation**: rotation dials, resize handles, grab rails and radii, mechanical-hand segments, and editable movement paths—drag points to reshape a polyline, right-click one to remove it.
- **Movement modes**: orbit and polyline paths (up to 99 points) with speed, direction, retrace, and closed-loop control.
- **Property panel**: per-attribute editors with inline help for the non-obvious ones (rocket impulse, slack-rope factor, water drain, and so on).
- **Selection and clipboard**: shift to multi-select, select all, cut/copy/paste, delete, and full undo/redo across every edit.
- **Snap to grid** for precise placement.

### Layers

- Add, rename, reorder, merge, and delete layers, with an undo-safe confirmation before a layer and its contents go away.
- Per-layer and per-object visibility and lock toggles—double-click an object on the canvas to lock or unlock it.
- Object counts per layer and expand/collapse-all for large levels.

### Level setup

- **Level settings dialog**: resolution presets or a custom size, rope physics speed, mobile physics, half-candy and night-level mechanics, and the special tutorial staging id.
- **Water**: pool height plus drain speed, with a live readout of how long the pool takes to empty.
- **Customization**: rope color, 17 backgrounds, 52 candy skins, and 17 Om Nom platforms—each pickable at random, and optionally remembered as your defaults for the next new level.

### Viewing

- **Live animation preview**: play object animations while you edit, per object or for the whole level.
- **Diagnostic overlays**: toggle hitboxes, force fields, and movement paths.
- **Tutorial language picker**: levels with localized tutorial text can be viewed one locale at a time.
- **Navigation**: zoom in/out/fit, <kbd>Ctrl</kbd>+wheel zoom, wheel and <kbd>Shift</kbd>+wheel scrolling, middle-drag panning, and pinch/magnify gestures on trackpads and touchscreens.

### Files and workflow

- **Drag and drop to open**: drop a level XML anywhere on the window.
- **Lossless XML round-trip**: unknown layers and attributes are preserved verbatim, so opening and re-saving a level never rewrites data the editor doesn't understand.
- **Level validation**: catches missing candy or Om Nom, mismatched candy halves, a night level with no bulb, duplicate `candyNumber`s, grabs bound to nonexistent candies or bulbs, idle ghosts, candy that spawns inside a spike or on Om Nom's mouth, and more—before you save or play.
- **Review changes**: a diff of the live level against the last save or open, side-by-side or unified, highlighting the exact characters that changed.
- **Playtest**: launch the level straight into _Cut the Rope: DX_ from the File menu.
- **Screenshot export**: save a PNG of the level as rendered.
- **Unsaved-changes protection** on new, open, close, and quit.
- **Guided asset setup**: the editor fetches the game assets it needs on first run, or installs them from a zip you already have.

### Platform

- **Cross-platform**: a native AOT, single-file desktop app for Windows, macOS, and Linux, or run it directly in the browser (WebAssembly)—no install required.

### Keyboard shortcuts

<kbd>Ctrl</kbd> is <kbd>⌘</kbd> on macOS.

| Action                   | Shortcut                                                                                                          |
| ------------------------ | ----------------------------------------------------------------------------------------------------------------- |
| New level                | <kbd>Ctrl</kbd>+<kbd>N</kbd>                                                                                      |
| Open                     | <kbd>Ctrl</kbd>+<kbd>O</kbd>                                                                                      |
| Save / Save As           | <kbd>Ctrl</kbd>+<kbd>S</kbd> / <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>S</kbd>                                      |
| Screenshot               | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>P</kbd>                                                                     |
| Close level              | <kbd>Ctrl</kbd>+<kbd>W</kbd>                                                                                      |
| Undo / Redo              | <kbd>Ctrl</kbd>+<kbd>Z</kbd> / <kbd>Ctrl</kbd>+<kbd>Y</kbd> (<kbd>⌘</kbd>+<kbd>Shift</kbd>+<kbd>Z</kbd> on macOS) |
| Cut / Copy / Paste       | <kbd>Ctrl</kbd>+<kbd>X</kbd> / <kbd>Ctrl</kbd>+<kbd>C</kbd> / <kbd>Ctrl</kbd>+<kbd>V</kbd>                        |
| Select all               | <kbd>Ctrl</kbd>+<kbd>A</kbd>                                                                                      |
| Delete selection         | <kbd>Delete</kbd>                                                                                                 |
| Zoom in / out / fit      | <kbd>Ctrl</kbd>+<kbd>+</kbd> / <kbd>Ctrl</kbd>+<kbd>-</kbd> / <kbd>Ctrl</kbd>+<kbd>0</kbd>                        |
| Toggle animation preview | <kbd>Space</kbd>                                                                                                  |

## Development & contributing

The development of _Cut the Rope DX: Level Editor_ is an ongoing process, and contributions are welcome! If you'd like to help out, please consider the following:

- **Reporting issues**: If you encounter any bugs or issues, please report them on the [GitHub Issues page](https://github.com/yell0wsuit/ctrdx-editor/issues).
- **Feature requests**: If you have ideas for new features or improvements, feel free to submit a feature request through Issues.
- **Contributing code**: If you're a developer and want to contribute code, please fork the repository and submit a pull request. Make sure to read the contribution guidelines in `CONTRIBUTING.md`.

### Building and running

The editor is an [Avalonia](https://avaloniaui.net/) app targeting .NET 10. It ships two front-ends that share the same core: **Desktop** ([src/CtrDxEditor.Desktop](src/CtrDxEditor.Desktop)) and **Browser** ([src/CtrDxEditor.Browser](src/CtrDxEditor.Browser), WebAssembly).

#### Prerequisites

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/), version 10.0.302 or higher.

> [!note]
> The minimum is pinned in `global.json` with `rollForward: latestFeature`, so newer 10.0.x SDKs work automatically. However, if your SDK is older than that, `dotnet` commands will fail with a version-mismatch error until you update.

2. Clone the repository:

    ```bash
    git clone https://github.com/yell0wsuit/ctrdx-editor.git
    cd ctrdx-editor
    ```

    You can also use [GitHub Desktop](https://desktop.github.com/) for ease of cloning.

3. _(Browser only)_ Install the WebAssembly build tools:

    ```bash
    dotnet workload install wasm-tools
    ```

#### Run in development

Desktop:

```bash
dotnet run --project src/CtrDxEditor.Desktop/CtrDxEditor.Desktop.csproj
```

Browser (serves the app locally in your browser):

```bash
dotnet run --project src/CtrDxEditor.Browser/CtrDxEditor.Browser.csproj
```

#### Run the tests

```bash
dotnet test
```

#### Publish a desktop build

Desktop publishes as a self-contained, native AOT single-file executable.

> Note:  
> To make `PublishAot` work, you need to follow the [AOT prerequisites](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/?tabs=windows%2Cnet8#prerequisites) for your OS.

a. Windows

```bash
dotnet publish src/CtrDxEditor.Desktop/CtrDxEditor.Desktop.csproj -c Release -r win-x64 -o ./publish/win-x64
```

b. macOS

```bash
dotnet publish src/CtrDxEditor.Desktop/CtrDxEditor.Desktop.csproj -c Release -r osx-arm64 -o ./publish/osx-arm64
```

> Note:  
> You can change `osx-arm64` to `osx-x64` for the Intel-based version. However, we do not guarantee it will work properly on Intel Macs.

c. Linux

```bash
dotnet publish src/CtrDxEditor.Desktop/CtrDxEditor.Desktop.csproj -c Release -r linux-x64 -o ./publish/linux-x64
```

> Warning:  
> A native AOT binary built on Linux is only guaranteed to run on the same or newer Linux distribution version.

#### Publish the browser build

```bash
dotnet publish src/CtrDxEditor.Browser/CtrDxEditor.Browser.csproj -c Release -o dist
```

The publishable static site is written to `dist/wwwroot`. This is what the [Deploy Browser to GitHub Pages](.github/workflows/deploy-pages.yml) workflow ships on every push to `main`.
