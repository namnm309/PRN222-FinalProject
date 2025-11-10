/**
 * Station Monitoring SignalR Client
 * Real-time monitoring for charging stations and spots
 */

class StationMonitor {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.subscribedStations = new Set();
        this.eventHandlers = {};
    }

    /**
     * Initialize and connect to SignalR hub
     */
    async connect() {
        try {
            // Create connection
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/hubs/stationMonitoring")
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: retryContext => {
                        if (retryContext.elapsedMilliseconds < 60000) {
                            return Math.random() * 10000;
                        } else {
                            return null;
                        }
                    }
                })
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Setup event handlers
            this.setupEventHandlers();

            // Setup connection lifecycle handlers
            this.connection.onclose(error => {
                console.error('[Monitor] Connection closed:', error);
                this.isConnected = false;
                this.handleDisconnect();
            });

            this.connection.onreconnecting(error => {
                console.warn('[Monitor] Reconnecting...', error);
                this.isConnected = false;
                this.showReconnectingStatus();
            });

            this.connection.onreconnected(connectionId => {
                console.log('[Monitor] Reconnected with ID:', connectionId);
                this.isConnected = true;
                this.reconnectAttempts = 0;
                this.hideReconnectingStatus();
                this.resubscribeToStations();
            });

            // Start connection
            await this.connection.start();
            this.isConnected = true;
            console.log('[Monitor] Connected to monitoring hub');

            // Subscribe to all stations by default
            await this.subscribeToAllStations();

            return true;
        } catch (error) {
            console.error('[Monitor] Failed to connect:', error);
            this.handleConnectionError(error);
            return false;
        }
    }

    /**
     * Setup SignalR event handlers
     */
    setupEventHandlers() {
        // Spot status changed
        this.connection.on('SpotStatusChanged', (data) => {
            console.log('[Monitor] Spot status changed:', data);
            this.trigger('spotStatusChanged', data);
            this.updateSpotUI(data);
        });

        // Station status changed
        this.connection.on('StationStatusChanged', (data) => {
            console.log('[Monitor] Station status changed:', data);
            this.trigger('stationStatusChanged', data);
            this.updateStationUI(data);
        });

        // Session started
        this.connection.on('SessionStarted', (data) => {
            console.log('[Monitor] Session started:', data);
            this.trigger('sessionStarted', data);
            this.showNotification('Phiên sạc mới', data.Message, 'info');
        });

        // Session ended
        this.connection.on('SessionEnded', (data) => {
            console.log('[Monitor] Session ended:', data);
            this.trigger('sessionEnded', data);
            this.showNotification('Phiên sạc kết thúc', data.Message, 'success');
        });

        // Stats updated
        this.connection.on('StatsUpdated', (data) => {
            console.log('[Monitor] Stats updated:', data);
            this.trigger('statsUpdated', data);
            this.updateStatsUI(data);
        });

        // Error reported
        this.connection.on('ErrorReported', (data) => {
            console.log('[Monitor] Error reported:', data);
            this.trigger('errorReported', data);
            this.showNotification('Lỗi mới', data.Message, 'error');
        });

        // Maintenance scheduled
        this.connection.on('MaintenanceScheduled', (data) => {
            console.log('[Monitor] Maintenance scheduled:', data);
            this.trigger('maintenanceScheduled', data);
            this.showNotification('Bảo trì mới', data.Message, 'warning');
        });

        // Staff alert
        this.connection.on('StaffAlert', (data) => {
            console.log('[Monitor] Staff alert:', data);
            this.trigger('staffAlert', data);
            this.showAlert(data);
        });

        // Subscription confirmed
        this.connection.on('SubscriptionConfirmed', (stationId) => {
            console.log('[Monitor] Subscription confirmed for:', stationId);
        });

        // Error from hub
        this.connection.on('Error', (message) => {
            console.error('[Monitor] Hub error:', message);
            this.showNotification('Lỗi', message, 'error');
        });

        // Pong response
        this.connection.on('Pong', (timestamp) => {
            console.log('[Monitor] Pong received at:', timestamp);
        });
    }

    /**
     * Subscribe to all stations
     */
    async subscribeToAllStations() {
        if (!this.isConnected) {
            console.warn('[Monitor] Not connected');
            return;
        }

        try {
            await this.connection.invoke('SubscribeToAllStations');
            console.log('[Monitor] Subscribed to all stations');
        } catch (error) {
            console.error('[Monitor] Failed to subscribe to all stations:', error);
        }
    }

    /**
     * Subscribe to specific station
     */
    async subscribeToStation(stationId) {
        if (!this.isConnected) {
            console.warn('[Monitor] Not connected');
            return;
        }

        try {
            await this.connection.invoke('SubscribeToStation', stationId);
            this.subscribedStations.add(stationId);
            console.log('[Monitor] Subscribed to station:', stationId);
        } catch (error) {
            console.error('[Monitor] Failed to subscribe to station:', error);
        }
    }

    /**
     * Unsubscribe from station
     */
    async unsubscribeFromStation(stationId) {
        if (!this.isConnected) return;

        try {
            await this.connection.invoke('UnsubscribeFromStation', stationId);
            this.subscribedStations.delete(stationId);
            console.log('[Monitor] Unsubscribed from station:', stationId);
        } catch (error) {
            console.error('[Monitor] Failed to unsubscribe from station:', error);
        }
    }

    /**
     * Resubscribe to stations after reconnection
     */
    async resubscribeToStations() {
        await this.subscribeToAllStations();
        
        for (const stationId of this.subscribedStations) {
            await this.subscribeToStation(stationId);
        }
    }

    /**
     * Send ping to keep connection alive
     */
    async ping() {
        if (!this.isConnected) return;

        try {
            await this.connection.invoke('Ping');
        } catch (error) {
            console.error('[Monitor] Ping failed:', error);
        }
    }

    /**
     * Disconnect from hub
     */
    async disconnect() {
        if (this.connection) {
            try {
                await this.connection.stop();
                this.isConnected = false;
                console.log('[Monitor] Disconnected');
            } catch (error) {
                console.error('[Monitor] Error disconnecting:', error);
            }
        }
    }

    /**
     * Register event handler
     */
    on(eventName, handler) {
        if (!this.eventHandlers[eventName]) {
            this.eventHandlers[eventName] = [];
        }
        this.eventHandlers[eventName].push(handler);
    }

    /**
     * Trigger event handlers
     */
    trigger(eventName, data) {
        if (this.eventHandlers[eventName]) {
            this.eventHandlers[eventName].forEach(handler => {
                try {
                    handler(data);
                } catch (error) {
                    console.error('[Monitor] Error in event handler:', error);
                }
            });
        }
    }

    /**
     * Update spot UI when status changes
     */
    updateSpotUI(data) {
        const spotElement = document.querySelector(`[data-spot-id="${data.SpotId}"]`);
        if (spotElement) {
            // Remove old status classes
            spotElement.classList.remove('available', 'occupied', 'maintenance', 'outofservice');
            
            // Add new status class
            const statusClass = ['available', 'occupied', 'maintenance', 'outofservice'][data.Status];
            spotElement.classList.add(statusClass);
            
            // Update title
            spotElement.title = `${data.SpotNumber} - ${data.StatusText}`;
            
            // Add animation
            spotElement.classList.add('status-changed');
            setTimeout(() => spotElement.classList.remove('status-changed'), 1000);
        }
    }

    /**
     * Update station UI when status changes
     */
    updateStationUI(data) {
        const stationElement = document.querySelector(`[data-station-id="${data.StationId}"]`);
        if (stationElement) {
            const statusBadge = stationElement.querySelector('.station-status-badge');
            if (statusBadge) {
                statusBadge.className = 'station-status-badge';
                const statusClass = ['active', 'inactive', 'maintenance'][data.Status];
                statusBadge.classList.add(statusClass);
                statusBadge.textContent = data.StatusText;
            }
        }
    }

    /**
     * Update stats UI
     */
    updateStatsUI(data) {
        const elements = {
            statAvailable: data.Available,
            statOccupied: data.Occupied,
            statMaintenance: data.Maintenance,
            statOffline: data.Offline
        };

        for (const [id, value] of Object.entries(elements)) {
            const element = document.getElementById(id);
            if (element) {
                // Animate number change
                this.animateNumber(element, parseInt(element.textContent) || 0, value);
            }
        }
    }

    /**
     * Animate number change
     */
    animateNumber(element, from, to) {
        const duration = 500;
        const start = Date.now();
        const diff = to - from;

        const step = () => {
            const elapsed = Date.now() - start;
            const progress = Math.min(elapsed / duration, 1);
            const current = Math.floor(from + diff * progress);
            element.textContent = current;

            if (progress < 1) {
                requestAnimationFrame(step);
            }
        };

        requestAnimationFrame(step);
    }

    /**
     * Show notification
     */
    showNotification(title, message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-icon">${this.getNotificationIcon(type)}</div>
            <div class="notification-content">
                <div class="notification-title">${title}</div>
                <div class="notification-message">${message}</div>
            </div>
            <button class="notification-close" onclick="this.parentElement.remove()">×</button>
        `;

        // Add to page
        let container = document.getElementById('notificationContainer');
        if (!container) {
            container = document.createElement('div');
            container.id = 'notificationContainer';
            container.className = 'notification-container';
            document.body.appendChild(container);
        }

        container.appendChild(notification);

        // Auto remove after 5 seconds
        setTimeout(() => {
            notification.style.animation = 'slideOut 0.3s ease-out';
            setTimeout(() => notification.remove(), 300);
        }, 5000);
    }

    /**
     * Show critical alert
     */
    showAlert(data) {
        const severity = data.Severity || 'info';
        
        if (severity === 'critical') {
            // Play sound
            this.playAlertSound();
            
            // Show prominent alert
            this.showNotification('⚠️ CẢNH BÁO', data.Message, 'error');
        } else {
            this.showNotification('Thông báo', data.Message, severity);
        }
    }

    /**
     * Play alert sound
     */
    playAlertSound() {
        try {
            const audio = new Audio('/sounds/alert.mp3');
            audio.play().catch(err => console.log('Audio play failed:', err));
        } catch (error) {
            console.log('Could not play alert sound:', error);
        }
    }

    /**
     * Get notification icon
     */
    getNotificationIcon(type) {
        const icons = {
            info: 'ℹ️',
            success: '✅',
            warning: '⚠️',
            error: '❌'
        };
        return icons[type] || icons.info;
    }

    /**
     * Show reconnecting status
     */
    showReconnectingStatus() {
        let statusBar = document.getElementById('connectionStatus');
        if (!statusBar) {
            statusBar = document.createElement('div');
            statusBar.id = 'connectionStatus';
            statusBar.className = 'connection-status reconnecting';
            document.body.appendChild(statusBar);
        }
        statusBar.textContent = '🔄 Đang kết nối lại...';
        statusBar.style.display = 'block';
    }

    /**
     * Hide reconnecting status
     */
    hideReconnectingStatus() {
        const statusBar = document.getElementById('connectionStatus');
        if (statusBar) {
            statusBar.textContent = '✅ Đã kết nối';
            statusBar.className = 'connection-status connected';
            setTimeout(() => {
                statusBar.style.display = 'none';
            }, 2000);
        }
    }

    /**
     * Handle disconnection
     */
    handleDisconnect() {
        this.showReconnectingStatus();
        
        // Try to reconnect
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.reconnectAttempts++;
            console.log(`[Monitor] Reconnect attempt ${this.reconnectAttempts}/${this.maxReconnectAttempts}`);
            setTimeout(() => this.connect(), 3000);
        } else {
            this.showNotification('Mất kết nối', 'Không thể kết nối đến server. Vui lòng tải lại trang.', 'error');
        }
    }

    /**
     * Handle connection error
     */
    handleConnectionError(error) {
        this.showNotification('Lỗi kết nối', 'Không thể kết nối đến hệ thống theo dõi.', 'error');
    }
}

// Create global instance
window.stationMonitor = new StationMonitor();

// Auto-connect when page loads
window.addEventListener('DOMContentLoaded', async () => {
    await window.stationMonitor.connect();
    
    // Keep alive ping every 30 seconds
    setInterval(() => {
        window.stationMonitor.ping();
    }, 30000);
});

// Disconnect when page unloads
window.addEventListener('beforeunload', () => {
    window.stationMonitor.disconnect();
});

