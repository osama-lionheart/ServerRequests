$(function () {

    $('select').each(function (i, item) {
        var $item = $(item);
        var name = $item.attr('name');

        var selectedVal = $item.find('option[selected]').val();

        var $select = $('<div class="select" data-name="' + name + '"/>');
        var $selectHeader = $('<div class="select-header" tabindex="0">Select...</div>').appendTo($select);
        var $input = $('<input type="hidden" name="' + name + '" value="-1"/>').appendTo($select);
        var $options = $('<div class="select-options jfk-scrollbar" />').appendTo($select);

        $item.find('option').each(function (idx, opt) {
            if (idx > 0) {
                var $opt = $(opt);
                var val = $opt.val();

                if (val == selectedVal) {
                    $input.val(val);
                    $selectHeader.html($opt.html());
                }

                $options.append('<div class="select-option" data-value="' + val + '">' + $opt.html() + '</div>')
            }
        });

        $item.after($select);
        $item.remove();
    });

    $('.select-header').live('mousedown', function (ev) {

        var $select = $(this).parents('.select');

        if (!$select.hasClass('opened')) {

            $select.addClass('opened');

            var cancelMouseDown = function (event) {

                if (!$(event.target).hasClass('select-options') && !$(event.target).parents('.select-options').length) {
                    $select.removeClass('opened');

                    $(document).unbind('mousedown.selectbox', cancelMouseDown);
                }
            };

            $(document).bind('mousedown.selectbox', cancelMouseDown);

            ev.preventDefault();
        }
    });

    $('.select-option').live('mouseup', function () {
        var $this = $(this);

        if ($this.hasClass('select-option-hover')) {
            var $select = $this.parents('.select');

            var name = $select.data('name');

            var $hiddenInput = $select.find('input[name="' + name + '"]').attr('value', $this.data('value'));

            $hiddenInput.trigger('change');

            $select.find('.select-header').html($this.html());

            $select.removeClass('opened');
            $(document).unbind('mousedown.selectbox');
        }
    });

    $('.select-option').live('mouseover', function () {
        $(this).addClass('select-option-hover');
    });

    $('.select-option').live('mouseout', function () {
        $(this).removeClass('select-option-hover');
    });

    $('form[data-confirm=true]').live('submit', function () {
        return confirm("Are you sure?");
    });

    $('.ip-address123123123').live('keypress', function (event) {
        event = event || window.event;
        var key = String.fromCharCode(event.keyCode || event.which),
            rfilter = /[0-9]|\.|\//,
            rip = '(\\d{1,3})',
            raddress = new RegExp(
                '^' +
                rip +
                '(?:\\.(?:' + rip + '(?:\\.(?:' + rip + '(?:\\.(?:' + rip +
                '(?:\\/(\\d{1,2})?)?' +
                ')?)?)?)?)?)?' +
                '$'
            );

        function cancelEvent() {
            event.returnValue = false;

            if (event.preventDefault) {
                event.preventDefault();
            }
        }

        if (!rfilter.test(key)) {
            cancelEvent();
            return;
        }

        var val = $(this).val() + key;
        var match = raddress.exec(val);

        if (match) {

            if (match[5] && match[5] > 32) {
                cancelEvent();
                return;
            }

            var lastByte;

            for (var i = 4; i >= 0; --i) {
                if (match[i]) {
                    lastByte = match[i];
                    break;
                }
            }

            if (lastByte > 255) {
                cancelEvent();
                return;
            }
            else if (lastByte.length == 3) {
                val += '.';
                var match2 = raddress.exec(val);

                if (match2) {
                    $(this).val(val);
                    cancelEvent();
                }
            }
        } else {
            cancelEvent();
        }
    });
});