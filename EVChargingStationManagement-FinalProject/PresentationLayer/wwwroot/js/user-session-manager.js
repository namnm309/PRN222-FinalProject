// User Session Manager - Tự động logout khi tài khoản bị khóa
(function () {
    'use strict';

    let userHubConnection = null;
    let isLoggingOut = false;

    // Khởi tạo SignalR connection để lắng nghe thông báo khóa tài khoản
    async function initUserSessionManager() {
        // Chỉ khởi tạo nếu user đã đăng nhập
        if (typeof signalR === 'undefined') {
            console.warn('SignalR library not loaded');
            return;
        }

        try {
            userHubConnection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/user')
                .withAutomaticReconnect()
                .build();

            // Lắng nghe event khi tài khoản bị khóa/mở khóa
            userHubConnection.on('AccountStatusChanged', function (data) {
                console.log('AccountStatusChanged received:', data);
                
                // Nếu tài khoản bị khóa (isActive = false), tự động logout
                if (data.isActive === false && !isLoggingOut) {
                    handleAccountLocked(data.message || 'Tài khoản của bạn đã bị khóa bởi quản trị viên');
                }
            });

            await userHubConnection.start();
            console.log('User Session Manager: SignalR connected');
        } catch (err) {
            console.error('Error starting User Session Manager SignalR connection:', err);
        }
    }

    // Xử lý khi tài khoản bị khóa
    async function handleAccountLocked(message) {
        if (isLoggingOut) {
            return; // Đang logout rồi, không làm gì thêm
        }

        isLoggingOut = true;

        // Hiển thị thông báo
        alert(message + '\n\nBạn sẽ được chuyển đến trang đăng nhập.');

        try {
            // Gọi API logout để revoke refresh tokens
            try {
                const response = await fetch('/api/Auth/logout', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    credentials: 'include'
                });

                // Không cần kiểm tra response, vì có thể user đã bị logout rồi
                if (response.ok) {
                    console.log('Logout API called successfully');
                }
            } catch (err) {
                console.warn('Error calling logout API:', err);
                // Tiếp tục logout dù API có lỗi
            }

            // Đóng SignalR connection
            if (userHubConnection) {
                try {
                    await userHubConnection.stop();
                } catch (err) {
                    console.warn('Error stopping SignalR connection:', err);
                }
            }

            // Xóa tất cả cookies liên quan đến authentication
            // Lưu ý: Cần xóa cookie với domain và path phù hợp
            document.cookie.split(";").forEach(function(c) { 
                document.cookie = c.replace(/^ +/, "").replace(/=.*/, "=;expires=" + new Date().toUTCString() + ";path=/"); 
            });

            // Redirect đến trang login
            window.location.href = '/Auth/Login?locked=true';
        } catch (error) {
            console.error('Error during logout:', error);
            // Vẫn redirect dù có lỗi
            window.location.href = '/Auth/Login?locked=true';
        }
    }

    // Khởi tạo khi DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initUserSessionManager);
    } else {
        initUserSessionManager();
    }

    // Cleanup khi page unload
    window.addEventListener('beforeunload', function() {
        if (userHubConnection) {
            userHubConnection.stop().catch(err => console.warn('Error stopping connection on unload:', err));
        }
    });
})();

