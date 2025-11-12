// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Mobile Menu Toggle
document.addEventListener('DOMContentLoaded', function() {
    const mobileMenuToggle = document.querySelector('.mobile-menu-toggle');
    const mobileMenu = document.querySelector('.mobile-menu');
    const body = document.body;

    if (mobileMenuToggle && mobileMenu) {
        mobileMenuToggle.addEventListener('click', function() {
            mobileMenuToggle.classList.toggle('active');
            mobileMenu.classList.toggle('active');
            body.classList.toggle('menu-open');
        });

        // Close mobile menu when clicking outside
        document.addEventListener('click', function(event) {
            if (!mobileMenuToggle.contains(event.target) && !mobileMenu.contains(event.target)) {
                mobileMenuToggle.classList.remove('active');
                mobileMenu.classList.remove('active');
                body.classList.remove('menu-open');
            }
        });

        // Close mobile menu when clicking on a link
        const mobileNavLinks = mobileMenu.querySelectorAll('.mobile-nav-link');
        mobileNavLinks.forEach(link => {
            link.addEventListener('click', function() {
                mobileMenuToggle.classList.remove('active');
                mobileMenu.classList.remove('active');
                body.classList.remove('menu-open');
            });
        });
    }

    // User Dropdown Toggle (Click support for all devices)
    const userDropdownBtn = document.getElementById('userDropdownBtn');
    const userDropdown = document.querySelector('.user-dropdown');
    const userDropdownMenu = document.getElementById('userDropdownMenu');

    if (userDropdownBtn && userDropdown && userDropdownMenu) {
        console.log('Dropdown elements found');
        
        // Toggle dropdown on click
        userDropdownBtn.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            
            console.log('Dropdown button clicked');
            
            // Close all other dropdowns if any
            document.querySelectorAll('.user-dropdown.active').forEach(dropdown => {
                if (dropdown !== userDropdown) {
                    dropdown.classList.remove('active');
                }
            });
            
            // Toggle current dropdown
            const isActive = userDropdown.classList.contains('active');
            if (isActive) {
                userDropdown.classList.remove('active');
                console.log('Dropdown closed');
            } else {
                userDropdown.classList.add('active');
                console.log('Dropdown opened');
            }
        });

        // Close dropdown when clicking outside
        document.addEventListener('click', function(event) {
            const isClickInside = userDropdown.contains(event.target);
            if (!isClickInside && userDropdown.classList.contains('active')) {
                userDropdown.classList.remove('active');
                console.log('Dropdown closed (click outside)');
            }
        });

        // Close dropdown when clicking on a dropdown item (but allow navigation)
        if (userDropdownMenu) {
            userDropdownMenu.addEventListener('click', function(e) {
                const isDropdownItem = e.target.closest('.dropdown-item');
                if (isDropdownItem) {
                    console.log('Dropdown item clicked');
                    if (!isDropdownItem.classList.contains('logout-item')) {
                        // Don't close immediately for non-logout items to allow navigation
                        setTimeout(function() {
                            userDropdown.classList.remove('active');
                        }, 100);
                    } else {
                        // Close immediately for logout
                        userDropdown.classList.remove('active');
                    }
                }
            });
        }
    } else {
        console.log('Dropdown elements not found:', {
            btn: !!userDropdownBtn,
            dropdown: !!userDropdown,
            menu: !!userDropdownMenu
        });
    }

    // Prevent body scroll when mobile menu is open
    if (mobileMenu) {
        const observer = new MutationObserver(function(mutations) {
            if (mobileMenu.classList.contains('active')) {
                body.style.overflow = 'hidden';
            } else {
                body.style.overflow = '';
            }
        });

        observer.observe(mobileMenu, {
            attributes: true,
            attributeFilter: ['class']
        });
    }
});
