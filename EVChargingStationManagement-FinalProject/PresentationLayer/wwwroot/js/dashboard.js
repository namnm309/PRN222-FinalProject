(function () {
    const toggle = document.getElementById('SidebarToggle');
    if (!toggle) return;
    toggle.addEventListener('click', function () {
        document.body.classList.toggle('sidebar-collapsed');
        const collapsed = document.body.classList.contains('sidebar-collapsed');
        try { localStorage.setItem('dashboard.sidebar.collapsed', collapsed ? '1' : '0'); } catch { }
    });
    try {
        const saved = localStorage.getItem('dashboard.sidebar.collapsed');
        if (saved === '1') document.body.classList.add('sidebar-collapsed');
    } catch { }
})();




