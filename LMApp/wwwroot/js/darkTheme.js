/**
 * Dark Theme Auto-Switch
 * Automatically switches between light and dark themes based on user's system preference
 */

(function () {
    'use strict';

    // Function to apply theme
    function applyTheme(isDark) {
        const htmlElement = document.documentElement;

        if (isDark) {
            htmlElement.setAttribute('data-bs-theme', 'dark');
        } else {
            htmlElement.removeAttribute('data-bs-theme');
        }
    }

    // Function to handle theme change
    function handleThemeChange(e) {
        applyTheme(e.matches);
    }

    // Initialize theme on page load
    function initializeTheme() {
        // Check if the browser supports prefers-color-scheme
        if (window.matchMedia) {
            const darkModeMediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

            // Apply initial theme
            applyTheme(darkModeMediaQuery.matches);

            // Listen for changes in color scheme preference
            if (darkModeMediaQuery.addEventListener) {
                darkModeMediaQuery.addEventListener('change', handleThemeChange);
            } else if (darkModeMediaQuery.addListener) {
                // Fallback for older browsers
                darkModeMediaQuery.addListener(handleThemeChange);
            }
        }
        const htmlElement = document.documentElement;
        htmlElement.removeAttribute('data-loading');
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeTheme);
    } else {
        initializeTheme();
    }
})();
