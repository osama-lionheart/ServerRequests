$(function () {

    $('.user-info').click(function () {
        $(this).parents('.user-info-box').toggleClass('opened');

        var closePopup = function (event) {

            if (!$(event.target).is('.user-info-box *')) {
                $('.user-info-box').removeClass('opened');
                $(document).unbind('click', closePopup);
            }
        };

        $(document).click(closePopup);
    });

    $('.advanced-search-arrow').click(function () {
        $('.top-bar-wrapper').toggleClass('open');
        $(this).siblings('.search-box').val('');
    });

    $('.help-button').click(function () {
        var $wrapper = $('.policy-help-wrapper').toggleClass('opened');

        var closePopup = function (event) {
            
            var $target = $(event.target);
            if (!$target.is('.policy-help-wrapper *') && !$target.is('.help-button') && !$target.is('.help-button *')) {
                $('.policy-help-wrapper').removeClass('opened');
                $(document).unbind('click', closePopup);
            }
        };

        if ($wrapper.hasClass("opened")) {
            $(document).click(closePopup);
        } else {
            $(document).unbind('click', closePopup);
        }
    });

});