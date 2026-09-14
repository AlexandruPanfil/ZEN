// WebDataRocks Interop Module for Blazor
window.webDataRocksInterop = {
    instances: new Map(),
    isWebDataRocksReady: false,
    pendingInitializations: [],

    // Check if WebDataRocks is loaded
    checkWebDataRocksReady: function() {
        if (typeof WebDataRocks !== 'undefined') {
            this.isWebDataRocksReady = true;
            console.log('WebDataRocks is ready');
            // Process any pending initializations
            this.pendingInitializations.forEach(callback => callback());
            this.pendingInitializations = [];
            return true;
        }
        console.log('WebDataRocks not yet loaded, waiting...');
        return false;
    },

    // Wait for WebDataRocks to be ready
    waitForWebDataRocks: function(callback) {
        if (this.isWebDataRocksReady || this.checkWebDataRocksReady()) {
            callback();
        } else {
            this.pendingInitializations.push(callback);
        }
    },

    // Initialize WebDataRocks pivot table
    initialize: function (containerId, data, options = {}) {
        return new Promise((resolve, reject) => {
            const attemptInit = () => {
                console.log(`Initializing WebDataRocks for container: ${containerId}`);
                console.log(`Data rows: ${data ? data.length : 0}`);
                
                const container = document.getElementById(containerId);
                if (!container) {
                    console.error(`Container with id '${containerId}' not found`);
                    console.log('Available elements:', document.querySelectorAll('[id]'));
                    resolve(false);
                    return;
                }

                console.log('Container found:', container);
                console.log('Container dimensions:', container.offsetWidth, 'x', container.offsetHeight);
                
                // Clear container
                container.innerHTML = '';

                // Default configuration
                const config = {
                    container: `#${containerId}`,
                    toolbar: true,
                    height: options.height || 500,
                    report: {
                        dataSource: {
                            data: data || [],
                            dataType: "json"
                        },
                        slice: options.slice || {
                            rows: [],
                            columns: [],
                            measures: []
                        },
                        options: {
                            grid: {
                                type: "flat",
                                showFilter: true,
                                showHeaders: true,
                                fitToContainer: true
                            }
                        }
                    }
                };

                // Merge custom options
                if (options.report) {
                    Object.assign(config.report, options.report);
                }

                console.log('Config:', JSON.stringify(config, null, 2));

                try {
                    // Create WebDataRocks instance
                    console.log('Creating WebDataRocks instance...');
                    const pivot = new WebDataRocks(config);
                    console.log('WebDataRocks instance created successfully');
                    console.log('Pivot instance:', pivot);
                    
                    // Store instance for later reference
                    this.instances.set(containerId, pivot);
                    
                    resolve(true);
                } catch (error) {
                    console.error('Failed to initialize WebDataRocks:', error);
                    console.error('Error stack:', error.stack);
                    resolve(false);
                }
            };

            this.waitForWebDataRocks(attemptInit);
        });
    },

    // Update data source for existing instance
    updateData: function (containerId, data) {
        return new Promise((resolve) => {
            const attemptUpdate = () => {
                const pivot = this.instances.get(containerId);
                if (pivot && pivot.setData) {
                    pivot.setData({
                        data: data,
                        dataType: "json"
                    });
                    resolve(true);
                } else {
                    resolve(false);
                }
            };
            this.waitForWebDataRocks(attemptUpdate);
        });
    },

    // Update report configuration
    updateReport: function (containerId, report) {
        return new Promise((resolve) => {
            const attemptUpdate = () => {
                const pivot = this.instances.get(containerId);
                if (pivot && pivot.setReport) {
                    pivot.setReport(report);
                    resolve(true);
                } else {
                    resolve(false);
                }
            };
            this.waitForWebDataRocks(attemptUpdate);
        });
    },

    // Get current report configuration
    getReport: function (containerId) {
        return new Promise((resolve) => {
            const attemptGet = () => {
                const pivot = this.instances.get(containerId);
                if (pivot && pivot.getReport) {
                    resolve(pivot.getReport());
                } else {
                    resolve(null);
                }
            };
            this.waitForWebDataRocks(attemptGet);
        });
    },

    // Export to CSV
    exportToCSV: function (containerId) {
        return new Promise((resolve) => {
            const attemptExport = () => {
                const pivot = this.instances.get(containerId);
                if (pivot && pivot.exportTo) {
                    pivot.exportTo("csv");
                    resolve(true);
                } else {
                    resolve(false);
                }
            };
            this.waitForWebDataRocks(attemptExport);
        });
    },

    // Export to PDF
    exportToPDF: function (containerId) {
        return new Promise((resolve) => {
            const attemptExport = () => {
                const pivot = this.instances.get(containerId);
                if (pivot && pivot.exportTo) {
                    pivot.exportTo("pdf");
                    resolve(true);
                } else {
                    resolve(false);
                }
            };
            this.waitForWebDataRocks(attemptExport);
        });
    },

    // Handle resize
    resize: function (containerId) {
        return new Promise((resolve) => {
            const attemptResize = () => {
                console.log(`Resizing WebDataRocks for container: ${containerId}`);
                const pivot = this.instances.get(containerId);
                if (pivot && pivot.resize) {
                    pivot.resize();
                    console.log('Resize completed');
                    resolve(true);
                } else {
                    console.log('No pivot instance found for resize');
                    resolve(false);
                }
            };
            this.waitForWebDataRocks(attemptResize);
        });
    },

    // Destroy instance and cleanup
    destroy: function (containerId) {
        return new Promise((resolve) => {
            const attemptDestroy = () => {
                console.log(`Destroying WebDataRocks for container: ${containerId}`);
                const pivot = this.instances.get(containerId);
                if (pivot && pivot.dispose) {
                    pivot.dispose();
                }
                this.instances.delete(containerId);
                
                // Clean up DOM
                const container = document.getElementById(containerId);
                if (container) {
                    container.innerHTML = '';
                }
                console.log('Destroy completed');
                resolve(true);
            };
            this.waitForWebDataRocks(attemptDestroy);
        });
    },

    // Check if instance exists
    hasInstance: function (containerId) {
        return this.instances.has(containerId);
    }
};

// Handle window resize events
window.addEventListener('resize', function() {
    window.webDataRocksInterop.instances.forEach((pivot, containerId) => {
        if (pivot && pivot.resize) {
            pivot.resize();
        }
    });
});