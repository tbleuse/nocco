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

    //$(function () {
    //    $(".header").resizable({
    //        alsoResize: ".desc"
    //    });
    //    $("#.desc").resizable({
    //        alsoResize: ".header"
    //    });
    //});

});