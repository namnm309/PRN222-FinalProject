// Driver History JavaScript

document.addEventListener('DOMContentLoaded', function() {
    loadSessionsHistory();
    loadPaymentsHistory();

    // Reload when tab changes
    document.getElementById('sessions-tab').addEventListener('shown.bs.tab', loadSessionsHistory);
    document.getElementById('payments-tab').addEventListener('shown.bs.tab', loadPaymentsHistory);
});

async function loadSessionsHistory() {
    try {
        const response = await fetch('/api/ChargingSession/me?limit=50', {
            credentials: 'include'
        });

        if (response.ok) {
            const sessions = await response.json();
            displaySessionsHistory(sessions);
        } else {
            document.getElementById('sessions-history-body').innerHTML = 
                '<tr><td colspan="6" class="text-center text-danger">Không thể tải dữ liệu</td></tr>';
        }
    } catch (error) {
        console.error('Error loading sessions history:', error);
        document.getElementById('sessions-history-body').innerHTML = 
            '<tr><td colspan="6" class="text-center text-danger">Lỗi khi tải dữ liệu</td></tr>';
    }
}

function displaySessionsHistory(sessions) {
    const tbody = document.getElementById('sessions-history-body');
    
    if (sessions.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted">Chưa có phiên sạc nào</td></tr>';
        return;
    }

    tbody.innerHTML = sessions.map(s => {
        const startTime = new Date(s.sessionStartTime).toLocaleString('vi-VN');
        const stationName = s.chargingStationName || 'N/A';
        const energy = s.energyDeliveredKwh ? s.energyDeliveredKwh.toFixed(2) : '-';
        const cost = s.cost ? new Intl.NumberFormat('vi-VN').format(s.cost) + ' VND' : '-';
        const status = getStatusBadge(s.status);
        
        return `
            <tr>
                <td>${startTime}</td>
                <td>${stationName}</td>
                <td>${energy}</td>
                <td>${cost}</td>
                <td>${status}</td>
                <td>
                    <a href="/Driver/SessionDetail/${s.id}" class="btn btn-sm btn-outline-primary">Xem</a>
                </td>
            </tr>
        `;
    }).join('');
}

async function loadPaymentsHistory() {
    try {
        const response = await fetch('/api/Payment/me?limit=50', {
            credentials: 'include'
        });

        if (response.ok) {
            const payments = await response.json();
            displayPaymentsHistory(payments);
        } else {
            document.getElementById('payments-history-body').innerHTML = 
                '<tr><td colspan="5" class="text-center text-danger">Không thể tải dữ liệu</td></tr>';
        }
    } catch (error) {
        console.error('Error loading payments history:', error);
        document.getElementById('payments-history-body').innerHTML = 
            '<tr><td colspan="5" class="text-center text-danger">Lỗi khi tải dữ liệu</td></tr>';
    }
}

function displayPaymentsHistory(payments) {
    const tbody = document.getElementById('payments-history-body');
    
    if (payments.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" class="text-center text-muted">Chưa có giao dịch thanh toán nào</td></tr>';
        return;
    }

    tbody.innerHTML = payments.map(p => {
        const date = p.processedAt 
            ? new Date(p.processedAt).toLocaleString('vi-VN')
            : (p.createdAt ? new Date(p.createdAt).toLocaleString('vi-VN') : '-');
        const amount = new Intl.NumberFormat('vi-VN').format(p.amount) + ' ' + p.currency;
        const method = getPaymentMethodText(p.method);
        const status = getPaymentStatusBadge(p.status);
        
        return `
            <tr>
                <td>${date}</td>
                <td>${amount}</td>
                <td>${method}</td>
                <td>${status}</td>
                <td>
                    <a href="/Driver/Payment?paymentId=${p.id}" class="btn btn-sm btn-outline-primary">Xem</a>
                </td>
            </tr>
        `;
    }).join('');
}

function getStatusBadge(status) {
    const badges = {
        'InProgress': '<span class="badge bg-warning">Đang sạc</span>',
        'Completed': '<span class="badge bg-success">Hoàn thành</span>',
        'Cancelled': '<span class="badge bg-secondary">Đã hủy</span>',
        'Failed': '<span class="badge bg-danger">Thất bại</span>'
    };
    return badges[status] || `<span class="badge bg-secondary">${status}</span>`;
}

function getPaymentStatusBadge(status) {
    const badges = {
        'Pending': '<span class="badge bg-warning">Chờ thanh toán</span>',
        'Captured': '<span class="badge bg-success">Đã thanh toán</span>',
        'Failed': '<span class="badge bg-danger">Thất bại</span>',
        'Refunded': '<span class="badge bg-info">Đã hoàn tiền</span>'
    };
    return badges[status] || `<span class="badge bg-secondary">${status}</span>`;
}

function getPaymentMethodText(method) {
    const methods = {
        'VNPay': 'VNPay',
        'Wallet': 'Ví điện tử',
        'CreditCard': 'Thẻ tín dụng',
        'DebitCard': 'Thẻ ghi nợ',
        'Cash': 'Tiền mặt',
        'BankTransfer': 'Chuyển khoản',
        'QrCode': 'QR Code'
    };
    return methods[method] || method;
}

