(function () {
    'use strict';

    let usersTableBody;
    let userDetailModal;
    let changeRoleModal;
    let changeRoleForm;
    let saveRoleBtn;
    let roleFilter;
    let statusFilter;
    let searchInput;
    let pagination;
    let paginationInfo;

    let users = [];
    let currentPage = 1;
    let pageSize = 50;
    let totalCount = 0;
    let totalPages = 0;
    let userHubConnection = null;

    // Initialize SignalR connection
    async function initSignalR() {
        if (typeof signalR === 'undefined') {
            console.warn('SignalR library not loaded');
            return;
        }

        try {
            userHubConnection = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/user')
                .withAutomaticReconnect()
                .build();

            // Listen for user status updates
            userHubConnection.on('UserStatusUpdated', function (userData) {
                console.log('UserStatusUpdated received:', userData);
                
                // Cập nhật user trong danh sách nếu đang hiển thị
                const userIndex = users.findIndex(u => u.id === userData.userId);
                if (userIndex !== -1) {
                    users[userIndex].isActive = userData.isActive;
                    users[userIndex].updatedAt = userData.updatedAt;
                    renderUsers();
                } else {
                    // Nếu user không có trong danh sách hiện tại, reload để đảm bảo đồng bộ
                    loadUsers();
                }

                // Hiển thị thông báo toast/alert
                const statusText = userData.isActive ? 'kích hoạt' : 'vô hiệu hóa';
                showNotification(`Tài khoản ${userData.fullName} (${userData.email}) đã được ${statusText}`, userData.isActive ? 'success' : 'warning');
            });

            await userHubConnection.start();
            console.log('SignalR UserHub connected');
        } catch (err) {
            console.error('Error starting SignalR UserHub connection:', err);
        }
    }

    // Show notification function
    function showNotification(message, type = 'info') {
        // Tạo toast notification
        const toastContainer = document.getElementById('toast-container') || createToastContainer();
        
        const toastId = 'toast-' + Date.now();
        const bgClass = type === 'success' ? 'bg-success' : type === 'warning' ? 'bg-warning' : 'bg-info';
        const textClass = type === 'warning' ? 'text-dark' : 'text-white';
        
        const toastHTML = `
            <div id="${toastId}" class="toast ${bgClass} ${textClass}" role="alert" aria-live="assertive" aria-atomic="true" data-bs-delay="5000">
                <div class="toast-header ${bgClass} ${textClass}">
                    <strong class="me-auto">Thông báo</strong>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
                <div class="toast-body">
                    ${message}
                </div>
            </div>
        `;
        
        toastContainer.insertAdjacentHTML('beforeend', toastHTML);
        const toastElement = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastElement);
        toast.show();
        
        // Xóa toast element sau khi ẩn
        toastElement.addEventListener('hidden.bs.toast', function () {
            toastElement.remove();
        });
    }

    function createToastContainer() {
        const container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        container.style.zIndex = '9999';
        document.body.appendChild(container);
        return container;
    }

    // Initialize when DOM is ready
    function init() {
        usersTableBody = document.getElementById('usersTableBody');
        const userDetailModalElement = document.getElementById('userDetailModal');
        const changeRoleModalElement = document.getElementById('changeRoleModal');
        roleFilter = document.getElementById('UserRoleFilter');
        statusFilter = document.getElementById('UserStatusFilter');
        searchInput = document.getElementById('UserSearch');
        pagination = document.getElementById('pagination');
        paginationInfo = document.getElementById('paginationInfo');

        if (userDetailModalElement) {
            userDetailModal = new bootstrap.Modal(userDetailModalElement);
        }

        if (changeRoleModalElement) {
            changeRoleModal = new bootstrap.Modal(changeRoleModalElement);
        }

        changeRoleForm = document.getElementById('changeRoleForm');
        saveRoleBtn = document.getElementById('saveRoleBtn');

        // Event listeners
        const refreshBtn = document.querySelector('[data-action="refresh-users"]');
        if (refreshBtn) {
            refreshBtn.addEventListener('click', (e) => {
                e.preventDefault();
                currentPage = 1;
                loadUsers();
            });
        }

        if (roleFilter) {
            roleFilter.addEventListener('change', () => {
                currentPage = 1;
                loadUsers();
            });
        }

        if (statusFilter) {
            statusFilter.addEventListener('change', () => {
                currentPage = 1;
                loadUsers();
            });
        }

        if (searchInput) {
            searchInput.addEventListener('input', debounce(() => {
                currentPage = 1;
                loadUsers();
            }, 300));
        }

        if (saveRoleBtn) {
            saveRoleBtn.addEventListener('click', saveRole);
        }

        // Load users on page load
        loadUsers();
        
        // Initialize SignalR
        initSignalR();
    }

    // Wait for DOM to be ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    async function loadUsers() {
        if (!usersTableBody) return;

        try {
            const params = new URLSearchParams({
                page: currentPage.toString(),
                pageSize: pageSize.toString()
            });

            if (roleFilter && roleFilter.value !== '') {
                params.append('role', roleFilter.value);
            }

            if (statusFilter && statusFilter.value !== '') {
                params.append('isActive', statusFilter.value);
            }

            if (searchInput && searchInput.value.trim() !== '') {
                params.append('search', searchInput.value.trim());
            }

            const response = await fetch(`/api/User?${params.toString()}`, {
                credentials: 'include'
            });

            if (!response.ok) {
                if (response.status === 401) {
                    window.location.href = '/Auth/Login';
                    return;
                }
                throw new Error('Failed to load users');
            }

            const result = await response.json();
            users = result.data || [];
            totalCount = result.totalCount || 0;
            totalPages = result.totalPages || 0;

            renderUsers();
            renderPagination();
        } catch (error) {
            console.error('Error loading users:', error);
            usersTableBody.innerHTML = '<tr><td colspan="8" class="text-center text-danger">Lỗi khi tải dữ liệu</td></tr>';
        }
    }

    function renderUsers() {
        if (!usersTableBody) return;

        if (users.length === 0) {
            usersTableBody.innerHTML = '<tr><td colspan="8" class="text-center">Không có dữ liệu</td></tr>';
            return;
        }

        usersTableBody.innerHTML = users.map(user => {
            const roleBadgeClass = getRoleBadgeClass(user.role);
            const roleText = getRoleText(user.role);
            const statusBadge = user.isActive 
                ? '<span class="badge bg-success">Đang hoạt động</span>'
                : '<span class="badge bg-secondary">Đã vô hiệu hóa</span>';
            const createdDate = new Date(user.createdAt).toLocaleDateString('vi-VN');

            // Chỉ hiển thị nút thay đổi role cho Driver và Staff
            const canChangeRole = user.role !== 2; // 2 = Admin
            const changeRoleBtn = canChangeRole
                ? `<button class="btn btn-outline-warning btn-sm btn-change-role" data-user-id="${user.id}" title="Thay đổi vai trò" style="min-width: 38px; height: 32px; padding: 4px 8px;">
                        <i class="bi bi-person-badge"></i>
                   </button>`
                : '';

            return `
                <tr>
                    <td>${escapeHtml(user.fullName)}</td>
                    <td>${escapeHtml(user.username)}</td>
                    <td>${escapeHtml(user.email)}</td>
                    <td>${escapeHtml(user.phone || '')}</td>
                    <td><span class="badge badge-role ${roleBadgeClass}">${roleText}</span></td>
                    <td>${statusBadge}</td>
                    <td>${createdDate}</td>
                    <td>
                        <div style="display: flex; gap: 4px; align-items: center; justify-content: center;">
                            <button class="btn btn-outline-primary btn-sm btn-view-detail" data-user-id="${user.id}" title="Xem chi tiết" style="min-width: 38px; height: 32px; padding: 4px 8px;">
                                <i class="bi bi-eye"></i>
                            </button>
                            ${changeRoleBtn}
                            <button class="btn btn-outline-${user.isActive ? 'danger' : 'success'} btn-sm btn-toggle-status" data-user-id="${user.id}" data-is-active="${user.isActive}" title="${user.isActive ? 'Vô hiệu hóa' : 'Kích hoạt'}" style="min-width: 38px; height: 32px; padding: 4px 8px;">
                                <i class="bi ${user.isActive ? 'bi-toggle-on' : 'bi-toggle-off'}"></i>
                            </button>
                        </div>
                    </td>
                </tr>
            `;
        }).join('');

        // Attach event listeners
        usersTableBody.querySelectorAll('.btn-view-detail').forEach(btn => {
            btn.addEventListener('click', function() {
                const userId = this.getAttribute('data-user-id');
                viewUserDetail(userId);
            });
        });

        usersTableBody.querySelectorAll('.btn-change-role').forEach(btn => {
            btn.addEventListener('click', function() {
                const userId = this.getAttribute('data-user-id');
                openChangeRoleModal(userId);
            });
        });

        usersTableBody.querySelectorAll('.btn-toggle-status').forEach(btn => {
            btn.addEventListener('click', function() {
                const userId = this.getAttribute('data-user-id');
                const isActive = this.getAttribute('data-is-active') === 'true';
                toggleUserStatus(userId, !isActive);
            });
        });
    }

    function renderPagination() {
        if (!pagination || !paginationInfo) return;

        if (totalPages <= 1) {
            pagination.innerHTML = '';
            paginationInfo.textContent = `Hiển thị ${users.length > 0 ? 1 : 0} - ${users.length} của ${totalCount}`;
            return;
        }

        const start = (currentPage - 1) * pageSize + 1;
        const end = Math.min(currentPage * pageSize, totalCount);
        paginationInfo.textContent = `Hiển thị ${start} - ${end} của ${totalCount}`;

        let paginationHTML = '';

        // Previous button
        paginationHTML += `
            <li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${currentPage - 1}">Trước</a>
            </li>
        `;

        // Page numbers
        const maxPagesToShow = 5;
        let startPage = Math.max(1, currentPage - Math.floor(maxPagesToShow / 2));
        let endPage = Math.min(totalPages, startPage + maxPagesToShow - 1);

        if (endPage - startPage < maxPagesToShow - 1) {
            startPage = Math.max(1, endPage - maxPagesToShow + 1);
        }

        if (startPage > 1) {
            paginationHTML += `<li class="page-item"><a class="page-link" href="#" data-page="1">1</a></li>`;
            if (startPage > 2) {
                paginationHTML += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
            }
        }

        for (let i = startPage; i <= endPage; i++) {
            paginationHTML += `
                <li class="page-item ${i === currentPage ? 'active' : ''}">
                    <a class="page-link" href="#" data-page="${i}">${i}</a>
                </li>
            `;
        }

        if (endPage < totalPages) {
            if (endPage < totalPages - 1) {
                paginationHTML += `<li class="page-item disabled"><span class="page-link">...</span></li>`;
            }
            paginationHTML += `<li class="page-item"><a class="page-link" href="#" data-page="${totalPages}">${totalPages}</a></li>`;
        }

        // Next button
        paginationHTML += `
            <li class="page-item ${currentPage === totalPages ? 'disabled' : ''}">
                <a class="page-link" href="#" data-page="${currentPage + 1}">Sau</a>
            </li>
        `;

        pagination.innerHTML = paginationHTML;

        // Attach event listeners
        pagination.querySelectorAll('.page-link').forEach(link => {
            link.addEventListener('click', function(e) {
                e.preventDefault();
                if (this.parentElement.classList.contains('disabled')) return;
                const page = parseInt(this.getAttribute('data-page'));
                if (page && page !== currentPage) {
                    currentPage = page;
                    loadUsers();
                }
            });
        });
    }

    async function viewUserDetail(userId) {
        if (!userDetailModal) return;

        const content = document.getElementById('userDetailContent');
        if (!content) return;

        content.innerHTML = '<p class="text-center">Đang tải...</p>';

        try {
            const response = await fetch(`/api/User/${userId}`, {
                credentials: 'include'
            });

            if (!response.ok) {
                throw new Error('Failed to load user details');
            }

            const user = await response.json();
            const createdDate = new Date(user.createdAt).toLocaleDateString('vi-VN', {
                year: 'numeric',
                month: 'long',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            });
            const updatedDate = new Date(user.updatedAt).toLocaleDateString('vi-VN', {
                year: 'numeric',
                month: 'long',
                day: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            });
            const dob = new Date(user.dateOfBirth).toLocaleDateString('vi-VN');

            content.innerHTML = `
                <div class="row">
                    <div class="col-md-6 mb-3">
                        <strong>Họ và tên:</strong>
                        <p>${escapeHtml(user.fullName)}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <strong>Username:</strong>
                        <p>${escapeHtml(user.username)}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <strong>Email:</strong>
                        <p>${escapeHtml(user.email)}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <strong>Phone:</strong>
                        <p>${escapeHtml(user.phone || 'N/A')}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <strong>Ngày sinh:</strong>
                        <p>${dob}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <strong>Giới tính:</strong>
                        <p>${escapeHtml(user.gender)}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <strong>Vai trò:</strong>
                        <p><span class="badge badge-role ${getRoleBadgeClass(user.role)}">${getRoleText(user.role)}</span></p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <strong>Trạng thái:</strong>
                        <p>${user.isActive ? '<span class="badge bg-success">Đang hoạt động</span>' : '<span class="badge bg-secondary">Đã vô hiệu hóa</span>'}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <strong>Ngày tạo:</strong>
                        <p>${createdDate}</p>
                    </div>
                    <div class="col-md-6 mb-3">
                        <strong>Ngày cập nhật:</strong>
                        <p>${updatedDate}</p>
                    </div>
                    ${user.googleId ? `
                    <div class="col-md-12 mb-3">
                        <strong>Google ID:</strong>
                        <p>${escapeHtml(user.googleId)}</p>
                    </div>
                    ` : ''}
                </div>
            `;

            userDetailModal.show();
        } catch (error) {
            console.error('Error loading user details:', error);
            content.innerHTML = '<p class="text-center text-danger">Lỗi khi tải thông tin người dùng</p>';
        }
    }

    function openChangeRoleModal(userId) {
        if (!changeRoleModal || !changeRoleForm) return;

        const user = users.find(u => u.id === userId);
        if (!user) return;

        // Không cho phép thay đổi role của Admin
        if (user.role === 2) {
            alert('Không thể thay đổi role của Admin');
            return;
        }

        document.getElementById('changeRoleUserId').value = userId;
        document.getElementById('newRole').value = user.role.toString();
        changeRoleModal.show();
    }

    async function saveRole() {
        if (!changeRoleForm) return;

        const userId = document.getElementById('changeRoleUserId').value;
        const newRole = parseInt(document.getElementById('newRole').value);

        if (!userId) {
            alert('Không tìm thấy user ID');
            return;
        }

        try {
            const response = await fetch(`/api/User/${userId}/role`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'include',
                body: JSON.stringify({ role: newRole })
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Failed to update user role');
            }

            alert('Thay đổi vai trò thành công!');
            if (changeRoleModal) {
                changeRoleModal.hide();
            }
            loadUsers();
        } catch (error) {
            console.error('Error updating user role:', error);
            alert('Lỗi khi thay đổi vai trò: ' + error.message);
        }
    }

    async function toggleUserStatus(userId, newStatus) {
        if (!confirm(`Bạn có chắc chắn muốn ${newStatus ? 'kích hoạt' : 'vô hiệu hóa'} tài khoản này?`)) {
            return;
        }

        try {
            const response = await fetch(`/api/User/${userId}/status`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'include',
                body: JSON.stringify({ isActive: newStatus })
            });

            if (!response.ok) {
                let errorMessage = 'Failed to update user status';
                const contentType = response.headers.get('content-type');
                if (contentType && contentType.includes('application/json')) {
                    try {
                        const error = await response.json();
                        errorMessage = error.message || errorMessage;
                    } catch (e) {
                        // Nếu không parse được JSON, dùng status text
                        errorMessage = response.statusText || errorMessage;
                    }
                } else {
                    errorMessage = response.statusText || errorMessage;
                }
                throw new Error(errorMessage);
            }

            // Parse response để đảm bảo request thành công
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                await response.json();
            }

            // Hiển thị thông báo thành công
            const statusText = newStatus ? 'kích hoạt' : 'vô hiệu hóa';
            showNotification(`Đã ${statusText} tài khoản thành công!`, 'success');
            
            // Không cần reload vì SignalR sẽ tự động cập nhật
            // loadUsers(); // SignalR sẽ tự động cập nhật UI
        } catch (error) {
            console.error('Error updating user status:', error);
            alert('Lỗi khi cập nhật trạng thái: ' + error.message);
        }
    }

    function getRoleBadgeClass(role) {
        switch (role) {
            case 0: return 'driver';
            case 1: return 'staff';
            case 2: return 'admin';
            default: return '';
        }
    }

    function getRoleText(role) {
        switch (role) {
            case 0: return 'Driver';
            case 1: return 'Staff';
            case 2: return 'Admin';
            default: return 'Unknown';
        }
    }

    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    function escapeHtml(text) {
        if (text == null) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
})();

