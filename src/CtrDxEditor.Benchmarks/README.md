# Render benchmarks

Measures editor render passes against real Skia rasterization, so the numbers reflect work the desktop
editor actually pays rather than a model of it. Avalonia boots headless with `UseHeadlessDrawing = false`
and draws into a `RenderTargetBitmap`.

## Running

```sh
cd src/CtrDxEditor.Benchmarks
dotnet run -c Release -- --filter '*'          # full run, a few minutes
dotnet run -c Release -- --filter '*' --job short   # quicker, noisier
dotnet run -c Release -- --describe            # print fixture sizes, no benchmarking
```

Release is required — BenchmarkDotNet refuses to measure a Debug build.

`SceneRenderBenchmarks` draws no art unless `CTRDX_CONTENT` points at an installed content folder (the one
holding `images/`), such as the desktop build's `bin/<config>/net10.0/content`:

```sh
CTRDX_CONTENT=../CtrDxEditor.Desktop/bin/Debug/net10.0/content dotnet run -c Release -- --filter '*SceneRender*'
```

## Levels

Levels are generated in code (`StressLevels`), so no level files are needed and the fixtures cannot drift
with someone's saved content. These shapes isolate different costs:

| Shape          | Objects | Total path        | Isolates                          |
| -------------- | ------: | ----------------: | --------------------------------- |
| `OffMapMovers` |      56 | ~1,296,900 units  | path length (sentinel off-map paths) |
| `LocalMovers`  |      56 |     ~26,900 units | mover count, without the length   |
| `DenseStatic`  |      56 |                 0 | object count, with no paths at all |
| `StackedGuns`  | 6 + guns |                0 | per-grab work in a whole frame     |

`StackedGuns` mirrors a real stress level (600+ gun grabs on one point) whose frames allocated ~30 MB each.

`OffMapMovers` is tuned to match a real pathological level (~1.30M level units across 56 movers) that made
the editor lag. Run `--describe` to confirm a fixture still carries the workload it claims to.

## Reading the results

`Zoom` is the axis that matters most for the movement-path pass. Cost that **grows with zoom** means work
is being spent on geometry that is off-screen — segments get longer in screen space as you zoom in, and a
dashed or dotted pen tessellates a segment's whole length before rasterization can discard the invisible
part. Cost that stays **flat across zoom** means clipping is doing its job.

`DenseStatic` is the floor: surface clear plus the per-object walk, drawing no paths. Subtract it to see
what the path drawing itself costs.

`SceneRenderBenchmarks` renders a whole `LevelCanvas` frame. `OnScreen = False` pans the level away so Skia
rejects every draw, leaving the editor's own per-frame work; the gap to `OnScreen = True` is rasterization,
which the headless target does on the CPU and the desktop app hands to the GPU. **Allocated** should grow
linearly with `Guns` — a quadratic climb means something is rebuilding a per-level list per object.
