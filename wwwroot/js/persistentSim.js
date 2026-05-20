(function () {
    // Keep the Unity WebGL <iframe> alive across Blazor navigations by positioning
    // a single fixed-position host over an anchor element placed by the current page.
    // The host moves offscreen (but stays mounted) when no anchor is present so the
    // simulation — and its Supabase writes — keep running in the background.

    const HOST_ID = "sim-frame-host";
    const ANCHOR_ID = "sim-anchor";
    const HIDDEN_LEFT = "-99999px";
    const OFFSCREEN_W = "1280px";
    const OFFSCREEN_H = "800px";

    let started = false;
    let lastTop = null, lastLeft = null, lastWidth = null, lastHeight = null, lastVisible = null;

    function applyStyle(host, top, left, width, height, visible) {
        if (top === lastTop && left === lastLeft && width === lastWidth
            && height === lastHeight && visible === lastVisible) return;
        host.style.position = "fixed";
        host.style.top = top;
        host.style.left = left;
        host.style.width = width;
        host.style.height = height;
        host.style.visibility = visible ? "visible" : "hidden";
        host.style.pointerEvents = visible ? "auto" : "none";
        host.style.zIndex = visible ? "1" : "-1";
        lastTop = top; lastLeft = left; lastWidth = width;
        lastHeight = height; lastVisible = visible;
    }

    function tick() {
        const host = document.getElementById(HOST_ID);
        if (host) {
            const anchor = document.getElementById(ANCHOR_ID);
            if (anchor && anchor.offsetParent !== null) {
                const r = anchor.getBoundingClientRect();
                applyStyle(host, r.top + "px", r.left + "px",
                    r.width + "px", r.height + "px", true);
            } else {
                applyStyle(host, "0px", HIDDEN_LEFT, OFFSCREEN_W, OFFSCREEN_H, false);
            }
        }
        requestAnimationFrame(tick);
    }

    function start() {
        if (started) return;
        started = true;
        requestAnimationFrame(tick);
    }

    window.smartbinSim = { init: start };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", start, { once: true });
    } else {
        start();
    }
})();
