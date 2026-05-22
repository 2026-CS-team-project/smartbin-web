(function () {
    // The WebGL simulation iframe is created once here — outside Blazor's component tree —
    // so Blazor re-renders and SignalR reconnections can never reload (reset) Unity.
    //
    // When the Home dashboard is active, the host is positioned over the #sim-anchor
    // placeholder with z-index:1 so the user sees the simulation.
    //
    // On other pages (no anchor) the host stays at its last known bounds but sinks to
    // z-index:-1, keeping it inside the browser viewport so requestAnimationFrame is
    // never throttled and Unity's Supabase telemetry keeps running.

    const HOST_ID = "sim-frame-host";
    const ANCHOR_ID = "sim-anchor";

    let lastTop = null, lastLeft = null, lastWidth = null, lastHeight = null;
    let lastFocusedBin = undefined; // tracks last bin sent to Unity to avoid duplicate messages

    function createHost() {
        const host = document.createElement("div");
        host.id = HOST_ID;
        host.style.cssText =
            "position:fixed;top:0;left:0;width:100%;height:100%;" +
            "visibility:visible;pointer-events:none;z-index:-1;";

        const iframe = document.createElement("iframe");
        iframe.src = "/webgl/index.html";
        iframe.title = "SmartBin Simulation";
        iframe.allow = "fullscreen";
        iframe.setAttribute("scrolling", "no");
        iframe.style.cssText = "width:100%;height:100%;border:0;display:block;background:#0b1220;";

        host.appendChild(iframe);
        return host;
    }

    function ensureHost() {
        let host = document.getElementById(HOST_ID);
        if (!host) {
            host = createHost();
            document.body.appendChild(host);
            // Reset tracked values so applyStyle re-applies styles to the new element.
            lastTop = null; lastLeft = null; lastWidth = null; lastHeight = null;
        }
        return host;
    }

    function applyStyle(host, top, left, width, height, onTop) {
        host.style.position = "fixed"; // re-apply in case idiomorph stripped it
        host.style.top = top;
        host.style.left = left;
        host.style.width = width;
        host.style.height = height;
        host.style.visibility = "visible";
        host.style.pointerEvents = onTop ? "auto" : "none";
        host.style.zIndex = onTop ? "1" : "-1";
        lastTop = top; lastLeft = left; lastWidth = width; lastHeight = height;
    }

    function hideLoadingHint() {
        const el = document.getElementById("sim-loading");
        if (el) el.style.display = "none";
    }

    function tick() {
        const host = ensureHost();
        const anchor = document.getElementById(ANCHOR_ID);

        const r = anchor ? anchor.getBoundingClientRect() : null;
        if (r && r.width > 0 && r.height > 0) {
            hideLoadingHint();
            applyStyle(host, r.top + "px", r.left + "px",
                r.width + "px", r.height + "px", true);

            // Send camera focus to Unity only when the target bin changes.
            const rawBin = anchor.dataset.bin;
            const binNum = rawBin !== undefined ? parseInt(rawBin, 10) : null;
            if (binNum !== lastFocusedBin) {
                lastFocusedBin = binNum;
                const target = binNum ?? 0;
                // Prefer direct call (set by webgl/index.html after Unity loads).
                if (typeof window.smartbinFocusBin === "function") {
                    window.smartbinFocusBin(target);
                } else {
                    const iframe = host.querySelector("iframe");
                    if (iframe && iframe.contentWindow) {
                        iframe.contentWindow.postMessage(
                            { type: "smartbin-focus-bin", bin: target }, "*");
                    }
                }
            }
        } else {
            // Not on dashboard (or anchor briefly absent during same-page morph).
            // Keep last known position to avoid a resize cycle that corrupts WebGL.
            if (lastTop !== null) {
                applyStyle(host, lastTop, lastLeft, lastWidth, lastHeight, false);
            } else {
                // No saved position yet — just sink behind content without resizing.
                host.style.position = "fixed";
                host.style.pointerEvents = "none";
                host.style.zIndex = "-1";
            }
        }

        requestAnimationFrame(tick);
    }

    let started = false;

    function start() {
        if (started) return;
        started = true;
        ensureHost();
        requestAnimationFrame(tick);
    }

    // After Blazor Enhanced Navigation, idiomorph may strip sim-frame-host's inline styles.
    // applyStyle() re-applies position:fixed on every tick, so styles are restored within one frame.
    // Intentionally NOT resetting lastTop/Left/Width/Height here — clearing them caused a
    // full-viewport resize cycle that blacked out the WebGL canvas on same-page navigation.

    function focusBin(bin) {
        if (typeof window.smartbinFocusBin === "function") {
            window.smartbinFocusBin(bin);
        } else {
            const host = document.getElementById(HOST_ID);
            if (host) {
                const iframe = host.querySelector("iframe");
                if (iframe && iframe.contentWindow) {
                    iframe.contentWindow.postMessage(
                        { type: "smartbin-focus-bin", bin: bin }, "*");
                }
            }
        }
        // Force tick() to re-send on next anchor detection by resetting the cache.
        lastFocusedBin = bin === 0 ? null : bin;
    }

    window.smartbinSim = { init: start, focusBin: focusBin };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", start, { once: true });
    } else {
        start();
    }
})();
