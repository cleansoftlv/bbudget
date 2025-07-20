export function getScrollInfo(element) {
    if (!element)
        return {};

    const scrollTop = element.scrollTop;
    const scrollHeight = element.scrollHeight;
    const clientHeight = element.clientHeight;
    const isAtBottom = scrollTop + clientHeight >= scrollHeight - 50; // 50px tolerance

    return { isAtBottom: isAtBottom };
}

export function fixEnterIssue(element) {
    if (!element || element.dataset.enterFixed)
        return;

    element.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
            console.log('Enter pressed and swallowed');
            e.preventDefault();
            e.stopImmediatePropagation();
        }
    })

    element.dataset.enterFixed = true;
}


function fixEnterHandling(element) {
    var inputs = element.querySelectorAll('.select.dropdown input.form-select, .select.datetime-picker input.dropdown-toggle');
    inputs.forEach(function (input) {
        input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                e.stopImmediatePropagation();
                input.click();
            }
        });
    });
}

export function fixBootstrapEnterHandling(element) {
    if (!element)
        return;

    fixEnterHandling(element);
}

export function fixBootstrapEnterHandlingById(id) {
    const elm = document.getElementById(id);
    if (elm) {
        fixBootstrapEnterHandling(elm);
    }
}

export function focusById(id) {
    const elm = document.getElementById(id);
    if (elm) {
        elm.focus();
    }
}


export function onThrottledEvent(eventName, callbackMethodName, elem, component, interval) {
    if (elem)
        return;

    elem.addEventListener(eventName, _.throttle(e => {
        component.invokeMethodAsync(callbackMethodName);
    }, interval));
}

export function onDebouncedEvent(eventName, callbackMethodName, elem, component, interval) {
    if (elem)
        return;

    elem.addEventListener(eventName, _.debounce(e => {
        component.invokeMethodAsync(callbackMethodName);
    }, interval));
}

export function historyBack() {
    history.back();
}
export function openNewTab(url) {
    window
}

export function attachScrollEndEvent(callbackMethodName, elem, component, fallbackDebounceIntervalMs) {
    if (!elem)
        return;

    if (typeof window.onscrollend !== "undefined") {
        elem.addEventListener("scrollend", e => {
            component.invokeMethodAsync(callbackMethodName);
        });
    }
    else {
        onDebouncedEvent("scroll", callbackMethodName, elem, component, fallbackDebounceIntervalMs);
    }
}

export function downloadFile(filename, content, mimeType) {
    // UTF-8 BOM
    const BOM = "\uFEFF";
    // Prepend BOM to content
    const blob = new Blob([BOM + content], { type: mimeType || "text/plain;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
}

// Transaction form hotkeys - Global document-level handling
let activeTransactionFormComponent = null;
let globalHotkeyListener = null;

export function registerTransactionFormHotkeys(component) {
    // Set this component as the active one
    activeTransactionFormComponent = component;

    // Only add the global listener if it doesn't exist yet
    if (!globalHotkeyListener) {
        globalHotkeyListener = function (e) {
            // Only handle if we have an active component
            if (!activeTransactionFormComponent) {
                return;
            }

            // Ctrl+Shift+S (Save/Create) - Using KeyS for layout independence
            if (e.ctrlKey && e.shiftKey && e.code === 'KeyS' && !e.altKey) {
                e.preventDefault();
                e.stopPropagation();
                activeTransactionFormComponent.invokeMethodAsync('HandleHotkey', 'save');
                return;
            }

            // Ctrl+Shift+P (Save and Next/Create and Next) - Using KeyP for layout independence
            if (e.ctrlKey && e.shiftKey && e.code === 'KeyP' && !e.altKey) {
                e.preventDefault();
                e.stopPropagation();
                activeTransactionFormComponent.invokeMethodAsync('HandleHotkey', 'saveAndNext');
                return;
            }

            // Ctrl+Shift+D (Delete) - Using KeyD for layout independence
            if (e.ctrlKey && e.shiftKey && e.code === 'KeyD' && !e.altKey) {
                e.preventDefault();
                e.stopPropagation();
                activeTransactionFormComponent.invokeMethodAsync('HandleHotkey', 'delete');
                return;
            }

            // Ctrl+Shift+C (Copy) - Using KeyC for layout independence
            if (e.ctrlKey && e.shiftKey && e.code === 'KeyC' && !e.altKey) {
                e.preventDefault();
                e.stopPropagation();
                activeTransactionFormComponent.invokeMethodAsync('HandleHotkey', 'copy');
                return;
            }

            // Ctrl+Shift+G (Create Transfer) - Using KeyG for layout independence
            if (e.ctrlKey && e.shiftKey && e.code === 'KeyG' && !e.altKey) {
                e.preventDefault();
                e.stopPropagation();
                activeTransactionFormComponent.invokeMethodAsync('HandleHotkey', 'createTransfer');
                return;
            }
        };

        // Add the global listener to document
        document.addEventListener('keydown', globalHotkeyListener, true);
    }
}

export function unregisterTransactionFormHotkeys() {
    // Clear the active component
    activeTransactionFormComponent = null;

    // Remove the global listener
    if (globalHotkeyListener) {
        document.removeEventListener('keydown', globalHotkeyListener, true);
        globalHotkeyListener = null;
    }
}

// Scroll selected item into view for keyboard navigation
export function scrollToSelectedItem(containerElement, selectedClass) {
    if (!containerElement) return;

    const selectedElement = containerElement.querySelector('.' + selectedClass);
    if (!selectedElement) return;

    // Find the actual scrollable parent (the one with overflow: auto)
    let scrollableParent = containerElement;
    let parent = containerElement.parentElement;

    while (parent && parent !== document.body) {
        const style = window.getComputedStyle(parent);
        if (style.overflow === 'auto' || style.overflow === 'scroll' ||
            style.overflowY === 'auto' || style.overflowY === 'scroll') {
            scrollableParent = parent;
            break;
        }
        parent = parent.parentElement;
    }

    const elementRect = selectedElement.getBoundingClientRect();
    const containerRect = scrollableParent.getBoundingClientRect();
    if (elementRect.top < containerRect.top) {
        selectedElement.scrollIntoView(true);
    } else if (elementRect.bottom > containerRect.bottom) {
        selectedElement.scrollIntoView(false);
    }
}

export function share(url, title, text) {
    if (!url) return;

    if (navigator.share) {
        let options = {
            url: url
        };
        if (title) {
            options.title = title;
        }
        if (text) {
            options.text = text;
        }

        navigator.share(options).catch(error => {
            console.error('Error sharing:', error);
        });
    } else {
        console.warn('Web Share API not supported in this browser.');
    }
}

