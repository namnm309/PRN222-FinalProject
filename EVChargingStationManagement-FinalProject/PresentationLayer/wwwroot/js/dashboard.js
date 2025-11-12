(function () {
    const toggle = document.getElementById('SidebarToggle');
    if (toggle) {
        toggle.addEventListener('click', function () {
            document.body.classList.toggle('sidebar-collapsed');
            const collapsed = document.body.classList.contains('sidebar-collapsed');
            try { localStorage.setItem('dashboard.sidebar.collapsed', collapsed ? '1' : '0'); } catch { }
        });
        try {
            const saved = localStorage.getItem('dashboard.sidebar.collapsed');
            if (saved === '1') document.body.classList.add('sidebar-collapsed');
        } catch { }
    }

    window.dashboardUtils = {
        fetchJson: async (url, options = {}) => {
            const res = await fetch(url, {
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    ...(options.headers || {})
                },
                credentials: 'include',
                ...options
            });
            if (!res.ok) {
                const text = await res.text();
                throw new Error(text || `Request failed: ${res.status}`);
            }
            return res.status === 204 ? null : await res.json();
        },
        formatNumber: (value, digits = 0) => {
            if (value === null || value === undefined || Number.isNaN(value)) return '--';
            return Number(value).toLocaleString('vi-VN', { minimumFractionDigits: digits, maximumFractionDigits: digits });
        },
        formatCurrency: (value) => {
            if (value === null || value === undefined || Number.isNaN(value)) return '--';
            return Number(value).toLocaleString('vi-VN', { style: 'currency', currency: 'VND', minimumFractionDigits: 0 });
        },
        formatDateTime: (value) => {
            if (!value) return '--';
            const date = new Date(value);
            return date.toLocaleString('vi-VN', {
                hour12: false,
                year: 'numeric',
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit'
            });
        },
        setKpiValue: (root, key, value, formatter = null) => {
            const el = root.querySelector(`[data-kpi="${key}"]`);
            if (!el) return;
            el.textContent = formatter ? formatter(value) : value;
        },
        renderStatusBadge: (status) => {
            if (!status) return '<span class="badge-status info">Không rõ</span>';
            const normalized = status.toString().toLowerCase();
            let tone = 'info';
            if (['completed', 'confirmed', 'active'].some(s => normalized.includes(s))) tone = 'success';
            else if (['pending', 'scheduled'].some(s => normalized.includes(s))) tone = 'warn';
            else if (['cancel', 'failed', 'error'].some(s => normalized.includes(s))) tone = 'danger';
            return `<span class="badge-status ${tone}">${status}</span>`;
        }
    };
})();
