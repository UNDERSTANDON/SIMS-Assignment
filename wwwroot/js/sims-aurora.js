/**
 * SIMS 2026 Ambient Aurora & Mouse Tracking Inertia Script
 * Super lightweight & 60 FPS GPU-optimized
 */
(function () {
    'use strict';

    let targetX = 0;
    let targetY = 0;
    let currentX = 0;
    let currentY = 0;
    let isRunning = true;
    let rafId = null;

    function onMouseMove(e) {
        // Normalized coordinates from -0.5 to 0.5
        targetX = (e.clientX / window.innerWidth) - 0.5;
        targetY = (e.clientY / window.innerHeight) - 0.5;
    }

    function update() {
        if (!isRunning) return;

        // Smooth Lerp (Linear Interpolation) for inertia motion
        currentX += (targetX - currentX) * 0.05;
        currentY += (targetY - currentY) * 0.05;

        document.documentElement.style.setProperty('--mouse-x', currentX.toFixed(4));
        document.documentElement.style.setProperty('--mouse-y', currentY.toFixed(4));

        rafId = requestAnimationFrame(update);
    }

    function init() {
        window.addEventListener('mousemove', onMouseMove, { passive: true });

        document.addEventListener('visibilitychange', () => {
            isRunning = !document.hidden;
            if (isRunning) {
                update();
            } else if (rafId) {
                cancelAnimationFrame(rafId);
            }
        });

        update();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
