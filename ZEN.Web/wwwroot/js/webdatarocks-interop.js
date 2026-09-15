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
                    console.log('Pivot type:', typeof pivot);
                    console.log('Pivot has then:', typeof pivot?.then === 'function');
                    console.log('Pivot has onready:', typeof pivot?.onready === 'function');
                    console.log('Pivot has ready:', typeof pivot?.ready === 'function');
                    
                    // WebDataRocks constructor returns a promise that resolves to the actual pivot instance
                    if (pivot && typeof pivot.then === 'function') {
                        console.log('WebDataRocks returned a promise, waiting for resolution...');
                        pivot.then((actualPivot) => {
                            console.log('Actual pivot instance resolved:', actualPivot);
                            console.log('Actual pivot type:', typeof actualPivot);
                            console.log('Actual pivot methods:', Object.getOwnPropertyNames(Object.getPrototypeOf(actualPivot)).filter(m => typeof actualPivot[m] === 'function'));
                            
                            // Store the ACTUAL pivot instance
                            this.instances.set(containerId, actualPivot);
                            
                            // Call onready if available
                            if (actualPivot.onready) {
                                actualPivot.onready(() => {
                                    console.log('WebDataRocks pivot is ready (via onready)');
                                });
                            }
                            
                            resolve(true);
                        }).catch((error) => {
                            console.error('WebDataRocks promise rejected:', error);
                            resolve(false);
                        });
                    } else if (pivot.onready) {
                        // Direct instance with onready callback
                        console.log('WebDataRocks returned direct instance with onready');
                        this.instances.set(containerId, pivot);
                        pivot.onready(() => {
                            console.log('WebDataRocks pivot is ready (via onready)');
                            resolve(true);
                        });
                    } else if (pivot.ready) {
                        // Direct instance with ready promise
                        console.log('WebDataRocks returned direct instance with ready promise');
                        this.instances.set(containerId, pivot);
                        pivot.ready.then(() => {
                            console.log('WebDataRocks pivot is ready (via ready promise)');
                            resolve(true);
                        });
                    } else {
                        // Try to get the actual pivot from the container after a delay
                        // WebDataRocks might attach the instance to the container element
                        console.log('WebDataRocks returned direct object, will check container for actual instance...');
                        this.instances.set(containerId, pivot); // Store initial object temporarily
                        
                        // Wait a bit and check if the container has the actual pivot
                        setTimeout(() => {
                            const container = document.getElementById(containerId);
                            if (container && container.webDataRocks) {
                                console.log('Found pivot on container.webDataRocks:', container.webDataRocks);
                                console.log('Container pivot methods:', Object.getOwnPropertyNames(Object.getPrototypeOf(container.webDataRocks)).filter(m => typeof container.webDataRocks[m] === 'function'));
                                this.instances.set(containerId, container.webDataRocks);
                            } else if (container && container.wdr) {
                                console.log('Found pivot on container.wdr:', container.wdr);
                                this.instances.set(containerId, container.wdr);
                            } else if (container && container.flexmonster) {
                                console.log('Found pivot on container.flexmonster:', container.flexmonster);
                                this.instances.set(containerId, container.flexmonster);
                            } else {
                                console.log('No pivot found on container, checking window...');
                                // Check if there's a global reference
                                if (window.webDataRocksInstance) {
                                    console.log('Found pivot on window.webDataRocksInstance');
                                    this.instances.set(containerId, window.webDataRocksInstance);
                                }
                            }
                            
                            const storedPivot = this.instances.get(containerId);
                            console.log('Final stored pivot methods:', storedPivot ? Object.getOwnPropertyNames(Object.getPrototypeOf(storedPivot)).filter(m => typeof storedPivot[m] === 'function') : 'none');
                            
                            if (storedPivot && (storedPivot.setReport || storedPivot.setData)) {
                                resolve(true);
                            } else {
                                console.warn('Pivot may not be fully ready, but resolving anyway');
                                resolve(true);
                            }
                        }, 1000);
                    }
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
                console.log('updateData called for:', containerId, 'pivot exists:', !!pivot);
                if (pivot) {
                    const methods = Object.getOwnPropertyNames(Object.getPrototypeOf(pivot)).filter(m => typeof pivot[m] === 'function');
                    console.log('Pivot methods:', methods);
                    
                    // Try multiple approaches
                    let success = false;
                    
                    // Approach 1: setData
                    if (pivot.setData) {
                        console.log('Trying setData...');
                        try {
                            pivot.setData({
                                data: data,
                                dataType: "json"
                            });
                            success = true;
                            console.log('setData succeeded');
                        } catch (e) {
                            console.error('setData failed:', e);
                        }
                    }
                    
                    // Approach 2: setReport with full report
                    if (!success && pivot.setReport) {
                        console.log('Trying setReport...');
                        try {
                            const currentReport = pivot.getReport();
                            console.log('Current report:', currentReport);
                            if (currentReport) {
                                currentReport.dataSource = {
                                    data: data,
                                    dataType: "json"
                                };
                                pivot.setReport(currentReport);
                                success = true;
                                console.log('setReport succeeded');
                            }
                        } catch (e) {
                            console.error('setReport failed:', e);
                        }
                    }
                    
                    // Approach 3: Try refresh/render/resize
                    if (success) {
                        if (pivot.refresh) {
                            try { pivot.refresh(); console.log('refresh called'); } catch (e) { console.error('refresh failed:', e); }
                        }
                        if (pivot.render) {
                            try { pivot.render(); console.log('render called'); } catch (e) { console.error('render failed:', e); }
                        }
                        if (pivot.resize) {
                            try { pivot.resize(); console.log('resize called'); } catch (e) { console.error('resize failed:', e); }
                        }
                        // Force resize after a delay
                        setTimeout(() => {
                            if (pivot.resize) {
                                try { pivot.resize(); console.log('delayed resize called'); } catch (e) { console.error('delayed resize failed:', e); }
                            }
                        }, 100);
                    }
                    
                    resolve(success);
                } else {
                    console.error('No pivot instance found for container:', containerId);
                    console.log('Available instances:', Array.from(this.instances.keys()));
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