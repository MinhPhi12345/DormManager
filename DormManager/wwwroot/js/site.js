// Tự động ẩn alert sau 6 giây
document.querySelectorAll('.alert-dismissible').forEach(function (el) {
    setTimeout(function () {
        bootstrap.Alert.getOrCreateInstance(el).close();
    }, 6000);
});
