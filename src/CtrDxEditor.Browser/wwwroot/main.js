import { dotnet } from "./_framework/dotnet.js";

const is_browser = typeof window != "undefined";
if (!is_browser) {
    throw new Error(`Expected to be running in a browser`);
}

const progress = document.getElementById("splash-progress");
const hint = document.getElementById("splash-hint");

let highestTotal = 0;
let settleTimer = 0;
let hintTimer = 0;

const reportDownloadProgress = (loaded, total) => {
    if (progress === null) {
        return;
    }

    highestTotal = Math.max(highestTotal, total);
    progress.textContent = `Loading ${loaded} out of ${highestTotal}...`;

    // Downloads moved again, so neither the handover nor the advice that follows it is due.
    clearTimeout(settleTimer);
    clearTimeout(hintTimer);
    if (hint !== null) {
        hint.hidden = true;
    }

    // Downloading is only half of the wait - the runtime still has to compile what arrived, and
    // nothing reports on that - so the counter hands over once it stops moving. A settle delay is
    // what makes "everything is loaded" trustworthy: the counts are briefly equal after the very
    // first asset too, and switching on that alone would flash this message during startup.
    if (loaded >= highestTotal) {
        settleTimer = setTimeout(() => {
            progress.textContent = "Starting the editor...";

            // Compilation is the one stretch with no reporting at all, so a slow one and a hung one
            // look identical. The advice waits well past a normal compile rather than accusing every
            // slow device of being broken.
            if (hint !== null) {
                hintTimer = setTimeout(() => {
                    hint.hidden = false;
                }, 15000);
            }
        }, 400);
    }
};

const builder = dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery();

// Present in dotnet.js but absent from this runtime's DotnetHostBuilder typings, so a future runtime
// could drop it. Probed rather than called outright: losing the counter is a cosmetic regression,
// while throwing here would cost the whole app its boot.
if (typeof builder.withModuleConfig === "function") {
    builder.withModuleConfig({
        onDownloadResourceProgress: reportDownloadProgress,
    });
}

const dotnetRuntime = await builder.create();

const config = dotnetRuntime.getConfig();

await guardUnsavedChanges(dotnetRuntime, config);

await dotnetRuntime.runMain(config.mainAssemblyName, [
    globalThis.location.href,
]);

/**
 * Warns before the tab closes on a level with unsaved edits.
 *
 * The desktop head handles this in MainWindow.OnClosing, but the browser head runs on Avalonia's
 * single-view lifetime, which has no closing event, so the page has to ask the editor. Registered
 * before runMain, which does not return while the app is running; the export answers false until
 * the editor has mounted, so the gap before it does is safe rather than merely brief.
 *
 * Best-effort: if the export cannot be reached the editor still starts, just without the warning.
 *
 * @param {import("./_framework/dotnet.js").RuntimeAPI} runtime
 * @param {{ mainAssemblyName?: string }} config
 */
async function guardUnsavedChanges(runtime, config) {
    let hasUnsavedChanges;
    try {
        const exports = await runtime.getAssemblyExports(
            config.mainAssemblyName,
        );
        hasUnsavedChanges =
            exports.CtrDxEditor?.Browser?.Content?.UnsavedChangesInterop
                ?.HasUnsavedChanges;
    } catch (error) {
        console.warn("unsaved-changes guard unavailable:", error);
        return;
    }
    if (typeof hasUnsavedChanges !== "function") {
        console.warn("unsaved-changes guard unavailable: export not found");
        return;
    }

    globalThis.addEventListener("beforeunload", (event) => {
        // Throwing here would leave the page unclosable, so a broken check gives up its say
        // rather than trapping the user.
        let dirty = false;
        try {
            dirty = hasUnsavedChanges();
        } catch (error) {
            console.warn("unsaved-changes check failed:", error);
        }
        if (dirty) {
            // The browser supplies its own wording; the deprecated returnValue is the only way to
            // reach browsers too old to honour this, and none of those run an Avalonia WASM app.
            event.preventDefault();
        }
    });
}
