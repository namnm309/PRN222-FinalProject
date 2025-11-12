// Staff Reservations Management
(function () {
    'use strict';

    let allReservations = [];
    let filteredReservations = [];
    let allStations = [];

    // Status mapping (support both number and string enum)
    const statusMap = {
        // Number format
        0: { label: 'Pending', class: 'warning', icon: 'clock-history' },
        1: { label: 'Confirmed', class: 'success', icon: 'check-circle' },
        2: { label: 'CheckedIn', class: 'info', icon: 'box-arrow-in-right' },
        3: { label: 'Completed', class: 'secondary', icon: 'check-all' },
        4: { label: 'Cancelled', class: 'danger', icon: 'x-circle' },
        5: { label: 'Expired', class: 'dark', icon: 'hourglass-bottom' },
        6: { label: 'NoShow', class: 'muted', icon: 'person-x' },
        // String format
        'Pending': { label: 'Pending', class: 'warning', icon: 'clock-history' },
        'Confirmed': { label: 'Confirmed', class: 'success', icon: 'check-circle' },
        'CheckedIn': { label: 'CheckedIn', class: 'info', icon: 'box-arrow-in-right' },
        'Completed': { label: 'Completed', class: 'secondary', icon: 'check-all' },
        'Cancelled': { label: 'Cancelled', class: 'danger', icon: 'x-circle' },
        'Expired': { label: 'Expired', class: 'dark', icon: 'hourglass-bottom' },
        'NoShow': { label: 'NoShow', class: 'muted', icon: 'person-x' }
    };

    const statusLabels = {
        // Number format
        0: 'Chờ xác nhận',
        1: 'Đã xác nhận',
        2: 'Đã check-in',
        3: 'Hoàn thành',
        4: 'Đã hủy',
        5: 'Hết hạn',
        6: 'Không đến',
        // String format
        'Pending': 'Chờ xác nhận',
        'Confirmed': 'Đã xác nhận',
        'CheckedIn': 'Đã check-in',
        'Completed': 'Hoàn thành',
        'Cancelled': 'Đã hủy',
        'Expired': 'Hết hạn',
        'NoShow': 'Không đến'
    };

    // Initialize
    document.addEventListener('DOMContentLoaded', function () {
        loadStations();
        loadReservations();
        setupEventListeners();
        
        // Set default date range (today to next 7 days)
        const today = new Date();
        const nextWeek = new Date();
        nextWeek.setDate(today.getDate() + 7);
        
        document.getElementById('DateFromFilter').valueAsDate = today;
        document.getElementById('DateToFilter').valueAsDate = nextWeek;
    });

    // Load stations for filter
    async function loadStations() {
        try {
            const response = await fetch('/api/ChargingStation', {
                credentials: 'include'
            });

            if (!response.ok) throw new Error('Failed to load stations');

            allStations = await response.json();
            
            const stationFilter = document.getElementById('StationFilter');
            stationFilter.innerHTML = '<option value="">Tất cả trạm</option>';
            
            allStations.forEach(station => {
                const option = document.createElement('option');
                option.value = station.id;
                option.textContent = station.name;
                stationFilter.appendChild(option);
            });
        } catch (error) {
            console.error('Error loading stations:', error);
            showToast('Không thể tải danh sách trạm sạc', 'error');
        }
    }

    // Load all reservations
    async function loadReservations() {
        try {
            showLoading(true);

            const response = await fetch('/api/Reservation/staff/all', {
                credentials: 'include',
                headers: {
                    'Accept': 'application/json'
                }
            });

            if (!response.ok) {
                const errorText = await response.text();
                console.error('API Error:', response.status, errorText);
                throw new Error(`Failed to load reservations: ${response.status} ${response.statusText}`);
            }

            allReservations = await response.json();
            filteredReservations = [...allReservations];
            
            applyFilters();
            renderReservations();
            updateKPIs();
            
        } catch (error) {
            console.error('Error loading reservations:', error);
            showError('Không thể tải danh sách đặt trước. Vui lòng thử lại sau.');
        } finally {
            showLoading(false);
        }
    }

    // Apply filters
    function applyFilters() {
        const stationId = document.getElementById('StationFilter').value;
        const status = document.getElementById('StatusFilter').value;
        const dateFrom = document.getElementById('DateFromFilter').value;
        const dateTo = document.getElementById('DateToFilter').value;
        const searchTerm = document.getElementById('SearchFilter').value.toLowerCase().trim();

        filteredReservations = allReservations.filter(reservation => {
            // Filter by station
            if (stationId && reservation.chargingStationId !== stationId) {
                return false;
            }

            // Filter by status (support both number and string enum)
            if (status !== '') {
                const statusNum = parseInt(status);
                const statusNames = ['Pending', 'Confirmed', 'CheckedIn', 'Completed', 'Cancelled', 'Expired', 'NoShow'];
                const statusName = statusNames[statusNum];
                
                // Check both number and string format
                if (reservation.status !== statusNum && reservation.status !== statusName) {
                    return false;
                }
            }

            // Filter by date range
            if (dateFrom) {
                const resDate = new Date(reservation.scheduledStartTime);
                const filterDate = new Date(dateFrom);
                if (resDate < filterDate) return false;
            }

            if (dateTo) {
                const resDate = new Date(reservation.scheduledStartTime);
                const filterDate = new Date(dateTo);
                filterDate.setHours(23, 59, 59);
                if (resDate > filterDate) return false;
            }

            // Search filter
            if (searchTerm) {
                const matchConfirmation = reservation.confirmationCode?.toLowerCase().includes(searchTerm);
                const matchCustomer = reservation.userFullName?.toLowerCase().includes(searchTerm);
                const matchStation = reservation.chargingStationName?.toLowerCase().includes(searchTerm);
                
                if (!matchConfirmation && !matchCustomer && !matchStation) {
                    return false;
                }
            }

            return true;
        });

        // Sort by scheduled start time (newest first)
        filteredReservations.sort((a, b) => 
            new Date(b.scheduledStartTime) - new Date(a.scheduledStartTime)
        );
    }

    // Render reservations table
    function renderReservations() {
        const tbody = document.getElementById('reservationsTableBody');
        const resultCount = document.querySelector('[data-result-count]');
        
        resultCount.textContent = `${filteredReservations.length} kết quả`;

        if (filteredReservations.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="9" class="text-center text-muted">
                        <i class="bi bi-inbox" style="font-size: 2rem;"></i>
                        <p class="mt-2">Không tìm thấy đặt trước nào</p>
                    </td>
                </tr>
            `;
            return;
        }

        tbody.innerHTML = filteredReservations.map(reservation => {
            const status = statusMap[reservation.status] || { label: 'Unknown', class: 'secondary', icon: 'question-circle' };
            const statusLabel = statusLabels[reservation.status] || 'Không xác định';
            const startTime = new Date(reservation.scheduledStartTime);
            const endTime = new Date(reservation.scheduledEndTime);
            
            return `
                <tr data-reservation-id="${reservation.id}">
                    <td>
                        <strong>${reservation.confirmationCode}</strong>
                    </td>
                    <td>${escapeHtml(reservation.userFullName || 'N/A')}</td>
                    <td>${escapeHtml(reservation.chargingStationName || 'N/A')}</td>
                    <td>
                        <span class="badge bg-secondary">${escapeHtml(reservation.chargingSpotNumber || 'N/A')}</span>
                    </td>
                    <td>
                        <small>${formatDateTime(startTime)}</small>
                    </td>
                    <td>
                        <small>${formatDateTime(endTime)}</small>
                    </td>
                    <td>
                        <small>${escapeHtml(reservation.vehicleName || 'N/A')}</small>
                    </td>
                    <td>
                        <span class="badge bg-${status.class}">
                            <i class="bi bi-${status.icon}"></i> ${statusLabel}
                        </span>
                    </td>
                    <td>
                        <div class="btn-group btn-group-sm" role="group">
                            <button class="btn btn-outline-info" data-action="view-detail" data-id="${reservation.id}" title="Xem chi tiết">
                                <i class="bi bi-eye"></i>
                            </button>
                            ${(reservation.status === 0 || reservation.status === 'Pending' || 
                               reservation.status === 1 || reservation.status === 'Confirmed') ? `
                                <button class="btn btn-outline-primary" data-action="update-status" data-id="${reservation.id}" title="Cập nhật">
                                    <i class="bi bi-pencil"></i>
                                </button>
                            ` : ''}
                        </div>
                    </td>
                </tr>
            `;
        }).join('');

        // Add event listeners to buttons
        tbody.querySelectorAll('[data-action="view-detail"]').forEach(btn => {
            btn.addEventListener('click', () => viewReservationDetail(btn.dataset.id));
        });

        tbody.querySelectorAll('[data-action="update-status"]').forEach(btn => {
            btn.addEventListener('click', () => openUpdateStatusModal(btn.dataset.id));
        });
    }

    // Update KPIs
    function updateKPIs() {
        const kpis = {
            total: allReservations.length,
            pending: allReservations.filter(r => r.status === 0 || r.status === 'Pending').length,
            confirmed: allReservations.filter(r => r.status === 1 || r.status === 'Confirmed').length,
            checkedIn: allReservations.filter(r => r.status === 2 || r.status === 'CheckedIn').length
        };

        document.querySelector('[data-kpi="total"]').textContent = kpis.total;
        document.querySelector('[data-kpi="pending"]').textContent = kpis.pending;
        document.querySelector('[data-kpi="confirmed"]').textContent = kpis.confirmed;
        document.querySelector('[data-kpi="checkedIn"]').textContent = kpis.checkedIn;
    }

    // View reservation detail
    async function viewReservationDetail(reservationId) {
        try {
            const response = await fetch(`/api/Reservation/${reservationId}`, {
                credentials: 'include'
            });

            if (!response.ok) throw new Error('Failed to load reservation detail');

            const reservation = await response.json();
            const status = statusMap[reservation.status] || { label: 'Unknown', class: 'secondary', icon: 'question-circle' };
            const statusLabel = statusLabels[reservation.status] || 'Không xác định';
            
            const content = `
                <div class="reservation-detail">
                    <div class="row mb-3">
                        <div class="col-md-6">
                            <h6>Thông tin đặt chỗ</h6>
                            <table class="table table-sm">
                                <tr>
                                    <th width="40%">Mã đặt chỗ:</th>
                                    <td><strong>${reservation.confirmationCode}</strong></td>
                                </tr>
                                <tr>
                                    <th>Trạng thái:</th>
                                    <td>
                                        <span class="badge bg-${status.class}">
                                            <i class="bi bi-${status.icon}"></i> ${statusLabel}
                                        </span>
                                    </td>
                                </tr>
                                <tr>
                                    <th>Thanh toán trước:</th>
                                    <td>${reservation.isPrepaid ? '<span class="text-success">Có</span>' : '<span class="text-muted">Không</span>'}</td>
                                </tr>
                            </table>
                        </div>
                        <div class="col-md-6">
                            <h6>Khách hàng</h6>
                            <table class="table table-sm">
                                <tr>
                                    <th width="40%">Tên:</th>
                                    <td>${escapeHtml(reservation.userFullName || 'N/A')}</td>
                                </tr>
                                <tr>
                                    <th>Xe:</th>
                                    <td>${escapeHtml(reservation.vehicleName || 'N/A')}</td>
                                </tr>
                            </table>
                        </div>
                    </div>
                    
                    <div class="row mb-3">
                        <div class="col-md-6">
                            <h6>Trạm sạc</h6>
                            <table class="table table-sm">
                                <tr>
                                    <th width="40%">Tên trạm:</th>
                                    <td>${escapeHtml(reservation.chargingStationName || 'N/A')}</td>
                                </tr>
                                <tr>
                                    <th>Điểm sạc:</th>
                                    <td><span class="badge bg-secondary">${escapeHtml(reservation.chargingSpotNumber || 'N/A')}</span></td>
                                </tr>
                            </table>
                        </div>
                        <div class="col-md-6">
                            <h6>Thời gian</h6>
                            <table class="table table-sm">
                                <tr>
                                    <th width="40%">Bắt đầu:</th>
                                    <td>${formatDateTime(new Date(reservation.scheduledStartTime))}</td>
                                </tr>
                                <tr>
                                    <th>Kết thúc:</th>
                                    <td>${formatDateTime(new Date(reservation.scheduledEndTime))}</td>
                                </tr>
                            </table>
                        </div>
                    </div>
                    
                    <div class="row mb-3">
                        <div class="col-md-6">
                            <h6>Ước tính</h6>
                            <table class="table table-sm">
                                <tr>
                                    <th width="40%">Năng lượng:</th>
                                    <td>${reservation.estimatedEnergyKwh ? reservation.estimatedEnergyKwh.toFixed(2) + ' kWh' : 'N/A'}</td>
                                </tr>
                                <tr>
                                    <th>Chi phí:</th>
                                    <td>${reservation.estimatedCost ? formatCurrency(reservation.estimatedCost) : 'N/A'}</td>
                                </tr>
                            </table>
                        </div>
                        <div class="col-md-6">
                            ${reservation.notes ? `
                                <h6>Ghi chú</h6>
                                <p class="text-muted">${escapeHtml(reservation.notes)}</p>
                            ` : ''}
                        </div>
                    </div>
                </div>
            `;

            document.getElementById('reservationDetailContent').innerHTML = content;
            new bootstrap.Modal(document.getElementById('reservationDetailModal')).show();
            
        } catch (error) {
            console.error('Error loading reservation detail:', error);
            showToast('Không thể tải chi tiết đặt trước', 'error');
        }
    }

    // Open update status modal
    function openUpdateStatusModal(reservationId) {
        const reservation = allReservations.find(r => r.id === reservationId);
        if (!reservation) return;

        document.getElementById('updateReservationId').value = reservationId;
        document.getElementById('currentStatus').value = statusLabels[reservation.status] || 'Không xác định';
        document.getElementById('newStatus').value = '';
        document.getElementById('statusNotes').value = '';

        new bootstrap.Modal(document.getElementById('updateStatusModal')).show();
    }

    // Update reservation status
    async function updateReservationStatus() {
        const reservationId = document.getElementById('updateReservationId').value;
        const newStatus = document.getElementById('newStatus').value;
        const notes = document.getElementById('statusNotes').value;

        if (!newStatus) {
            showToast('Vui lòng chọn trạng thái mới', 'warning');
            return;
        }

        // Convert number to string enum
        const statusNames = ['Pending', 'Confirmed', 'CheckedIn', 'Completed', 'Cancelled', 'Expired', 'NoShow'];
        const statusString = statusNames[parseInt(newStatus)];

        try {
            const response = await fetch(`/api/Reservation/${reservationId}/status`, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'include',
                body: JSON.stringify({
                    status: statusString,
                    notes: notes || null
                })
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Failed to update status');
            }

            showToast('Cập nhật trạng thái thành công', 'success');
            bootstrap.Modal.getInstance(document.getElementById('updateStatusModal')).hide();
            
            // Reload reservations
            await loadReservations();
            
        } catch (error) {
            console.error('Error updating status:', error);
            showToast(error.message || 'Không thể cập nhật trạng thái', 'error');
        }
    }

    // Setup event listeners
    function setupEventListeners() {
        // Apply filters button
        document.querySelector('[data-action="apply-filters"]').addEventListener('click', () => {
            applyFilters();
            renderReservations();
        });

        // Clear filters button
        document.querySelector('[data-action="clear-filters"]').addEventListener('click', () => {
            document.getElementById('StationFilter').value = '';
            document.getElementById('StatusFilter').value = '';
            document.getElementById('DateFromFilter').value = '';
            document.getElementById('DateToFilter').value = '';
            document.getElementById('SearchFilter').value = '';
            
            filteredReservations = [...allReservations];
            renderReservations();
        });

        // Refresh button
        document.querySelector('[data-action="refresh-reservations"]').addEventListener('click', () => {
            loadReservations();
        });

        // Confirm update status
        document.querySelector('[data-action="confirm-update-status"]').addEventListener('click', () => {
            updateReservationStatus();
        });

        // Enter key on search
        document.getElementById('SearchFilter').addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                applyFilters();
                renderReservations();
            }
        });
    }

    // Utility functions
    function showLoading(show) {
        const tbody = document.getElementById('reservationsTableBody');
        if (show) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="9" class="text-center">
                        <div class="spinner-border text-primary" role="status">
                            <span class="visually-hidden">Đang tải...</span>
                        </div>
                    </td>
                </tr>
            `;
        }
    }

    function showError(message) {
        const tbody = document.getElementById('reservationsTableBody');
        tbody.innerHTML = `
            <tr>
                <td colspan="9" class="text-center text-danger">
                    <i class="bi bi-exclamation-triangle" style="font-size: 2rem;"></i>
                    <p class="mt-2">${escapeHtml(message)}</p>
                    <button class="btn btn-primary btn-sm" onclick="location.reload()">Thử lại</button>
                </td>
            </tr>
        `;
    }

    function showToast(message, type = 'info') {
        // Create toast container if not exists
        let container = document.querySelector('.toast-container');
        if (!container) {
            container = document.createElement('div');
            container.className = 'toast-container position-fixed top-0 end-0 p-3';
            container.style.zIndex = '9999';
            document.body.appendChild(container);
        }

        // Create toast element
        const toastId = 'toast-' + Date.now();
        const bgClass = type === 'success' ? 'bg-success' : type === 'error' ? 'bg-danger' : type === 'warning' ? 'bg-warning' : 'bg-info';
        const iconClass = type === 'success' ? 'check-circle' : type === 'error' ? 'x-circle' : type === 'warning' ? 'exclamation-triangle' : 'info-circle';
        
        const toastHtml = `
            <div id="${toastId}" class="toast align-items-center text-white ${bgClass} border-0" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="d-flex">
                    <div class="toast-body">
                        <i class="bi bi-${iconClass} me-2"></i>
                        ${message}
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;
        
        container.insertAdjacentHTML('beforeend', toastHtml);
        
        const toastElement = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastElement, { delay: 3000 });
        toast.show();
        
        // Remove toast element after hidden
        toastElement.addEventListener('hidden.bs.toast', () => {
            toastElement.remove();
        });
    }

    function formatDateTime(date) {
        const day = date.getDate().toString().padStart(2, '0');
        const month = (date.getMonth() + 1).toString().padStart(2, '0');
        const year = date.getFullYear();
        const hours = date.getHours().toString().padStart(2, '0');
        const minutes = date.getMinutes().toString().padStart(2, '0');
        
        return `${day}/${month}/${year} ${hours}:${minutes}`;
    }

    function formatCurrency(amount) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(amount);
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

})();

