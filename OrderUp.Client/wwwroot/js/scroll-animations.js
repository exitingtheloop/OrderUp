/**
 * Scroll-reveal animations for OrderUp.
 * Uses IntersectionObserver to add a `.visible` class
 * when [data-animate] elements enter the viewport.
 * A MutationObserver auto-detects elements Blazor injects after async renders.
 */
(function () {
    'use strict';

    const THRESHOLD = 0.12;
    const ROOT_MARGIN = '0px 0px -60px 0px';

    // Respect prefers-reduced-motion
    const prefersReduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    function makeVisible(el) {
        el.classList.add('visible');
    }

    let scrollObserver;

    if (!prefersReduced) {
        scrollObserver = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    makeVisible(entry.target);
                    scrollObserver.unobserve(entry.target);
                }
            });
        }, { threshold: THRESHOLD, rootMargin: ROOT_MARGIN });
    }

    function observe(root) {
        var elements = (root || document).querySelectorAll('[data-animate]:not(.visible)');
        elements.forEach(function (el) {
            if (prefersReduced) {
                makeVisible(el);
            } else {
                scrollObserver.observe(el);
            }
        });
    }

    // Auto-detect new [data-animate] elements added by Blazor
    var mutationObserver = new MutationObserver(function (mutations) {
        var dominated = false;
        for (var i = 0; i < mutations.length; i++) {
            if (mutations[i].addedNodes.length > 0) {
                dominated = true;
                break;
            }
        }
        if (dominated) {
            observe(document);
        }
    });

    // Public API
    window.ScrollAnimations = {
        init: function () {
            observe(document);
            mutationObserver.observe(document.getElementById('app') || document.body, {
                childList: true,
                subtree: true
            });
        },
        refresh: function () {
            observe(document);
        }
    };
})();
