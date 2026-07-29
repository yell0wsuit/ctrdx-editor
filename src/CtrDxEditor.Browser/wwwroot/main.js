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

await dotnetRuntime.runMain(config.mainAssemblyName, [
    globalThis.location.href,
]);
