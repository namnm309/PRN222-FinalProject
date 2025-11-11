// SignalR Connection Manager

class SignalRManager {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 3000;
        this.subscriptions = new Set();
    }

    async connect() {
        if (this.connection && this.isConnected) {
            return this.connection;
        }

        if (typeof signalR === 'undefined') {
            console.warn('SignalR library not loaded');
            return null;
        }

        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/station')
                .withAutomaticReconnect()
                .build();

            // Connection event handlers
            this.connection.onclose(() => {
                this.isConnected = false;
                console.log('SignalR connection closed');
            });

            this.connection.onreconnecting(() => {
                this.isConnected = false;
                console.log('SignalR reconnecting...');
            });

            this.connection.onreconnected(() => {
                this.isConnected = true;
                this.reconnectAttempts = 0;
                console.log('SignalR reconnected');
                // Resubscribe to all subscriptions
                this.resubscribeAll();
            });

            await this.connection.start();
            this.isConnected = true;
            this.reconnectAttempts = 0;
            console.log('SignalR connected');

            return this.connection;
        } catch (err) {
            console.error('Error starting SignalR connection:', err);
            this.isConnected = false;
            return null;
        }
    }

    async subscribeSession(sessionId, callback) {
        if (!this.connection || !this.isConnected) {
            await this.connect();
        }

        if (!this.connection) {
            return false;
        }

        try {
            const groupKey = `session-${sessionId}`;
            if (!this.subscriptions.has(groupKey)) {
                await this.connection.invoke('SubscribeSession', sessionId);
                this.subscriptions.add(groupKey);
            }

            // Register callback
            this.connection.on('ChargingProgressUpdated', (updatedSessionId, progress) => {
                if (updatedSessionId === sessionId && callback) {
                    callback(progress);
                }
            });

            return true;
        } catch (err) {
            console.error('Error subscribing to session:', err);
            return false;
        }
    }

    async unsubscribeSession(sessionId) {
        if (!this.connection || !this.isConnected) {
            return;
        }

        try {
            const groupKey = `session-${sessionId}`;
            if (this.subscriptions.has(groupKey)) {
                await this.connection.invoke('UnsubscribeSession', sessionId);
                this.subscriptions.delete(groupKey);
            }
        } catch (err) {
            console.error('Error unsubscribing from session:', err);
        }
    }

    async resubscribeAll() {
        for (const groupKey of this.subscriptions) {
            const match = groupKey.match(/session-(.+)/);
            if (match) {
                const sessionId = match[1];
                try {
                    await this.connection.invoke('SubscribeSession', sessionId);
                } catch (err) {
                    console.error(`Error resubscribing to ${groupKey}:`, err);
                }
            }
        }
    }

    disconnect() {
        if (this.connection) {
            this.connection.stop();
            this.connection = null;
            this.isConnected = false;
            this.subscriptions.clear();
        }
    }
}

// Global instance
window.signalRManager = new SignalRManager();

