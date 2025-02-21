document.addEventListener("DOMContentLoaded", function () {
    // Use the global variable 'errorMessage' that we define in the view.
    if (typeof errorMessage !== "undefined" && errorMessage) {
        Swal.fire({
            icon: 'error',
            title: 'Oops...',
            text: errorMessage,
        });
    }
});
