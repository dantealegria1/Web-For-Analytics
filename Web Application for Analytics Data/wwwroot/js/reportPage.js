document.addEventListener("DOMContentLoaded", function () {
    var clearFiltersBtn = document.getElementById("clearFilters");
    if (clearFiltersBtn) {
        clearFiltersBtn.addEventListener("click", function () {
            var form = document.querySelector("form");
            if (form) {
                form.reset();

                // Si no funciona hacerlo manualmente
                document.querySelectorAll("input").forEach(function(input) {
                    input.value = "";
                });

                // Submit the form to refresh the results
                form.submit();
            }
        });
    }
});
