// Reads the current CSS safe-area insets. They vary with orientation and Safari's browser chrome, and
// Avalonia's InsetsManager under-reported the landscape right and bottom edges in device measurements,
// so the browser head reads them here instead. Requires viewport-fit=cover on the viewport meta tag.

let probe = null;

const ensureProbe = () => {
    if (probe) {
        return probe;
    }
    probe = document.createElement("div");
    probe.style.cssText =
        "position:fixed;top:0;left:0;width:0;height:0;visibility:hidden;pointer-events:none;" +
        "padding-top:env(safe-area-inset-top,0px);" +
        "padding-right:env(safe-area-inset-right,0px);" +
        "padding-bottom:env(safe-area-inset-bottom,0px);" +
        "padding-left:env(safe-area-inset-left,0px);";
    document.body.appendChild(probe);
    return probe;
};

// Returns [left, top, right, bottom] in CSS pixels, matching Avalonia's Thickness argument order.
export const readInsets = () => {
    const s = getComputedStyle(ensureProbe());
    return [
        parseFloat(s.paddingLeft) || 0,
        parseFloat(s.paddingTop) || 0,
        parseFloat(s.paddingRight) || 0,
        parseFloat(s.paddingBottom) || 0,
    ];
};
