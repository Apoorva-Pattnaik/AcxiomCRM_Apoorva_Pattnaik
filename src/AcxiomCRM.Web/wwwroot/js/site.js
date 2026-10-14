// AcxiomCRM Global JavaScript Utilities

document.addEventListener('DOMContentLoaded', function () {
    // 1. Initialize Bootstrap Tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // 2. Destructive Action Confirmation Dialogs
    var confirmButtons = document.querySelectorAll('[data-confirm]');
    confirmButtons.forEach(function (button) {
        button.addEventListener('click', function (e) {
            var message = button.getAttribute('data-confirm') || 'Are you sure you want to proceed with this action?';
            if (!confirm(message)) {
                e.preventDefault();
                return false;
            }
        });
    });

    // 3. Auto-dismiss alerts after 5 seconds
    var autoAlerts = document.querySelectorAll('.alert-dismissible');
    autoAlerts.forEach(function (alert) {
        setTimeout(function () {
            var bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            if (bsAlert) {
                bsAlert.close();
            }
        }, 6000);
    });
});

// Format Currency in INR
function formatCurrency(amount) {
    return new Intl.NumberFormat('en-IN', {
        style: 'currency',
        currency: 'INR',
        maximumFractionDigits: 0
    }).format(amount);
}
