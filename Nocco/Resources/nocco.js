$(document).ready(function () {
    // Back to top
    // When we read a long page we want to go back to top quickly so we make a link appear when user
    // scrolls down.
    var offset = 250;
    var duration = 300;
    $(window).scroll(function () {
        if ($(this).scrollTop() > offset) {
            $('.back-to-top').fadeIn(duration);
        } else {
            $('.back-to-top').fadeOut(duration);
        }
    });

    $('.back-to-top').fadeOut(1);

    $('.back-to-top').click(function (event) {
        event.preventDefault();
        $('html, body').animate({ scrollTop: 0 }, duration);
        return false;
    })

    $(".folder").click(function () {
        $(this).next("ul").toggle(500);
    });

    // Gestion de la recherche [ici](http://kilianvalkhof.com/2010/javascript/how-to-build-a-fast-simple-list-filter-with-jquery/) 
    $("#search").change(function () {
        var filter = $(this).val(); 
        if (filter) {
            $(".folder").next("ul").show();
            $("#menu ul").find("a:not(:Contains(" + filter +"))").parent().slideUp(); 
            $("#menu ul").find("a:Contains(" + filter +")").parent().slideDown(); 
        } else {            
            $("#menu ul").find("li").slideDown(); 
        } 
    }).keyup(function() { 
        $(this).change(); 
    });

});

jQuery.expr[':'].Contains = function (a, i, m) {
    return (a.textContent || a.innerText || "").toUpperCase().indexOf(m[3].toUpperCase()) >= 0;
};