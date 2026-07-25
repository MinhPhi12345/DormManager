// Tự động ẩn alert sau 6 giây
document.querySelectorAll('.alert-dismissible').forEach(function (el) {
    setTimeout(function () {
        bootstrap.Alert.getOrCreateInstance(el).close();
    }, 6000);
});

/* =====================================================================
   MODAL XÁC NHẬN DÙNG CHUNG - thay thế confirm() mặc định của trình duyệt
   Cách dùng: gắn vào <form> (hoặc <button>) 3 thuộc tính:
     data-confirm-title   : tiêu đề ngắn
     data-confirm-message : nội dung cảnh báo
     data-confirm-level   : "danger" | "warning" | "success" (mặc định "warning")
   ===================================================================== */
(function () {
    var modalEl = document.getElementById('modalXacNhanChung');
    if (!modalEl) return;

    var modal = new bootstrap.Modal(modalEl);
    var icon = document.getElementById('xnIcon');
    var iconWrap = document.getElementById('xnIconWrap');
    var title = document.getElementById('xnTitle');
    var message = document.getElementById('xnMessage');
    var btnXacNhan = document.getElementById('xnBtnXacNhan');
    var dangCho = null; // form hoặc nút đang chờ xác nhận

    var CAU_HINH = {
        danger:  { icon: 'bi-exclamation-triangle-fill', lop: 'xn-danger',  nut: 'btn-danger' },
        warning: { icon: 'bi-question-circle-fill',      lop: 'xn-warning', nut: 'btn-dm-accent' },
        success: { icon: 'bi-check-circle-fill',         lop: 'xn-success', nut: 'btn-dm' }
    };

    function moModal(el) {
        dangCho = el;
        var muc = el.dataset.confirmLevel || 'warning';
        var cfg = CAU_HINH[muc] || CAU_HINH.warning;
        icon.className = 'bi ' + cfg.icon;
        iconWrap.className = 'xn-icon-wrap ' + cfg.lop;
        title.textContent = el.dataset.confirmTitle || 'Xác nhận thao tác';
        message.textContent = el.dataset.confirmMessage || '';
        btnXacNhan.className = 'btn px-4 ' + cfg.nut;
        modal.show();
    }

    // Chặn submit của <form data-confirm-message="...">
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!(form instanceof HTMLFormElement) || !form.hasAttribute('data-confirm-message')) return;
        if (form.dataset.daXacNhan === '1') { delete form.dataset.daXacNhan; return; } // đã xác nhận -> cho submit thật
        e.preventDefault();
        moModal(form);
    });

    // Chặn click của nút độc lập <button data-confirm-message="..."> (không nằm trong <form> cần chặn riêng)
    document.addEventListener('click', function (e) {
        var el = e.target.closest('[data-confirm-message]');
        if (!el || el.tagName === 'FORM' || el.closest('form[data-confirm-message]')) return;
        if (el.dataset.daXacNhan === '1') { delete el.dataset.daXacNhan; return; }
        e.preventDefault();
        moModal(el);
    });

    btnXacNhan.addEventListener('click', function () {
        modal.hide();
        if (!dangCho) return;
        var el = dangCho;
        dangCho = null;
        el.dataset.daXacNhan = '1';
        if (el.tagName === 'FORM') {
            (el.requestSubmit ? el.requestSubmit() : el.submit());
        } else {
            el.click();
        }
    });
})();
