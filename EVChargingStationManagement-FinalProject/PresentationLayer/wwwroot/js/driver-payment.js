// Driver Payment JavaScript

let sessionData = null;
let paymentData = null;

document.addEventListener('DOMContentLoaded', function() {
    const sessionId = document.getElementById('session-id').value;
    const paymentId = document.getElementById('payment-id').value;

    if (sessionId) {
        loadSessionAndCreatePayment(sessionId);
    } else if (paymentId) {
        loadPayment(paymentId);
    } else {
        document.getElementById('payment-content').innerHTML = 
            '<div class="text-center text-danger">Không có thông tin thanh toán</div>';
    }
});

async function loadSessionAndCreatePayment(sessionId) {
    try {
        const response = await fetch(`/api/ChargingSession/${sessionId}`, {
            credentials: 'include'
        });

        if (response.ok) {
            sessionData = await response.json();
            displayPaymentForm(sessionData);
        } else {
            document.getElementById('payment-content').innerHTML = 
                '<div class="text-center text-danger">Không tìm thấy phiên sạc</div>';
        }
    } catch (error) {
        console.error('Error loading session:', error);
        document.getElementById('payment-content').innerHTML = 
            '<div class="text-center text-danger">Lỗi khi tải dữ liệu</div>';
    }
}

async function loadPayment(paymentId) {
    try {
        const response = await fetch(`/api/Payment/${paymentId}`, {
            credentials: 'include'
        });

        if (response.ok) {
            paymentData = await response.json();
            displayPaymentInfo(paymentData);
        } else {
            document.getElementById('payment-content').innerHTML = 
                '<div class="text-center text-danger">Không tìm thấy thông tin thanh toán</div>';
        }
    } catch (error) {
        console.error('Error loading payment:', error);
        document.getElementById('payment-content').innerHTML = 
            '<div class="text-center text-danger">Lỗi khi tải dữ liệu</div>';
    }
}

function displayPaymentForm(session) {
    const stationName = session.chargingStationName || 'N/A';
    const spotNumber = session.chargingSpotNumber || 'N/A';
    const energy = session.energyDeliveredKwh || 0;
    const cost = session.cost || 0;
    const pricePerKwh = session.pricePerKwh || 0;

    document.getElementById('payment-content').innerHTML = `
        <div class="invoice-details">
            <div class="invoice-section">
                <h4>Thông tin phiên sạc</h4>
                <div class="detail-row">
                    <strong>Trạm sạc:</strong> ${stationName}
                </div>
                <div class="detail-row">
                    <strong>Cổng sạc:</strong> ${spotNumber}
                </div>
                <div class="detail-row">
                    <strong>Năng lượng đã sạc:</strong> ${energy.toFixed(2)} kWh
                </div>
                <div class="detail-row">
                    <strong>Giá:</strong> ${new Intl.NumberFormat('vi-VN').format(pricePerKwh)} VND/kWh
                </div>
            </div>

            <div class="invoice-section">
                <h4>Chi tiết thanh toán</h4>
                <div class="cost-breakdown">
                    <div class="cost-item">
                        <span>Năng lượng:</span>
                        <span>${energy.toFixed(2)} kWh × ${new Intl.NumberFormat('vi-VN').format(pricePerKwh)} VND</span>
                    </div>
                    <div class="cost-total">
                        <strong>Tổng cộng:</strong>
                        <strong>${new Intl.NumberFormat('vi-VN').format(cost)} VND</strong>
                    </div>
                </div>
            </div>

            <div class="payment-methods">
                <h4>Chọn phương thức thanh toán</h4>
                <div class="payment-options">
                    <button class="btn btn-primary btn-lg w-100 mb-2" onclick="payWithVNPay('${session.id}', ${cost})">
                        <i class="bi bi-credit-card"></i> Thanh toán VNPay
                    </button>
                    <button class="btn btn-success btn-lg w-100 mb-2" onclick="payWithCash('${session.id}', ${cost})">
                        <i class="bi bi-cash-coin"></i> Thanh toán bằng tiền mặt
                    </button>
                    <button class="btn btn-outline-secondary btn-lg w-100" onclick="payWithWallet('${session.id}', ${cost})" disabled>
                        <i class="bi bi-wallet2"></i> Thanh toán bằng ví (Sắp có)
                    </button>
                </div>
            </div>
        </div>
    `;
}

function displayPaymentInfo(payment) {
    const status = getPaymentStatusBadge(payment.status);
    const method = getPaymentMethodText(payment.method);

    document.getElementById('payment-content').innerHTML = `
        <div class="invoice-details">
            <div class="invoice-section">
                <h4>Thông tin thanh toán</h4>
                <div class="detail-row">
                    <strong>Mã giao dịch:</strong> ${payment.id}
                </div>
                <div class="detail-row">
                    <strong>Số tiền:</strong> ${new Intl.NumberFormat('vi-VN').format(payment.amount)} ${payment.currency}
                </div>
                <div class="detail-row">
                    <strong>Phương thức:</strong> ${method}
                </div>
                <div class="detail-row">
                    <strong>Trạng thái:</strong> ${status}
                </div>
                ${payment.providerTransactionId ? `
                <div class="detail-row">
                    <strong>Mã giao dịch VNPay:</strong> ${payment.providerTransactionId}
                </div>
                ` : ''}
            </div>
        </div>
    `;
}

async function payWithVNPay(sessionId, amount) {
    try {
        const response = await fetch('/api/Payment/vnpay/create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify({
                sessionId: sessionId,
                amount: amount,
                returnUrl: window.location.origin + '/Driver/Payment/VnPayReturn?paymentId='
            })
        });

        if (response.ok) {
            const data = await response.json();
            // Redirect to VNPay
            window.location.href = data.paymentUrl;
        } else {
            const error = await response.json();
            alert('Lỗi: ' + (error.message || 'Không thể tạo thanh toán VNPay'));
        }
    } catch (error) {
        console.error('Error creating VNPay payment:', error);
        alert('Có lỗi xảy ra. Vui lòng thử lại.');
    }
}

function payWithWallet(sessionId, amount) {
    alert('Tính năng thanh toán bằng ví sẽ sớm có mặt!');
}

async function payWithCash(sessionId, amount) {
    if (!confirm(`Xác nhận thanh toán ${new Intl.NumberFormat('vi-VN').format(amount)} VND bằng tiền mặt?`)) {
        return;
    }

    try {
        // Create cash payment using dedicated endpoint
        const paymentResponse = await fetch('/api/Payment/cash', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify({
                sessionId: sessionId,
                amount: amount,
                description: `Thanh toán tiền mặt cho phiên sạc ${sessionId}`
            })
        });

        if (!paymentResponse.ok) {
            const error = await paymentResponse.json();
            throw new Error(error.message || 'Không thể tạo thanh toán');
        }

        const payment = await paymentResponse.json();
        
        alert('Thanh toán bằng tiền mặt thành công!');
        
        // Reload payment info to show success status
        loadPayment(payment.id);
    } catch (error) {
        console.error('Error processing cash payment:', error);
        alert('Lỗi khi xử lý thanh toán: ' + error.message);
    }
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

