// Drag-drop prevention for Windows WebView2
// Prevents default browser drag behavior that interferes with custom drag-drop

document.addEventListener('dragover', function(e) {
    // Only prevent default on elements that are part of our menu drag-drop
    if (e.target.closest('.menu-root-item, .menu-product-card, .product-grid, .menu-group-content')) {
        e.preventDefault();
    }
}, { passive: false });

document.addEventListener('dragenter', function(e) {
    if (e.target.closest('.menu-root-item, .menu-product-card, .product-grid, .menu-group-content')) {
        e.preventDefault();
    }
}, { passive: false });

// Initialize function called from Blazor
window.initMenuDragDrop = function() {
    // Already attached via document-level listeners above
    console.log('Menu drag-drop prevention initialized');
};